use std::{
    io::{
        self,
        Read,
    },
    panic::{
        RefUnwindSafe,
        UnwindSafe,
    },
    sync::Arc,
};

use crate::{
    data::{
        Data,
        Key,
    },
    error::Error,
    store::Store,
};

pub struct Reader {
    iter: Iter,
    current: Option<Data<Payload>>,
}

#[derive(Clone)]
pub struct Payload(sled::IVec);

impl Read for Payload {
    fn read(&mut self, buf: &mut [u8]) -> io::Result<usize> {
        unimplemented!("implement a reader")
    }
}

impl Reader {
    pub(super) fn begin(store: &Store) -> Self {
        let db = store.inner.db.clone();

        Reader {
            iter: Iter::new(db),
            current: None,
        }
    }

    pub fn with_current<R>(&mut self, f: impl FnOnce(Data<Payload>) -> R) -> Option<R> {
        if let Some(ref current) = self.current {
            let r = f(Data {
                key: current.key,
                payload: current.payload.clone(),
            });

            Some(r)
        } else {
            None
        }
    }

    pub fn move_next(&mut self) -> Result<bool, Error> {
        if let Some(next) = self.iter.next()? {
            self.current = Some(next);
            Ok(true)
        } else {
            Ok(false)
        }
    }

    pub fn complete(&mut self) -> Result<(), Error> {
        Ok(())
    }
}

rental! {
    mod iter {
        use super::*;

        #[rental]
        pub(super) struct Iter {
            db: Arc<sled::Db>,
            iter: sled::Iter<'db>,
        }
    }
}

struct Iter(iter::Iter);

impl UnwindSafe for Iter {}
impl RefUnwindSafe for Iter {}

impl Iter {
    fn new(db: Arc<sled::Db>) -> Self {
        Iter(iter::Iter::new(db, |db| db.iter()))
    }

    fn next(&mut self) -> Result<Option<Data<Payload>>, Error> {
        let kv = self
            .0
            .rent_mut(|iter| iter.next())
            .transpose()
            .map_err(Error::fail)?;

        if let Some((k, v)) = kv {
            let key = Key::from_vec(k)?;

            let data = Data {
                key,
                payload: Payload(v),
            };

            Ok(Some(data))
        } else {
            Ok(None)
        }
    }
}
