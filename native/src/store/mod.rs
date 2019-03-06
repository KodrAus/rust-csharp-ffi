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
    /**
    Create or open a store at the given location.
    */
    pub fn open(path: impl AsRef<Path>) -> Result<Self, Error> {
        let db = sled::Db::start_default(path).map_err(Error::fail)?;

        Ok(Store {
            inner: Inner { db: Arc::new(db) },
        })
    }

    /**
    Close a store.
    */
    pub fn close(&mut self) -> Result<(), Error> {
        self.inner.db.flush().map_err(Error::fail)?;

        Ok(())
    }

    pub fn read_begin(&self) -> Result<reader::Reader, Error> {
        Ok(reader::Reader::begin(self))
    }
}
