use std::{
    panic::{
        RefUnwindSafe,
        UnwindSafe,
    },
    sync::Arc,
};

use crate::{
    data::Key,
    error::Error,
    store::Store,
};

pub struct Deleter {
    inner: Arc<sled::Db>,
}

impl UnwindSafe for Deleter {}
impl RefUnwindSafe for Deleter {}

impl Deleter {
    pub(super) fn begin(store: &Store) -> Self {
        let db = store.inner.db.clone();

        Deleter { inner: db }
    }

    pub fn remove(&mut self, key: Key) -> Result<(), Error> {
        self.inner.del(key).map_err(Error::fail)?;

        Ok(())
    }

    pub fn complete(&mut self) -> Result<(), Error> {
        self.inner
            .flush()
            .map_err(|_| Error::msg("failed to flush database"))?;

        Ok(())
    }
}
