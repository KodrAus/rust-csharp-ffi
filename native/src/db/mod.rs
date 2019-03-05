use std::{
    panic::RefUnwindSafe,
    path::Path,
};

use crate::error::Error;

struct Inner {
    db: sled::Db,
}

impl RefUnwindSafe for Inner {}

/**
A database instance.
*/
pub struct Db {
    inner: Inner,
}

impl Db {
    /**
    Create or open a store at the given location.
    */
    pub fn open(path: impl AsRef<Path>) -> Result<Self, Error> {
        let db = sled::Db::start_default(path).map_err(Error::fail)?;

        Ok(Db {
            inner: Inner { db },
        })
    }

    /**
    Close a store.
    */
    pub fn close(self) -> Result<(), Error> {
        self.inner.db.flush().map_err(Error::fail)?;

        Ok(())
    }
}
