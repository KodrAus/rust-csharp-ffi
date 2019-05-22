use std::io::Read;

use failure_derive::*;

use crate::{
    DbKey,
    DbResult,
};

use db::data::Data;

#[derive(Debug, Fail)]
pub(super) enum Error {
    #[fail(display = "received a zero-sized buffer to read into")]
    ZeroSizedBuf,
}

/// Read an event payload into a given buffer.
///
/// If the event payload doesn't fit then `DbResult::BufferTooSmall` will be returned
/// and `actual_value_len` will contain the minimum size of the buffer needed.
pub(super) fn into_fixed_buffer(
    data: &mut Data<impl Read>,
    buf: &mut [u8],
    key: &mut DbKey,
    actual_value_len: &mut usize,
) -> DbResult {
    // A zero-sized input buffer will cause an infinite loop below
    // if we let it through.
    if buf.len() == 0 {
        Err(Error::ZeroSizedBuf)?;
    }

    let mut written = 0;
    let mut head = 0;

    'read: loop {
        let buf = &mut buf[head..];

        // If the buffer is full, continue writing over the previous data
        // This lets us figure out the actual payload size to return
        if buf.len() == 0 {
            head = 0;
            continue 'read;
        }

        match data.payload.read(buf)? {
            // The complete payload has been read, break and return
            0 => break 'read,
            // Continue reading the payload
            n => {
                written += n;
                head += n;
            }
        }
    }

    // If we wrote more bytes than the buffer could fit, return the required size
    if written > buf.len() {
        *actual_value_len = written;

        DbResult::BufferTooSmall
    // The entire payload fit in the buffer
    } else {
        *actual_value_len = written;
        *key = DbKey(data.key.to_bytes());

        DbResult::Ok
    }
}
