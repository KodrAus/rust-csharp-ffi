use std::{
    panic::{
        RefUnwindSafe,
        UnwindSafe,
    },
};

use crate::{
    data::Key,
    error::Error,
    store::{
        Store,
        Db,
    },
};

pub struct Deleter {
    db: Db,
}

impl Deleter {
    pub(super) fn begin(store: &Store) -> Self {
        let db = store.db.clone();

        Deleter { db }
    }

    pub fn remove(&mut self, key: Key) -> Result<(), Error> {
        self.db.del(key).map_err(Error::fail)?;

        Ok(())
    }

    pub fn complete(&mut self) -> Result<(), Error> {
        self.db
            .flush()
            .map_err(|_| Error::msg("failed to flush database"))?;

        Ok(())
    }
}

/*
NOTE: Usually, just declaring a type as unwind safe like this isn't
a great idea, especially when it contains other types you don't own.
We do this here to keep the example moving forward.

See: https://github.com/spacejam/sled/issues/662
*/
impl UnwindSafe for Deleter {}
impl RefUnwindSafe for Deleter {}
