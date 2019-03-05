use std::fmt::{
    Debug,
    Display,
};

use failure::{
    self,
    err_msg,
    Fail,
};

/**
An error encountered while working with a database.
*/
#[derive(Debug, Fail)]
#[fail(display = "error using a db")]
pub struct Error(#[cause] failure::Error);

impl Error {
    pub(crate) fn fail(err: impl Fail) -> Self {
        Error(err.into())
    }

    pub(crate) fn msg(msg: impl Display + Debug + Sync + Send + 'static) -> Self {
        Error::fail(err_msg(msg).compat())
    }
}
