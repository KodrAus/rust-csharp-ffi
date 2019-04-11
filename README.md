# A hybrid Rust + C# example [![Build Status](https://dev.azure.com/kodraus/rust-csharp-ffi/_apis/build/status/KodrAus.rust-csharp-ffi?branchName=master)](https://dev.azure.com/kodraus/rust-csharp-ffi/_build/latest?definitionId=2&branchName=master)

This repository contains an example Rust + C# hybrid application, based on [this blog post](https://blog.getseq.net/rust-at-datalust-how-we-integrate-rust-with-csharp/). It's an ASP.NET Core web API over an embedded Rust database called [`sled`](https://github.com/spacejam/sled).

It can be run as a typical .NET application, or it can be compiled ahead of time into a single native binary for [CoreRT](https://github.com/dotnet/corert).

## Building

This project requires:

- A recent [Rust nightly toolchain](https://rustup.rs).
- A [.NET Core SDK](https://dotnet.microsoft.com/download) supporting `netcoreapp2.2`.

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
  - `/db`: The Rust storage engine implementation.
  - `/c`: The Rust C bindings to the storage engine.
- `/dotnet`: Contains the managed C# library (raw bindings and a web API built on top).
  - `/Db.Storage`: The raw bindings to the Rust library.
  - `/Db.Api`: An ASP.NET Core web API that uses the raw bindings.

The most interesting bits for FFI live in the `/native/c` and `/dotnet/Db.Storage` projects.

### Building Rust with MsBuild

The `dotnet/Native.targets` file contains properties and targets that can call `cargo build` on the native library when building the managed one. It attempts to be project-agnostic. It also has compile-time constants for the target platform, and whether or not compilation is ahead-of-time. That way, `dotnet build` and `dotnet publish` can coordinate the complete managed and unmanaged build process in a single invocation.

The `dotnet/Dbc.targets` file is specific for this sample.

### Calling unmanaged code from .NET

The .NET runtime has a feature called Pinvoke for calling into and being called from 'unmanaged' code (like our Rust library). Runtime features like garbage collection and exception handling impose requirements on code within the runtime that isn't guaranteed to be upheld by unmanaged code. When it encounters an unmanaged call, the runtime will generate code around it that performs some bookkeeping to make sure everything works.

The base cost of calling into unmanaged code at runtime is significant. That usually makes fine-grained unmanaged calls unviable. When compiling with CoreRT though, Pinvoke calls to functions that are statically linked into the binary appear to be treated like internal calls so can be made more efficiently (to make things concrete, I measured it as the difference between ~2000ns and ~70ns of overhead for an unmanaged call to a function like `int Add(int, int)` locally).

On top of the base cost of calling into unmanaged code within the .NET runtime, each argument in an unmanaged function may need special marshaling. Using only [blittable types](https://docs.microsoft.com/en-us/dotnet/framework/interop/blittable-and-non-blittable-types) like integers and simple structs can avoid that extra cost, or at least put marshaling under your control.

### Modeling the .NET runtime in Rust

Handles in the Rust C ABI try to model the way C# can interact with them. Some considerations are:

- C# doesn't guarantee data-race freedom. Multiple threads may attempt to use the same value concurrently.
- If an unmanaged resource reaches finalization, the .NET runtime will attempt to free them from a different thread than the one that created them.
- C# has APIs that can protect an unmanaged resource from being used after it's been freed.

These constraints lead to the `HandleShared` and `HandleOwned` wrappers.
