/**
Wrap an FFI function.

This macro ensures all arguments satisfy `NotNull::not_null`. It's also a simple way to work
around not having a stable catch expression yet so we can handle early returns from ffi functions.
The macro doesn't support generics or argument patterns that are more complex than simple identifiers.

A more advanced implementation could use a procedural macro, and generate C# bindings automatically.
*/
macro_rules! ffi {
    ($(fn $name:ident ( $( $arg_ident:ident : $arg_ty:ty),* ) -> DbResult $body:expr)*) => {
        $(
            #[allow(unsafe_code, unused_attributes)]
            #[no_mangle]
            pub unsafe extern "cdecl" fn $name( $($arg_ident : $arg_ty),* ) -> DbResult {
                #[allow(unused_mut)]
                #[deny(unsafe_code)]
                fn call( $(mut $arg_ident: $arg_ty),* ) -> DbResult {
                    $(
                        if $crate::is_null::IsNull::is_null(&$arg_ident) {
                            return DbResult::argument_null().context($crate::is_null::Error { arg: stringify!($arg_ident) });
                        }
                    )*

                    $body
                }

                DbResult::catch(move || call( $($arg_ident),* ))
            }
        )*
    };
}

macro_rules! ffi_no_catch {
    ($(fn $name:ident ( $( $arg_ident:ident : $arg_ty:ty),* ) -> DbResult $body:expr)*) => {
        $(
            #[allow(unsafe_code, unused_attributes)]
            #[no_mangle]
            pub unsafe extern "cdecl" fn $name( $($arg_ident : $arg_ty),* ) -> DbResult {
                #[allow(unused_mut)]
                #[deny(unsafe_code)]
                fn call( $(mut $arg_ident: $arg_ty),* ) -> DbResult {
                    $(
                        if $crate::is_null::IsNull::is_null(&$arg_ident) {
                            return DbResult::argument_null().context($crate::is_null::Error { arg: stringify!($arg_ident) });
                        }
                    )*

                    $body
                }

                call( $($arg_ident),* )
            }
        )*
    };
}
