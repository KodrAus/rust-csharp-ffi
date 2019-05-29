use std::panic::{
    RefUnwindSafe,
    UnwindSafe,
};

use crate::{
    data::Data,
    error::Error,
    store::{
        Db,
        Store,
    },
};

pub struct Writer {
    db: Db,
}

impl Writer {
    pub(super) fn begin(store: &Store) -> Self {
        let db = store.db.clone();

        Writer { db }
    }

    pub fn set(&mut self, data: Data<impl Into<Vec<u8>>>) -> Result<(), Error> {
        self.db
            .set(data.key, data.payload.into())
            .map_err(Error::fail)?;

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
impl UnwindSafe for Writer {}
impl RefUnwindSafe for Writer {}
