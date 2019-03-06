use std::{
    panic::{
        RefUnwindSafe,
        UnwindSafe,
    },
    sync::Arc,
};

use crate::{
    data::Data,
    error::Error,
    store::Store,
};

pub struct Writer {
    inner: Arc<sled::Db>,
}

impl UnwindSafe for Writer {}
impl RefUnwindSafe for Writer {}

impl Writer {
    pub(super) fn begin(store: &Store) -> Self {
        let db = store.inner.db.clone();

        Writer {
            inner: db,
        }
    }

    pub fn set(&mut self, data: Data<impl Into<Vec<u8>>>) -> Result<(), Error> {
        self.inner.set(data.key, data.payload.into()).map_err(Error::fail)?;

        Ok(())
    }

    pub fn complete(&mut self) -> Result<(), Error> {
        self.inner.flush().map_err(|_| Error::msg("failed to flush database"))?;

        Ok(())
    }
}
