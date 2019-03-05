use std::{
    ops::{
        Deref,
        DerefMut,
    },
    panic::{
        RefUnwindSafe,
        UnwindSafe,
    },
};

use super::thread_bound::ThreadBound;

/*
The handles here are wrappers for a shared `&T` and an exclusive `&mut T`.

They protect from data races, but don't protect from use-after-free bugs.
The caller is expected to maintain that invariant, which in .NET can be
achieved using `SafeHandle`s.
*/

/**
A shared handle that can be accessed concurrently by multiple threads.

The interior value can be treated like `&T`.

Consumers must ensure a handle is not used again after it has been deallocated.
*/
#[repr(transparent)]
pub struct HandleShared<T: ?Sized>(*const T);

unsafe_impl!("The handle is semantically `&T`" => impl<T: ?Sized + Sync> Send for HandleShared<T> {});

impl<T: ?Sized + RefUnwindSafe> UnwindSafe for HandleShared<T> {}

impl<T: ?Sized> HandleShared<T> {
    pub(super) fn as_ptr(&self) -> *const T {
        self.0
    }
}

impl<T: Send + Sync + 'static> HandleShared<T> {
    pub(super) fn alloc(value: T) -> Self {
        let v = Box::new(value);
        HandleShared(Box::into_raw(v))
    }
}

impl<T: Send + Sync> HandleShared<T> {
    unsafe_fn!("There are no other live references and the handle won't be used again" =>
        pub(super) fn dealloc<R>(handle: Self, f: impl FnOnce(T) -> R) -> R {
            let v = Box::from_raw(handle.0 as *mut T);
            f(*v)
        });
}

/*
We require thread-safety bounds on `Deref` even though they're
not _technically_ needed here (the `alloc` method protects us)
so we can catch ourselves using data in the handles that doesn't
satisfy their safety requirements
*/
impl<T: ?Sized + Send + Sync> Deref for HandleShared<T> {
    type Target = T;

    fn deref(&self) -> &T {
        unsafe_block!("We own the interior value" => { &*self.0 })
    }
}

/**
A non-shared handle that cannot be accessed by multiple threads.

The interior value can be treated like `&mut T`.

The handle is bound to the thread that it was created on to ensure
there's no possibility for data races. Note that this doesn't rule out
the possibility of multiple live mutable aliases to the same handle, even
though memory accesses themselves will be safe.

The handle _can_ be deallocated from a different thread than the one that created it.

Consumers must ensure a handle is not used again after it has been deallocated.
*/
#[repr(transparent)]
pub struct HandleOwned<T: ?Sized>(*mut ThreadBound<T>);

unsafe_impl!("The handle is semantically `&mut T`" => impl<T: ?Sized + Send> Send for HandleOwned<T> {});

impl<T: ?Sized + RefUnwindSafe> UnwindSafe for HandleOwned<T> {}

impl<T: ?Sized> HandleOwned<T> {
    pub(super) fn as_ptr(&self) -> *mut ThreadBound<T> {
        self.0
    }
}

impl<T: Send + 'static> HandleOwned<T> {
    pub(super) fn alloc(value: T) -> Self {
        let v = Box::new(ThreadBound::new(value));
        HandleOwned(Box::into_raw(v))
    }
}

impl<T: Send> HandleOwned<T> {
    unsafe_fn!("There are no other live references and the handle won't be used again" =>
        pub(super) fn dealloc<R>(handle: Self, f: impl FnOnce(T) -> R) -> R {
            let v = Box::from_raw(handle.0);
            f(v.into_inner())
        });
}

/*
We require thread-safety bounds on `Deref` even though they're
not _technically_ needed here (the `alloc` method protects us)
so we can catch ourselves using data in the handles that doesn't
satisfy their safety requirements
*/
impl<T: ?Sized + Send> Deref for HandleOwned<T> {
    type Target = T;

    fn deref(&self) -> &T {
        unsafe_block!("We own the interior value" => { &**self.0 })
    }
}

/*
We require thread-safety bounds on `Deref` even though they're
not _technically_ needed here (the `alloc` method protects us)
so we can catch ourselves using data in the handles that doesn't
satisfy their safety requirements
*/
impl<T: ?Sized + Send> DerefMut for HandleOwned<T> {
    fn deref_mut(&mut self) -> &mut T {
        unsafe_block!("We own the interior value" => { &mut **self.0 })
    }
}

/**
A parameter that will have a value assigned during the FFI call.
*/
pub type Out<T> = *mut T;
