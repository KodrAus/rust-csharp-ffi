use std::{
    panic::{
        RefUnwindSafe,
        UnwindSafe,
    },
    path::Path,
    sync::Arc,
};

use crate::error::Error;

pub mod reader;
pub mod writer;
pub mod deleter;

#[derive(Clone)]
struct Inner {
    db: Arc<sled::Db>,
}

impl UnwindSafe for Inner {}
impl RefUnwindSafe for Inner {}

/**
A database instance.
*/
pub struct Store {
    inner: Inner,
}

impl Store {
    pub fn open(path: impl AsRef<Path>) -> Result<Self, Error> {
        let db = sled::Db::start_default(path).map_err(Error::fail)?;

        Ok(Store {
            inner: Inner { db: Arc::new(db) },
        })
    }

    pub fn close(&mut self) -> Result<(), Error> {
        self.inner.db.flush().map_err(Error::fail)?;

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
