/*!
Standard library extensions.
*/

#[cfg(test)]
#[macro_use]
pub(crate) mod test;

pub(crate) mod option;

pub(crate) mod prelude {
    #![allow(unused_imports)]

    pub(crate) use super::option::*;
}
