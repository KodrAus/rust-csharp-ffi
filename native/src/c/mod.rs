/*!
A C interface to a database.
*/

// Almost everything here is going to be unsafe
#![allow(unsafe_code)]

use std::{
    ops::{
        Deref,
        DerefMut,
    },
    panic::{
        RefUnwindSafe,
        UnwindSafe,
    },
    slice,
};

use libc::size_t;

use self::{
    is_null::IsNull,
    thread_bound::ThreadBound,
};

#[macro_use]
mod macros;

mod thread_bound;
mod is_null;
mod result;

pub use self::result::*;

/**
A shared handle that can be accessed concurrently by multiple threads.

The interior value can be treated like `&T`.
*/
#[repr(transparent)]
pub struct HandleShared<T: ?Sized>(*const T);

unsafe_impl!("The handle is semantically `&T`" => impl<T: ?Sized + Sync> Send for HandleShared<T> {});

impl<T: ?Sized + RefUnwindSafe> UnwindSafe for HandleShared<T> {}

impl<T: Send + Sync + 'static> HandleShared<T> {
    fn alloc(value: T) -> Self {
        let v = Box::new(value);
        HandleShared(Box::into_raw(v))
    }
}

impl<T: ?Sized + Send + Sync> HandleShared<T> {
    unsafe_fn!("There are no other live references and the handle won't be used again" =>
        fn dealloc<R>(handle: Self, f: impl FnOnce(&mut T) -> R) -> R {
            let mut v = Box::from_raw(handle.0 as *mut T);
            f(&mut *v)
        });
}

/*
We require thread-safety bounds on `Deref` even though they're
not _technically_ needed here so we can catch ourselves using
data in the handles that doesn't satisfy their safety requirements
*/
impl<T: ?Sized + Send + Sync> Deref for HandleShared<T> {
    type Target = T;

    fn deref(&self) -> &T {
        unsafe_block!("We own the interior value" => { &*self.0 })
    }
}

/**
A non-shared handle that cannot be accessed by multiple threads.

The handle is bound to the thread that it was created on.
The interior value can be treated like `&mut T`.
*/
#[repr(transparent)]
pub struct HandleOwned<T: ?Sized>(*mut ThreadBound<T>);

unsafe_impl!("The handle is semantically `&mut T`" => impl<T: ?Sized + Send> Send for HandleOwned<T> {});

impl<T: ?Sized + RefUnwindSafe> UnwindSafe for HandleOwned<T> {}

impl<T: Send + 'static> HandleOwned<T> {
    fn alloc(value: T) -> Self {
        let v = Box::new(ThreadBound::new(value));
        HandleOwned(Box::into_raw(v))
    }
}

impl<T: ?Sized + Send> HandleOwned<T> {
    unsafe_fn!("There are no other live references and the handle won't be used again" =>
        fn dealloc<R>(handle: Self, f: impl FnOnce(&mut T) -> R) -> R {
            let mut v = Box::from_raw(handle.0);
            f(&mut **v)
        });
}

/*
We require thread-safety bounds on `Deref` even though they're
not _technically_ needed here so we can catch ourselves using
data in the handles that doesn't satisfy their safety requirements
*/
impl<T: ?Sized + Send> Deref for HandleOwned<T> {
    type Target = T;

    fn deref(&self) -> &T {
        unsafe_block!("We own the interior value" => { &**self.0 })
    }
}

/*
We require thread-safety bounds on `Deref` even though they're
not _technically_ needed here so we can catch ourselves using
data in the handles that doesn't satisfy their safety requirements
*/
impl<T: ?Sized + Send> DerefMut for HandleOwned<T> {
    fn deref_mut(&mut self) -> &mut T {
        unsafe_block!("We own the interior value" => { &mut **self.0 })
    }
}

pub type Out<T> = *mut T;

#[repr(C)]
pub struct DbStore {

}

pub type DbStoreHandle = HandleShared<DbStore>;

// The last result function isn't part of the FFI macro because it doesn't set its own last error
#[no_mangle]
pub unsafe extern "C" fn db_last_result(
    message_buf: *mut u8,
    message_buf_len: size_t,
    actual_message_len: Out<size_t>,
    result: Out<DbResult>,
) -> DbResult {
    // Any arguments added to the `db_last_result` function
    // need to be checked for null here
    if message_buf.is_null()
        || message_buf_len.is_null()
        || actual_message_len.is_null()
        || result.is_null()
    {
        return DbResult::ArgumentNull;
    }

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

ffi! {
    fn db_store_open(store: Out<DbStoreHandle>) -> DbResult {
        *store = DbStoreHandle::alloc(DbStore { });

        DbResult::Ok
    }

    fn db_store_close(store: DbStoreHandle) -> DbResult {
        DbStoreHandle::dealloc(store, |s| {
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
