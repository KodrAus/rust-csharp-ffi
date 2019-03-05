use std::{
    ops::{
        Deref,
        DerefMut,
    },
    sync::atomic::{AtomicUsize, Ordering},
};

type ThreadId = usize;

static mut COUNTER: AtomicUsize = AtomicUsize::new(0);
thread_local!(static THREAD_ID: usize = next_thread_id());

fn next_thread_id() -> usize {
    unsafe { COUNTER.fetch_add(1, Ordering::SeqCst) }
}

fn get_thread_id() -> usize {
    THREAD_ID.with(|&x| x)
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
