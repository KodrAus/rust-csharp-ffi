#![deny(unsafe_code)]
// For converting Rust results into FFI results
#![feature(try_trait)]

#[macro_use]
mod macros;
#[macro_use]
mod std_ext;

pub mod c;
pub mod db;
pub mod error;
