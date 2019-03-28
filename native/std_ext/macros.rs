/**
Allow a block of `unsafe` code with a reason.

The macro will expand to an `unsafe` block.
*/
macro_rules! unsafe_block {
    ($reason:tt => $body:expr) => {{
        #[allow(unsafe_code)]
        let __result = unsafe { $body };
        __result
    }};
}

/**
Allow an `unsafe` function with a reason.

The macro will expand to an `unsafe fn`.
*/
macro_rules! unsafe_fn {
    ($reason: tt => fn $name:ident $($body:tt)*) => {
        unsafe_fn!($reason => pub(self) fn $name $($body)*);
    };
    ($reason: tt => $publicity:vis fn $name:ident $($body:tt)*) => {
        #[allow(unsafe_code)]
        $publicity unsafe fn $name $($body)*
    };
}

/**
Allow an `unsafe` trait implementation with a reason.

The macro will expand to an `unsafe impl`.
*/
macro_rules! unsafe_impl {
    ($reason: tt => impl $($body:tt)*) => {
        #[allow(unsafe_code)]
        unsafe impl $($body)*
    };
}
