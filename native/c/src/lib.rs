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

use std::{
    slice,
    str,
};

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
mod thread_bound;

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

pub type DbStoreHandle = HandleShared<DbStore>;

#[repr(C)]
pub struct DbReader {
    inner: thread_bound::DeferredCleanup<store::reader::Reader>,
}

pub type DbReaderHandle = HandleOwned<DbReader>;

#[repr(C)]
pub struct DbWriter {
    inner: store::writer::Writer,
}

pub type DbWriterHandle = HandleOwned<DbWriter>;

#[repr(C)]
pub struct DbDeleter {
    inner: store::deleter::Deleter,
}

pub type DbDeleterHandle = HandleOwned<DbDeleter>;

ffi_no_catch! {
    fn db_last_result(
        message_buf: *mut u8,
        message_buf_len: size_t,
        actual_message_len: Out<size_t>,
        result: Out<DbResult>
    ) -> DbResult {
        DbResult::with_last_result(|last_result| {
            let (value, error) = last_result.unwrap_or((DbResult::Ok, None));

            unsafe_block!("The out pointer is valid and not mutably aliased elsewhere" => *result = value);

            if let Some(error) = error {
                let message = unsafe_block!("The buffer lives as long as `db_last_result` and the length is within the buffer" => slice::from_raw_parts_mut(message_buf, message_buf_len));

                unsafe_block!("The out pointer is valid and not mutably aliased elsewhere" => *actual_message_len = error.len());

                if message.len() < error.len() {
                    return DbResult::BufferTooSmall;
                }

                (&mut message[0..error.len()]).copy_from_slice(error.as_bytes());
            } else {
                unsafe_block!("The out pointer is valid and not mutably aliased elsewhere" => *actual_message_len = 0);
            }

            DbResult::Ok
        })
    }
}

ffi! {
    fn db_store_open(path: *const u8, path_len: size_t, store: Out<DbStoreHandle>) -> DbResult {
        let path_slice = unsafe_block!("The path lives as long as `db_store_open` and the length is within the path" => slice::from_raw_parts(path, path_len));
        let path = str::from_utf8(path_slice)?;

        let handle = DbStoreHandle::alloc(DbStore {
            inner: store::Store::open(path)?,
        });

        unsafe_block!("The out pointer is valid and not mutably aliased elsewhere" => *store = handle);

        DbResult::Ok
    }

    fn db_store_close(store: DbStoreHandle) -> DbResult {
        unsafe_block!("The upstream caller guarantees the handle will not be accessed after being freed" => DbStoreHandle::dealloc(store, |mut store| {
            store.inner.close()?;

            DbResult::Ok
        }))
    }

    fn db_read_begin(
        store: DbStoreHandle,
        reader: Out<DbReaderHandle>
    ) -> DbResult {
        let handle = DbReaderHandle::alloc(DbReader {
            inner: thread_bound::DeferredCleanup::new(store.inner.read_begin()?),
        });

        unsafe_block!("The out pointer is valid and not mutably aliased elsewhere" => *reader = handle);

        DbResult::Ok
    }

    fn db_read_next(
        reader: DbReaderHandle,
        key: Out<DbKey>,
        value_buf: *mut u8,
        value_buf_len: size_t,
        actual_value_len: Out<size_t>
    ) -> DbResult {
        let buf = unsafe_block!("The buffer lives as long as `db_read_next` and the length is within the buffer" => slice::from_raw_parts_mut(value_buf, value_buf_len));
        let reader = &mut *reader;

        'read_event: loop {
            let read_result = reader.inner.with_current(|mut current| {
                let key = unsafe_block!("The out pointer is valid and not mutably aliased elsewhere" => &mut *key);
                let actual_value_len = unsafe_block!("The out pointer is valid and not mutably aliased elsewhere" => &mut *actual_value_len);

                read::into_fixed_buffer(&mut current, buf, key, actual_value_len)
            });

            match read_result {
                // If the result is ok then we're done with this event
                // Fetch the next one
                Some(DbResult::Ok) => {
                    reader.inner.move_next()?;

                    return DbResult::Ok;
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
                        return DbResult::Done;
                    }
                }
            }
        }
    }

    fn db_read_end(reader: DbReaderHandle) -> DbResult {
        unsafe_block!("The upstream caller guarantees the handle will not be accessed after being freed" => DbReaderHandle::dealloc(reader, |mut reader| {
            reader.inner.complete()?;

            DbResult::Ok
        }))
    }

    fn db_write_begin(
        store: DbStoreHandle,
        writer: Out<DbWriterHandle>
    ) -> DbResult {
        let handle = DbWriterHandle::alloc(DbWriter {
            inner: store.inner.write_begin()?,
        });

        unsafe_block!("The out pointer is valid and not mutably aliased elsewhere" => *writer = handle);

        DbResult::Ok
    }

    fn db_write_set(
        writer: DbWriterHandle,
        key: *const DbKey,
        value: *const u8,
        value_len: size_t
    ) -> DbResult {
        let key = unsafe_block!("The key pointer lives as long as `db_write_set` and points to valid data" => &*key);

        let value_slice = unsafe_block!("The buffer lives as long as `db_write_set` and the length is within the buffer" => slice::from_raw_parts(value, value_len));

        let data = Data {
            key: data::Key::from_bytes(key.0),
            payload: value_slice,
        };

        writer.inner.set(data)?;

        DbResult::Ok
    }

    fn db_write_end(writer: DbWriterHandle) -> DbResult {
        unsafe_block!("The upstream caller guarantees the handle will not be accessed after being freed" => DbWriterHandle::dealloc(writer, |mut writer| {
            writer.inner.complete()?;

            DbResult::Ok
        }))
    }

    fn db_delete_begin(
        store: DbStoreHandle,
        deleter: Out<DbDeleterHandle>
    ) -> DbResult {
        let handle = DbDeleterHandle::alloc(DbDeleter {
            inner: store.inner.delete_begin()?,
        });

        unsafe_block!("The out pointer is valid and not mutably aliased elsewhere" => *deleter = handle);

        DbResult::Ok
    }

    fn db_delete_remove(
        deleter: DbDeleterHandle,
        key: *const DbKey
    ) -> DbResult {
        let key = unsafe_block!("The key pointer lives as long as `db_delete_remove` and points to valid data" => &*key);

        deleter.inner.remove(data::Key::from_bytes(key.0))?;

        DbResult::Ok
    }

    fn db_delete_end(deleter: DbDeleterHandle) -> DbResult {
        unsafe_block!("The upstream caller guarantees the handle will not be accessed after being freed" => DbDeleterHandle::dealloc(deleter, |mut deleter| {
            deleter.inner.complete()?;

            DbResult::Ok
        }))
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
