/*!
C interface to the database.
*/

// Almost everything here is going to be unsafe
#![allow(unsafe_code)]

use std::{
    slice,
    str,
};

use libc::size_t;

use crate::{
    c::{
        is_null::IsNull,
    },
    db,
};

#[macro_use]
mod macros;

mod is_null;
mod result;
mod handle;
mod thread_bound;

pub use self::{
    result::*,
    handle::*,
};

#[repr(C)]
pub struct DbStore {
    inner: db::Db,
}

pub type DbStoreHandle = HandleShared<DbStore>;

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
            inner: db::Db::open(path)?,
        });

        DbResult::Ok
    }

    fn db_store_close(store: DbStoreHandle) -> DbResult {
        DbStoreHandle::dealloc(store, |store| {
            store.inner.close()?;

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
    }
}
