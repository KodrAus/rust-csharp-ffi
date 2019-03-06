use crate::error::Error;

pub struct Data<P> {
    pub(crate) key: Key,
    pub(crate) payload: P,
}

pub const KEY_SIZE: usize = 16;

#[derive(Clone, Copy)]
pub struct Key([u8; KEY_SIZE]);

impl Key {
    pub(crate) fn from_vec(value: Vec<u8>) -> Result<Self, Error> {
        Self::from_slice(&value)
    }

    pub(crate) fn from_slice(value: &[u8]) -> Result<Self, Error> {
        if value.len() > KEY_SIZE {
            return Err(Error::msg(format!(
                "key length `{}` is greater than the max allowed `{}`",
                value.len(),
                KEY_SIZE
            )));
        }

        let mut bytes = [0; KEY_SIZE];
        (&mut bytes[..value.len()]).copy_from_slice(value);

        Ok(Key(bytes))
    }

    pub(crate) fn to_bytes(self) -> [u8; KEY_SIZE] {
        self.0
    }
}
