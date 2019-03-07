# Rust + C#

This repository contains an example Rust + C# hybrid application, based on [this blog post](https://blog.getseq.net/rust-at-datalust-how-we-integrate-rust-with-csharp/). It's an ASP.NET Core web API over an embedded Rust database called [`sled`](https://github.com/spacejam/sled).

It can be run as a typical .NET application, or it can be compiled ahead of time into a single native binary for [CoreRT](https://github.com/dotnet/corert).

## Building

This project requires:

- A recent [Rust nightly toolchain](https://rustup.rs).
- A 2.2 [.NET Core SDK](https://dotnet.microsoft.com/download).

Building with CoreRT additionally requires a native C++ toolchain. See [the list of CoreRT prerequisites](https://github.com/dotnet/corert/blob/master/samples/prerequisites.md).

### Building for CoreCLR

Running the `Db.Api` project should be enough to get started:

```
$ cd dotnet/Db.Api

$ dotnet run
```

### Building for CoreRT

Passing the `AotBuild` property when publishing will use `Microsoft.DotNet.ILCompiler` to link a native binary for the given `$DOTNET_RID`:

```
$ cd dotnet/Db.Api

$ dotnet publish `
    -f netcoreapp2.2 `
    -r $DOTNET_RID `
    /p:AotBuild=true

$ ./bin/Debug/netcoreapp2.2/$DOTNET_RID/publish/Db.Api
```

where `$DOTNET_RID` is a [runtime identifier](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog).

## Notes

### Project structure

- `/native`: Contains the native, unmanaged Rust library.
- `/dotnet`: Contains the managed C# library (raw bindings and a web API built on top).
  - `/Db.Storage`: The raw bindings to the Rust library.
  - `/Db.Api`: An ASP.NET Core web API that uses the raw bindings.

### Building Rust with MsBuild

The `Native.targets` file contains properties and targets that can call `cargo build` on the native library when building the managed one. It also has compile-time constants for the target platform, and whether or not compilation is ahead-of-time. That way, `dotnet build` and `dotnet publish` can coordinate the complete managed and unmanaged build process in a single invocation.

### Calling unmanaged code in the .NET runtime

The .NET runtime has a feature called Pinvoke for calling into and being called from 'unmanaged' code (like our Rust library). Runtime features like the precise GC and exception handling impose requirements on running code that isn't guaranteed to be upheld by unmanaged code. When it encounters an unmanaged call, the runtime will generate code around it that performs some bookkeeping GC reporting.

The base cost of calling into unmanaged code at runtime makes fine-grained unmanaged calls unviable. When compiling with CoreRT though, Pinvoke calls to functions that are statically linked into the binary are treated like internal calls so can be made more efficiently (to make things concrete, I measured it as the difference between ~2000ns and ~70ns of overhead for an unmanaged call to a function like `int Add(int, int)` locally).

On top of the base cost of calling into unmanaged code from the .NET runtime, each argument may need special marshaling. Only using blittable types like integers and simple structs can avoid that cost, or at least put it under your control.

### Modeling the .NET runtime in Rust

Handles in the Rust C ABI try to model the way C# can interact with them. Some considerations are:

- C# doesn't guarantee data-race freedom. Multiple threads may attempt to use the same value concurrently.
- If an unmanaged resource reaches finalization, the .NET runtime will attempt to free them from a different thread than the one that created them.
- C# has APIs that can protect an unmanaged resource from being used after it's been freed.

These constraints lead to the following handle types:

```rust
pub struct HandleShared<T: ?Sized>(*const T);
```

A shared handle is safe for concurrent access and can be sent to other threads.

```rust
unsafe impl<T: ?Sized + Sync> Send for HandleShared<T> {}

impl<T: ?Sized + RefUnwindSafe> UnwindSafe for HandleShared<T> {}

impl<T: Send + Sync + 'static> HandleShared<T> {
    pub(super) fn alloc(value: T) -> Self { .. }
}

impl<T: Send + Sync> HandleShared<T> {
    pub(super) unsafe fn dealloc<R>(handle: Self, f: impl FnOnce(T) -> R) -> R { .. }
}
```

```rust
pub struct HandleOwned<T: ?Sized>(*mut ThreadBound<T>);
```

An owned handle is not safe for concurrent access. This is enforced by preventing the value from being accessed by any other thread than the one that created it. One subtlety is the finalization requirement, where a resource needs to be freed from a different thread.

```rust
unsafe impl<T: ?Sized + Send> Send for HandleOwned<T> {}

impl<T: ?Sized + RefUnwindSafe> UnwindSafe for HandleOwned<T> {}

impl<T: Send + 'static> HandleOwned<T> {
    pub(super) fn alloc(value: T) -> Self { .. }
}

impl<T: Send> HandleOwned<T> {
    pub(super) unsafe fn dealloc<R>(handle: Self, f: impl FnOnce(T) -> R) -> R { .. }
}
```

As an example of the kind of issues the handles catch, this is what happened when compiling after writing a `HandleOwned<DbReader>`, where `DbReader` wraps an iterator from `sled`:

```
error[E0599]: no function or associated item named `alloc` found for type `c::handle::HandleOwned<c::DbReader>` in the current scope
   --> src/c/mod.rs:155:25
    |
155 |         DbReaderHandle::alloc(DbReader {
    |         ----------------^^^^^
    |         |
    |         function or associated item not found in `c::handle::HandleOwned<c::DbReader>`
    | 
   ::: src/c/handle.rs:86:1
    |
86  | pub struct HandleOwned<T: ?Sized>(*mut ThreadBound<T>);
    | ------------------------------------------------------- function or associated item `alloc` not found for this
    |
    = note: the method `alloc` exists but the following trait bounds were not satisfied:
            `c::DbReader : std::marker::Send`
    = help: items from traits can only be used if the trait is implemented and in scope
    = note: the following traits define an item `alloc`, perhaps you need to implement one of them:
            candidate #1: `std::alloc::GlobalAlloc`
            candidate #2: `std::alloc::Alloc`
```

That wall of text tells us the `HandleOwned` didn't have an `alloc` method for a `DbReader`, because the reader didn't satisfy the `Send` requirement. Digging into why `DbReader` doesn't implement `Send` reveals some potentially thread-local state deep within the concurrency framework used by `sled`:

```
error[E0277]: `*const crossbeam_epoch::internal::Local` cannot be sent between threads safely
   --> src/c/mod.rs:174:9
    |
174 |         static_assert::is_send::<DbReaderHandle>();
    |         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ `*const crossbeam_epoch::internal::Local` cannot be sent between threads safely
    |
    = help: within `c::DbReader`, the trait `std::marker::Send` is not implemented for `*const crossbeam_epoch::internal::Local`
    = note: required because it appears within the type `crossbeam_epoch::guard::Guard`
    = note: required because it appears within the type `pagecache::tx::Tx`
    = note: required because it appears within the type `sled::iter::Iter<'static>`
    = note: required because it appears within the type `store::reader::iter::Iter`
    = note: required because it appears within the type `store::reader::Iter`
    = note: required because it appears within the type `store::reader::Reader`
    = note: required because it appears within the type `c::DbReader`
    = note: required because of the requirements on the impl of `std::marker::Send` for `c::handle::HandleOwned<c::DbReader>`
```

There are a few different approaches you could take to work around this. Since we couldn't really go change `sled` to make its iterator `Send`, we ended up with a generic `DeferredCleanup` wrapper. It's a scheme where deallocation could be deferred until the creating thread tears down or it notices there's garbage to cleanup. It's similar to `fragile::Sticky`. The `DeferredCleanup` type makes it safe to signal that a resource should be freed from a different thread, even if that resource doesn't implement `Send`. It may be a while before the resource can actually be freed, but that's only a problem if you forget to dispose a resource explicitly in C#.
