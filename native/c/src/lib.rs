/*!
C interface to the database.
*/

// Unsafe is explicitly allowed through `unsafe_*` macros
#![deny(unsafe_code)]
// For converting Rust results into FFI results
#![feature(try_trait)]

#[macro_use]
#[path = "../../std_ext/mod.rs"]
mod std_ext;

#[macro_use]
extern crate lazy_static;

use std::str;

use libc::size_t;

use db::{
    data::{
        self,
        Data,
    },
    store,
};

#[macro_use]
mod macros;

mod handle;
mod is_null;
mod read;
mod result;

pub use self::{
    handle::*,
    result::*,
};

#[repr(transparent)]
pub struct DbKey([u8; 16]);

#[repr(C)]
pub struct DbStore {
    inner: store::Store,
}

pub type DbStoreHandle<'a> = HandleShared<'a, DbStore>;

#[repr(C)]
pub struct DbReader {
    inner: thread_bound::DeferredCleanup<store::reader::Reader>,
}

pub type DbReaderHandle<'a> = HandleExclusive<'a, DbReader>;

#[repr(C)]
pub struct DbWriter {
    inner: store::writer::Writer,
}

pub type DbWriterHandle<'a> = HandleExclusive<'a, DbWriter>;

#[repr(C)]
pub struct DbDeleter {
    inner: store::deleter::Deleter,
}

pub type DbDeleterHandle<'a> = HandleExclusive<'a, DbDeleter>;

ffi_no_catch! {
    fn db_last_result(
        message_buf: Out<u8>,
        message_buf_len: size_t,
        actual_message_len: Out<size_t>,
        result: Out<DbResult>
    ) -> DbResult {
        DbResult::with_last_result(|last_result| {
            let (value, error) = last_result.unwrap_or((DbResult::ok(), None));

            unsafe_block!("The out pointer is valid and not mutably aliased elsewhere" => result.init(value));

            if let Some(error) = error {
                unsafe_block!("The out pointer is valid and not mutably aliased elsewhere" => actual_message_len.init(error.len()));

                if message_buf_len < error.len() {
                    return DbResult::buffer_too_small();
                }

                unsafe_block!("The buffer is valid for writes and the length is within the buffer" => message_buf.init_bytes(error.as_bytes()));
            } else {
                unsafe_block!("The out pointer is valid and not mutably aliased elsewhere" => actual_message_len.init(0));
            }

            DbResult::ok()
        })
    }
}

ffi! {
    fn db_store_open(path: Ref<u8>, path_len: size_t, store: Out<DbStoreHandle>) -> DbResult {
        let path_slice = unsafe_block!("The path lives as long as `db_store_open` and the length is within the path" => path.as_bytes(path_len));
        let path = str::from_utf8(path_slice)?;

        let handle = DbStoreHandle::alloc(DbStore {
            inner: store::Store::open(path)?,
        });

        unsafe_block!("The out pointer is valid and not mutably aliased elsewhere" => store.init(handle));

        DbResult::ok()
    }

    fn db_store_close(store: DbStoreHandle) -> DbResult {
        unsafe_block!("The upstream caller guarantees the handle will not be accessed after being freed" => DbStoreHandle::dealloc(store, |mut store| {
            store.inner.close()?;

            DbResult::ok()
        }))
    }

    fn db_read_begin(
        store: DbStoreHandle,
        reader: Out<DbReaderHandle>
    ) -> DbResult {
        let store = store.as_ref();

        let handle = DbReaderHandle::alloc(DbReader {
            inner: thread_bound::DeferredCleanup::new(store.inner.read_begin()?),
        });

        unsafe_block!("The out pointer is valid and not mutably aliased elsewhere" => reader.init(handle));

        DbResult::ok()
    }

    fn db_read_next(
        reader: DbReaderHandle,
        key: Out<DbKey>,
        value_buf: Out<u8>,
        value_buf_len: size_t,
        actual_value_len: Out<size_t>
    ) -> DbResult {
        let reader = reader.as_mut();

        let buf = unsafe_block!("The buffer lives as long as `db_read_next`, the length is within the buffer and the buffer won't be read before initialization" => value_buf.as_uninit_bytes_mut(value_buf_len));

        'read_event: loop {
            let read_result = reader.inner.with_current(|mut current| {
                read::into_fixed_buffer(&mut current, buf, &mut key, &mut actual_value_len)
            });

            match read_result {
                // If the result is ok then we're done with this event
                // Fetch the next one
                Some(result) if result.is_ok() => {
                    reader.inner.move_next()?;

                    return DbResult::ok();
                },
                // If the result is anything but `Ok` then return.
                // This probably means the caller-supplied buffer was
                // too small
                Some(result) => return result,
                // If there is no result then we don't have an event.
                // Fetch the next event and recurse.
                // This probably means we're reading the first event,
                // or have reached the end.
                None => {
                    if reader.inner.move_next()? {
                        continue 'read_event;
                    } else {
                        return DbResult::done();
                    }
                }
            }
        }
    }

    fn db_read_end(reader: DbReaderHandle) -> DbResult {
        unsafe_block!("The upstream caller guarantees the handle will not be accessed after being freed" => DbReaderHandle::dealloc(reader, |mut reader| {
            reader.inner.complete()?;

            DbResult::ok()
        }))
    }

    fn db_write_begin(
        store: DbStoreHandle,
        writer: Out<DbWriterHandle>
    ) -> DbResult {
        let store = store.as_ref();

        let handle = DbWriterHandle::alloc(DbWriter {
            inner: store.inner.write_begin()?,
        });

        unsafe_block!("The out pointer is valid and not mutably aliased elsewhere" => writer.init(handle));

        DbResult::ok()
    }

    fn db_write_set(
        writer: DbWriterHandle,
        key: Ref<DbKey>,
        value: Ref<u8>,
        value_len: size_t
    ) -> DbResult {
        let writer = writer.as_mut();

        let key = unsafe_block!("The key pointer lives as long as `db_write_set` and points to valid data" => key.as_ref());
        let value_slice = unsafe_block!("The buffer lives as long as `db_write_set` and the length is within the buffer" => value.as_bytes(value_len));

        let data = Data {
            key: data::Key::from_bytes(key.0),
            payload: value_slice,
        };

        writer.inner.set(data)?;

        DbResult::ok()
    }

    fn db_write_end(writer: DbWriterHandle) -> DbResult {
        unsafe_block!("The upstream caller guarantees the handle will not be accessed after being freed" => DbWriterHandle::dealloc(writer, |mut writer| {
            writer.inner.complete()?;

            DbResult::ok()
        }))
    }

    fn db_delete_begin(
        store: DbStoreHandle,
        deleter: Out<DbDeleterHandle>
    ) -> DbResult {
        let store = store.as_ref();

        let handle = DbDeleterHandle::alloc(DbDeleter {
            inner: store.inner.delete_begin()?,
        });

        unsafe_block!("The out pointer is valid and not mutably aliased elsewhere" => deleter.init(handle));

        DbResult::ok()
    }

    fn db_delete_remove(
        deleter: DbDeleterHandle,
        key: Ref<DbKey>
    ) -> DbResult {
        let deleter = deleter.as_mut();

        let key = unsafe_block!("The key pointer lives as long as `db_delete_remove` and points to valid data" => key.as_ref());

        deleter.inner.remove(data::Key::from_bytes(key.0))?;

        DbResult::ok()
    }

    fn db_delete_end(deleter: DbDeleterHandle) -> DbResult {
        unsafe_block!("The upstream caller guarantees the handle will not be accessed after being freed" => DbDeleterHandle::dealloc(deleter, |mut deleter| {
            deleter.inner.complete()?;

            DbResult::ok()
        }))
    }
}

#[cfg(debug_assertions)]
ffi! {
    fn db_test_error() -> DbResult {
        use std::io;

        DbResult::internal_error().context(io::Error::new(io::ErrorKind::Other, "A test error"))
    }

    fn db_test_ok() -> DbResult {
        DbResult::ok()
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    use crate::std_ext::test::static_assert;

    #[test]
    fn assert_send_sync() {
        static_assert::is_send::<DbStoreHandle>();
        static_assert::is_sync::<DbStoreHandle>();
        static_assert::is_unwind_safe::<DbStoreHandle>();

        static_assert::is_send::<DbReaderHandle>();
        static_assert::is_sync::<DbReaderHandle>();
        static_assert::is_unwind_safe::<DbReaderHandle>();

        static_assert::is_send::<DbWriterHandle>();
        static_assert::is_sync::<DbWriterHandle>();
        static_assert::is_unwind_safe::<DbWriterHandle>();

        static_assert::is_send::<DbDeleterHandle>();
        static_assert::is_sync::<DbDeleterHandle>();
        static_assert::is_unwind_safe::<DbDeleterHandle>();
    }
}
