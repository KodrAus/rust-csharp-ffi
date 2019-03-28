// Unsafe is explicitly allowed through `unsafe_*` macros
#![deny(unsafe_code)]
// For converting Rust results into FFI results
#![feature(try_trait)]

#[macro_use]
extern crate rental;

#[macro_use]
#[path = "../../std_ext/mod.rs"]
#[allow(unused_macros)]
mod std_ext;

pub mod data;
pub mod error;
pub mod store;
