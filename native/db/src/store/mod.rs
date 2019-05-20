use std::{
    panic::{
        RefUnwindSafe,
        UnwindSafe,
    },
    path::Path,
    sync::Arc,
};

use crate::error::Error;

pub mod deleter;
pub mod reader;
pub mod writer;

/**
A database instance.
*/
pub struct Store {
    db: Db,
}

impl Store {
    pub fn open(path: impl AsRef<Path>) -> Result<Self, Error> {
        let db = sled::Db::start_default(path).map_err(Error::fail)?;

        Ok(Store {
            db: Db::new(db),
        })
    }

    pub fn close(&mut self) -> Result<(), Error> {
        self.db.flush().map_err(Error::fail)?;

        Ok(())
    }

    pub fn read_begin(&self) -> Result<reader::Reader, Error> {
        Ok(reader::Reader::begin(self))
    }

    pub fn write_begin(&self) -> Result<writer::Writer, Error> {
        Ok(writer::Writer::begin(self))
    }

    pub fn delete_begin(&self) -> Result<deleter::Deleter, Error> {
        Ok(deleter::Deleter::begin(self))
    }
}

type Db = Arc<sled::Db>;

/*
NOTE: Usually, just declaring a type as unwind safe like this isn't
a great idea, especially when it contains other types you don't own.
We do this here to keep the example moving forward.

See: https://github.com/spacejam/sled/issues/662
*/
impl UnwindSafe for Store {}
impl RefUnwindSafe for Store {}
