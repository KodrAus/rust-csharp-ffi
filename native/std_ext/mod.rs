/*!
Standard library extensions.

These modules don't exist as a separate crate
so the API can remain private.
*/

#![allow(dead_code)]

#[macro_use]
mod macros;

#[cfg(test)]
#[macro_use]
pub(crate) mod test;

pub(crate) mod option;

pub(crate) mod prelude {
    #![allow(unused_imports)]

    pub(crate) use super::option::*;
}
