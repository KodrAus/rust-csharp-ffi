use std::{
    sync::Arc,
};

/**
A database instance.
*/
pub struct Db(Arc<sled::Db>);
