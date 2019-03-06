/*!
C interface to the database.
*/

use std::{
    slice,
    str,
};

use libc::size_t;

use crate::{
    store,
    data::{self, Data},
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
    inner: thread_bound::DeferredCleanup<store::writer::Writer>,
}

pub type DbWriterHandle = HandleOwned<DbWriter>;

ffi_no_catch! {
    fn db_last_result(
        message_buf: *mut u8,
        message_buf_len: size_t,
        actual_message_len: Out<size_t>,
        result: Out<DbResult>
    ) -> DbResult {
        DbResult::with_last_result(|last_result| {
            let (value, error) = last_result.unwrap_or((DbResult::Ok, None));

            *result = value;

            if let Some(error) = error {
                let message = slice::from_raw_parts_mut(message_buf, message_buf_len);

                *actual_message_len = error.len();

                if message.len() < error.len() {
                    return DbResult::BufferTooSmall;
                }

                (&mut message[0..error.len()]).copy_from_slice(error.as_bytes());
            } else {
                *actual_message_len = 0;
            }

            DbResult::Ok
        })
    }
}

ffi! {
    fn db_store_open(path: *const u8, path_len: size_t, store: Out<DbStoreHandle>) -> DbResult {
        let path_slice = slice::from_raw_parts(path, path_len);
        let path = str::from_utf8(path_slice)?;

        *store = DbStoreHandle::alloc(DbStore {
            inner: store::Store::open(path)?,
        });

        DbResult::Ok
    }

    fn db_store_close(store: DbStoreHandle) -> DbResult {
        DbStoreHandle::dealloc(store, |mut store| {
            store.inner.close()?;

            DbResult::Ok
        })
    }

    fn db_read_begin(
        store: DbStoreHandle,
        reader: Out<DbReaderHandle>
    ) -> DbResult {
        *reader = DbReaderHandle::alloc(DbReader {
            inner: thread_bound::DeferredCleanup::new(store.inner.read_begin()?),
        });

        DbResult::Ok
    }

    fn db_read_next(
        reader: DbReaderHandle,
        key: Out<DbKey>,
        value_buf: *mut u8,
        value_buf_len: size_t,
        actual_value_len: Out<size_t>
    ) -> DbResult {
        let buf = slice::from_raw_parts_mut(value_buf, value_buf_len);
        let reader = &mut *reader;

        'read_event: loop {
            let read_result = reader.inner.with_current(|mut current|
                read::into_fixed_buffer(&mut current, buf, &mut *key, &mut *actual_value_len));

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
        DbReaderHandle::dealloc(reader, |mut reader| {
            reader.inner.complete()?;

            DbResult::Ok
        })
    }

    fn db_write_begin(
        store: DbStoreHandle,
        writer: Out<DbWriterHandle>
    ) -> DbResult {
        *writer = DbWriterHandle::alloc(DbWriter {
            inner: thread_bound::DeferredCleanup::new(store.inner.begin_write()?),
        });

        DbResult::Ok
    }

    fn db_write_set(
        writer: DbWriterHandle,
        key: DbKey,
        value: *const u8,
        value_len: size_t
    ) -> DbResult {
        let value_slice = slice::from_raw_parts(value, value_len);

        let data = Data {
            key: data::Key::from_bytes(key.0),
            payload: value_slice,
        };
        
        writer.inner.set(data)?;

        DbResult::Ok
    }

    fn db_write_end(writer: DbWriterHandle) -> DbResult {
        DbWriterHandle::dealloc(writer, |mut writer| {
            writer.inner.complete()?;

            DbResult::Ok
        })
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    use crate::std_ext::test::static_assert;

    #[test]
    fn assert_send_sync() {
        static_assert::is_send::<DbStoreHandle>();
        static_assert::is_unwind_safe::<DbStoreHandle>();

        static_assert::is_send::<DbReaderHandle>();
        static_assert::is_unwind_safe::<DbReaderHandle>();
    }
}
