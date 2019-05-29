use std::{
    ops::{
        Deref,
        DerefMut,
    },
    panic::{
        RefUnwindSafe,
        UnwindSafe,
    },
    ptr,
    slice,
};

use super::{
    is_null::IsNull,
    thread_bound::ThreadBound,
};

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

unsafe_impl!("The handle is semantically `&T`" => impl<T: ?Sized> Send for HandleShared<T> where for<'a> &'a T: Send {});
unsafe_impl!("The handle is semantically `&T`" => impl<T: ?Sized> Sync for HandleShared<T> where for<'a> &'a T: Sync {});

impl<T: ?Sized + RefUnwindSafe> UnwindSafe for HandleShared<T> {}

impl<T> HandleShared<T>
where
    HandleShared<T>: Send + Sync,
{
    pub(super) fn alloc(value: T) -> Self
    where
        T: 'static,
    {
        let v = Box::new(value);
        HandleShared(Box::into_raw(v))
    }

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
impl<T: ?Sized> Deref for HandleShared<T>
where
    HandleShared<T>: Send + Sync,
{
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
pub struct HandleExclusive<T: ?Sized>(*mut ThreadBound<T>);

unsafe_impl!("The handle is semantically `&mut T`" => impl<T: ?Sized> Send for HandleExclusive<T> where for<'a> &'a mut T: Send {});
unsafe_impl!("The handle uses `ThreadBound` for synchronization" => impl<T: ?Sized> Sync for HandleExclusive<T> where ThreadBound<T>: Sync {});

impl<T: ?Sized + RefUnwindSafe> UnwindSafe for HandleExclusive<T> {}

impl<T> HandleExclusive<T>
where
    HandleExclusive<T>: Send + Sync,
{
    pub(super) fn alloc(value: T) -> Self
    where
        T: 'static,
    {
        let v = Box::new(ThreadBound::new(value));
        HandleExclusive(Box::into_raw(v))
    }

    unsafe_fn!("There are no other live references and the handle won't be used again" =>
        pub(super) fn dealloc<R>(handle: Self, f: impl FnOnce(T) -> R) -> R
        where
            T: Send,
        {
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
impl<T: ?Sized> Deref for HandleExclusive<T>
where
    HandleExclusive<T>: Send + Sync,
{
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
impl<T: ?Sized> DerefMut for HandleExclusive<T>
where
    HandleExclusive<T>: Send,
{
    fn deref_mut(&mut self) -> &mut T {
        unsafe_block!("We own the interior value" => { &mut **self.0 })
    }
}

/**
An initialized parameter passed by shared reference.
*/
#[repr(transparent)]
pub struct Ref<T: ?Sized>(*const T);

impl<T: ?Sized> Ref<T> {
    unsafe_fn!("The pointer must be nonnull and will remain valid" => pub fn as_ref(&self) -> &T {
        &*self.0
    });
}

impl Ref<u8> {
    unsafe_fn!("The pointer must be nonnull, the length is correct, and will remain valid" => pub fn as_bytes(&self, len: usize) -> &[u8] {
        slice::from_raw_parts(self.0, len)
    });
}

/**
An initialized parameter passed by exclusive reference.
*/
#[repr(transparent)]
pub struct RefMut<T: ?Sized>(*mut T);

impl<T: ?Sized> RefMut<T> {
    unsafe_fn!("The pointer must be nonnull and will remain valid" => pub fn as_mut(&mut self) -> &mut T {
        &mut *self.0
    });
}

impl RefMut<u8> {
    unsafe_fn!("The pointer must be nonnull, the length is correct, and will remain valid" => pub fn as_bytes_mut(&mut self, len: usize) -> &mut [u8] {
        slice::from_raw_parts_mut(self.0, len)
    });
}

/**
An uninitialized, assignable out parameter.

The inner value is not guaranteed to be initialized.
*/
#[repr(transparent)]
pub struct Out<T: ?Sized>(*mut T);

impl<T> Out<T> {
    unsafe_fn!("The pointer must be nonnull and valid for writes" => pub fn assign(&mut self, value: T) {
        ptr::write(self.0, value);
    });

    unsafe_fn!("The pointer must be nonnull, not overlap the slice, must be valid for the length of the slice, and valid for writes" => pub fn assign_slice(&mut self, value: &[T]) {
        ptr::copy_nonoverlapping(value.as_ptr(), self.0, value.len());
    });
}

impl Out<u8> {
    unsafe_fn!("The slice must never be read from" => pub fn as_uninit_bytes_mut(&mut self, len: usize) -> &mut [u8] {
        slice::from_raw_parts_mut(self.0, len)
    });
}

impl<T: ?Sized> IsNull for HandleExclusive<T> {
    fn is_null(&self) -> bool {
        self.0.is_null()
    }
}

impl<T: ?Sized + Sync> IsNull for HandleShared<T> {
    fn is_null(&self) -> bool {
        self.0.is_null()
    }
}

impl<T: ?Sized> IsNull for Ref<T> {
    fn is_null(&self) -> bool {
        self.0.is_null()
    }
}

impl<T: ?Sized + Sync> IsNull for RefMut<T> {
    fn is_null(&self) -> bool {
        self.0.is_null()
    }
}

impl<T: ?Sized> IsNull for Out<T> {
    fn is_null(&self) -> bool {
        self.0.is_null()
    }
}
