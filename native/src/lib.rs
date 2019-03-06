// Unsafe is explicitly allowed through `unsafe_*` macros
#![deny(unsafe_code)]
// For converting Rust results into FFI results
#![feature(try_trait)]

#[macro_use]
extern crate lazy_static;

#[macro_use]
extern crate rental;

#[macro_use]
mod macros;
#[macro_use]
mod std_ext;

pub mod c;
pub mod data;
pub mod error;
pub mod store;
