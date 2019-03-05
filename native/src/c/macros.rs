/**
Wrap an FFI function.

This macro ensures all arguments satisfy `NotNull::not_null`. It's also a simple way to work
around not having a stable catch expression yet so we can handle early returns from ffi functions.
The macro doesn't support generics or argument patterns that are more complex than simple identifiers.
*/
macro_rules! ffi {
    ($(fn $name:ident ( $( $arg_ident:ident : $arg_ty:ty),* ) -> DbResult $body:expr)*) => {
        $(
            #[no_mangle]
            pub unsafe extern "C" fn $name( $($arg_ident : $arg_ty),* ) -> DbResult {
                #[allow(unused_mut)]
                unsafe fn call( $(mut $arg_ident: $arg_ty),* ) -> DbResult {
                    if $($crate::c::is_null::IsNull::is_null(&$arg_ident)) || * {
                        return DbResult::ArgumentNull;
                    }

                    $body
                }

                DbResult::catch(move || call( $($arg_ident),* ))
            }
        )*
    };
}
