use std::{
    io::{
        self,
        Cursor,
        Read,
    },
    panic::{
        RefUnwindSafe,
        UnwindSafe,
    },
};

use crate::{
    data::{
        Data,
        Key,
    },
    error::Error,
    store::{
        Store,
        Db,
    },
};

pub struct Reader {
    iter: Iter,
    current: Option<Data<RawPayload>>,
}

impl Reader {
    pub(super) fn begin(store: &Store) -> Self {
        let db = store.db.clone();

        Reader {
            iter: Iter::new(db),
            current: None,
        }
    }

    pub fn with_current<R>(&mut self, f: impl FnOnce(Data<Payload>) -> R) -> Option<R> {
        if let Some(ref current) = self.current {
            let r = f(Data {
                key: current.key,
                payload: Payload::new(current.payload.clone()),
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
            self.current = None;
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
            db: Db,
            iter: sled::Iter<'db>,
        }
    }
}

struct Iter(iter::Iter);

impl Iter {
    fn new(db: Db) -> Self {
        Iter(iter::Iter::new(db, |db| db.iter()))
    }

    fn next(&mut self) -> Result<Option<Data<RawPayload>>, Error> {
        let kv = self
            .0
            .rent_mut(|iter| iter.next())
            .transpose()
            .map_err(Error::fail)?;

        if let Some((k, v)) = kv {
            let data = Data {
                key: Key::from_vec(k)?,
                payload: RawPayload(v),
            };

            Ok(Some(data))
        } else {
            Ok(None)
        }
    }
}

/*
NOTE: Usually, just declaring a type as unwind safe like this isn't
a great idea, especially when it contains other types you don't own.
We do this here to keep the example moving forward.

See: https://github.com/spacejam/sled/issues/662
*/
impl UnwindSafe for Iter {}
impl RefUnwindSafe for Iter {}

pub struct Payload(Cursor<RawPayload>);

#[derive(Clone)]
struct RawPayload(sled::IVec);

impl AsRef<[u8]> for RawPayload {
    fn as_ref(&self) -> &[u8] {
        &*self.0
    }
}

impl Payload {
    fn new(value: RawPayload) -> Self {
        Payload(Cursor::new(value))
    }
}

impl Read for Payload {
    fn read(&mut self, buf: &mut [u8]) -> io::Result<usize> {
        self.0.read(buf)
    }
}
