use crate::error::Error;

pub struct Data<P> {
    pub key: Key,
    pub payload: P,
}

pub const KEY_SIZE: usize = 16;

#[derive(Clone, Copy)]
pub struct Key([u8; KEY_SIZE]);

impl Key {
    pub fn from_bytes(value: [u8; KEY_SIZE]) -> Self {
        Key(value)
    }

    pub fn from_vec(value: Vec<u8>) -> Result<Self, Error> {
        Self::from_slice(&value)
    }

    pub fn from_slice(value: &[u8]) -> Result<Self, Error> {
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

    pub fn to_bytes(self) -> [u8; KEY_SIZE] {
        self.0
    }
}

impl AsRef<[u8]> for Key {
    fn as_ref(&self) -> &[u8] {
        &self.0
    }
}
