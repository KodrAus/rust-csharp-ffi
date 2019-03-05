use std::{
    ops::{
        Deref,
        DerefMut,
    },
    sync::atomic::{
        AtomicUsize,
        Ordering,
    },
};

type ThreadId = usize;

static mut GLOBAL_ID: AtomicUsize = AtomicUsize::new(0);
thread_local!(static THREAD_ID: usize = next_thread_id());

fn next_thread_id() -> usize {
    unsafe { GLOBAL_ID.fetch_add(1, Ordering::SeqCst) }
}

fn get_thread_id() -> usize {
    THREAD_ID.with(|x| *x)
}

pub(super) struct ThreadBound<T: ?Sized> {
    thread_id: ThreadId,
    inner: T,
}

impl<T> ThreadBound<T> {
    pub fn new(inner: T) -> Self {
        ThreadBound {
            thread_id: get_thread_id(),
            inner,
        }
    }
}

/*
We don't need to check the thread id when moving out of the inner
value so long as the inner value is itself `Send`. This allows
the .NET runtime to potentially finalize a value on another thread.
*/
impl<T: Send> ThreadBound<T> {
    pub fn into_inner(self) -> T {
        self.inner
    }
}

impl<T: ?Sized> ThreadBound<T> {
    fn check_id(&self) {
        let current = get_thread_id();

        if self.thread_id != current {
            panic!("attempted to access resource from a different thread");
        }
    }
}

unsafe_impl!("The inner value can't actually be accessed concurrently" => impl<T: ?Sized> Send for ThreadBound<T> {});
unsafe_impl!("The inner value can't actually be accessed concurrently" => impl<T: ?Sized> Sync for ThreadBound<T> {});

impl<T: ?Sized> Deref for ThreadBound<T> {
    type Target = T;

    fn deref(&self) -> &T {
        self.check_id();
        &self.inner
    }
}

impl<T: ?Sized> DerefMut for ThreadBound<T> {
    fn deref_mut(&mut self) -> &mut T {
        self.check_id();
        &mut self.inner
    }
}
