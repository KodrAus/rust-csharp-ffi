use failure::{self, Fail};

#[derive(Debug, Fail)]
#[fail(display = "error using a db")]
pub struct Error(#[cause] failure::Error);

impl Error {
    pub(crate) fn from_fail(err: impl Fail) -> Self {
        Error(err.into())
    }
}
