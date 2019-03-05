use std::{
    any::Any,
    fmt::Write,
    cell::RefCell,
    ops::Try,
    panic::{
        catch_unwind,
        UnwindSafe,
    },
};

use failure::Fail;

use crate::{
    error,
    std_ext::prelude::*,
};

thread_local! {
    static LAST_RESULT: RefCell<Option<LastResult>> = RefCell::new(None);
}

/**
The result of making a call across an FFI boundary.

The result may indicate success or an error.
If an error is returned, the thread-local `last_result` can be inspected for more details.
*/
#[repr(u32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum DbResult {
    Ok,

    Done,
    BufferTooSmall,

    ArgumentNull,
    InternalError,
}

impl DbResult {
    pub fn as_err(&self) -> Option<&'static str> {
        match *self {
            DbResult::Ok | DbResult::Done => None,
            DbResult::ArgumentNull => Some("a required argument was null"),
            DbResult::BufferTooSmall => Some("a supplied buffer was too small"),
            DbResult::InternalError => Some("an internal error occurred"),
        }
    }

    pub(super) fn catch(f: impl FnOnce() -> Self + UnwindSafe) -> Self {
        LAST_RESULT.with(|last_result| {
            {
                *last_result.borrow_mut() = None;
            }

            match catch_unwind(f) {
                Ok(db_result) => {
                    let extract_err = || db_result.as_err().map(Into::into);

                    // Always set the last result so it matches what's returned.
                    // This `Ok` branch doesn't necessarily mean the result is ok,
                    // only that there wasn't a panic.
                    last_result
                        .borrow_mut()
                        .map_mut(|last_result| {
                            last_result.value = db_result;
                            last_result.err.or_else_mut(extract_err);
                        })
                        .get_or_insert_with(|| LastResult {
                            value: db_result,
                            err: extract_err(),
                        })
                        .value
                }
                Err(e) => {
                    let extract_panic =
                        || extract_panic(&e).map(|s| format!("internal panic with '{}'", s));

                    // Set the last error to the panic message if it's not already set
                    last_result
                        .borrow_mut()
                        .map_mut(|last_result| {
                            last_result.err.or_else_mut(extract_panic);
                        })
                        .get_or_insert_with(|| LastResult {
                            value: DbResult::InternalError,
                            err: extract_panic(),
                        })
                        .value
                }
            }
        })
    }

    pub(super) fn with_last_result<R>(f: impl Fn(Option<(DbResult, Option<&str>)>) -> R) -> R {
        LAST_RESULT.with(|last_result| {
            let last_result = last_result.borrow();

            let last_result = last_result.as_ref().map(|last_result| {
                let msg = last_result.err.as_ref().map(|msg| msg.as_ref());

                (last_result.value, msg)
            });

            f(last_result)
        })
    }
}

/**
Map error types that are convertible into `Error` into `DbResult`s.

This is so we can use `?` on `Result<T, E: Into<error::Error>>` in FFI functions.
The error state will be serialized and stored in a thread-local that can be queried later.
*/
impl<E> From<E> for DbResult
where
    E: Into<error::Error> + Fail,
{
    fn from(e: E) -> Self {
        let err = Some(format_error(&e));
        let db_result = e.into().into_db_result();

        LAST_RESULT.with(|last_result| {
            *last_result.borrow_mut() = Some(LastResult {
                value: db_result,
                err,
            });
        });

        db_result
    }
}

/**
Allow carrying standard `Result`s as `DbResult`s.
*/
impl Try for DbResult {
    type Ok = Self;
    type Error = Self;

    fn into_result(self) -> Result<<Self as Try>::Ok, <Self as Try>::Error> {
        match self {
            DbResult::Ok | DbResult::Done => Ok(self),
            _ => Err(self),
        }
    }

    fn from_error(result: Self::Error) -> Self {
        if result.as_err().is_none() {
            panic!(format!(
                "attempted to return success code `{:?}` as an error",
                result
            ));
        }

        result
    }

    fn from_ok(result: <Self as Try>::Ok) -> Self {
        if result.as_err().is_some() {
            panic!(format!(
                "attempted to return error code `{:?}` as success",
                result
            ));
        }

        result
    }
}

impl error::Error {
    fn into_db_result(self) -> DbResult {
        DbResult::InternalError
    }
}

struct LastResult {
    value: DbResult,
    err: Option<String>,
}

fn format_error(err: &dyn Fail) -> String {
    let mut error_string = String::new();

    let mut causes = err.iter_causes();
    if let Some(cause) = causes.next() {
        let _ = writeln!(error_string, "{}.", cause);
    }

    let mut next = causes.next();
    while next.is_some() {
        let cause = next.unwrap();
        let _ = writeln!(error_string, "   caused by: {}", cause);
        next = causes.next();
    }

    if let Some(backtrace) = err.backtrace() {
        let _ = writeln!(error_string, "backtrace: {}", backtrace);
    }

    error_string
}

fn extract_panic(err: &Box<dyn Any + Send + 'static>) -> Option<String> {
    if let Some(err) = err.downcast_ref::<String>() {
        Some(err.clone())
    } else if let Some(err) = err.downcast_ref::<&'static str>() {
        Some((*err).to_owned())
    } else {
        None
    }
}

#[cfg(test)]
mod tests {
    use failure_derive::*;

    use super::*;

    #[derive(Debug, Fail)]
    enum TestInnerError {
        #[fail(display = "an inner error message")]
        Variant,
    }

    #[derive(Debug, Fail)]
    enum TestError {
        #[fail(display = "an error message")]
        Variant(#[cause] TestInnerError),
    }

    impl From<TestError> for error::Error {
        fn from(err: TestError) -> error::Error {
            error::Error::from_fail(err)
        }
    }

    #[test]
    fn last_result_catch_ok() {
        let result = DbResult::catch(|| DbResult::Ok);

        assert_eq!(DbResult::Ok, result);

        DbResult::with_last_result(|last_result| {
            assert_eq!(Some((DbResult::Ok, None)), last_result);
        });
    }

    #[test]
    fn last_result_catch_err_carrier() {
        let result = DbResult::catch(|| {
            Err(TestError::Variant(TestInnerError::Variant))?;

            unreachable!()
        });

        assert_eq!(DbResult::InternalError, result);

        DbResult::with_last_result(|last_result| {
            assert_match!(Some((result, err)) = last_result => {
                assert_eq!(DbResult::InternalError, result);

                assert!(err.is_some());
            });
        });
    }

    #[test]
    fn last_result_catch_err_return() {
        let result = DbResult::catch(|| DbResult::ArgumentNull);

        assert_eq!(DbResult::ArgumentNull, result);

        DbResult::with_last_result(|last_result| {
            assert_match!(Some((result, err)) = last_result => {
                assert_eq!(DbResult::ArgumentNull, result);

                assert!(err.is_some());
            });
        });
    }

    #[test]
    fn last_result_catch_panic() {
        let result = DbResult::catch(|| panic!("something didn't work"));

        assert_eq!(DbResult::InternalError, result);

        DbResult::with_last_result(|last_result| {
            assert_match!(Some((result, err)) = last_result => {
                assert_eq!(DbResult::InternalError, result);

                assert!(err.is_some());
            });
        });
    }
}
