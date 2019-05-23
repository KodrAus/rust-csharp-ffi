# A hybrid Rust + C# example [![Build Status](https://dev.azure.com/kodraus/rust-csharp-ffi/_apis/build/status/KodrAus.rust-csharp-ffi?branchName=master)](https://dev.azure.com/kodraus/rust-csharp-ffi/_build/latest?definitionId=2&branchName=master)

This repository contains an example Rust + C# hybrid application, based on [this blog post](https://blog.getseq.net/rust-at-datalust-how-we-integrate-rust-with-csharp/). It's an ASP.NET Core web API over an embedded Rust database called [`sled`](https://github.com/spacejam/sled).

It can be run as a typical .NET application, or it can be compiled ahead of time into a single native binary for [CoreRT](https://github.com/dotnet/corert).

## Building

### Using VS Code + Docker

This repository includes a [development container](https://code.visualstudio.com/docs/remote/containers) that includes all the system dependencies needed to build and debug this project.

> Note: native compilation can be a very intensive process. If you run the dev container but hit issues with slow or cancelled builds on platforms without native Docker support, try increasing resource limits set on your Docker host.

Use the _linux-x64 lldb corert launch_ task to begin a native debugging session.

### Locally

In a local environment, this project requires:

- A recent [Rust nightly toolchain](https://rustup.rs).
- A [.NET Core SDK](https://dotnet.microsoft.com/download) supporting `netcoreapp3.0`.
- A recent [Node](https://nodejs.org) with the [Angular CLI](https://angular.io/cli).

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
    -f netcoreapp3.0 `
    -r $DOTNET_RID `
    /p:AotBuild=true

$ ./bin/Debug/netcoreapp3.0/$DOTNET_RID/publish/Db.Api
```

where `$DOTNET_RID` is a [runtime identifier](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog).

### Running the UI

```
$ npm install
$ ng serve
```

### Configuration

The web API (`Db.Api` project) accepts the following command-line arguments:

- `--datapath`: The path to use for persistent data.
- `--urls`: The urls to listen on.

## Project structure

- `/native`: Contains the native, unmanaged Rust library.
  - `/db`: The Rust storage engine implementation.
  - `/c`: The Rust C bindings to the storage engine.
- `/dotnet`: Contains the managed C# library (raw bindings and a web API built on top).
  - `/Db.Storage`: The raw bindings to the Rust library.
  - `/Db.Api`: An ASP.NET Core web API that uses the raw bindings.
- `/ui`: Contains the UI app that interacts with the web API.
- `/ci`: Contains build scripts. These are safe to run in a local environment.

The most interesting bits for FFI live in the `/native/c` and `/dotnet/Db.Storage` projects.

## Notes

The following section contains some rough notes about aspects of the sample. Some of it may be inaccurate or out-of-date! If you spot anything PRs are very welcome :)

### Building Rust with MsBuild

Calling `cargo` commands and copying native binaries is managed by MsBuild through `targets` files. Calling something like `dotnet run -p dotnet/Db.Api/Db.Api.csproj` will also execute `cargo build -p dbc`.

The `dotnet/Native.targets` file contains properties and targets that can call `cargo build` on the native library when building the managed one. It attempts to be project-agnostic. It also has compile-time constants for the target platform, and whether or not compilation is ahead-of-time (using CoreRT).

The `dotnet/Dbc.targets` file is specific for this sample. It sets some MsBuild properties that point the `cargo build` command at the right Rust package to build. Each C# project needs to import the `Dbc.targets`.

### Modeling the .NET runtime in Rust

We model the FFI on the Rust side and owned data structures are allocated in Rust's heap.

Handles in the Rust C ABI try to model the way C# _can_ interact with them rather than just how we _expect_ it to. Some considerations are:

- C# doesn't guarantee data-race freedom. Multiple threads may attempt to use the same value concurrently.
- If an unmanaged resource is not manually disposed and reaches finalization, the .NET runtime will attempt to free it from a different thread than the one that created it. The unmanaged resource will be effectively _moved_ into the finalization thread.
- C#'s `SafeHandle` can protect an unmanaged resource from being used before it's been allocated or after it's been freed.

These constraints lead to the `HandleShared` and `HandleExclusive` types that are used in the C bindings.

### Calling unmanaged code from .NET

The .NET runtime has a feature called Pinvoke for calling into, and being called from, 'unmanaged' code (like our Rust library). The base cost of calling into unmanaged code at runtime is significant.

Runtime features like garbage collection and exception handling impose requirements on running .NET code that aren't guaranteed to be upheld by unmanaged code. For that reason, when the .NET runtime encounters an unmanaged call during JIT compilation, it will generate code around it that performs some bookkeeping to make sure everything works no matter how that unmanaged code behaves.

That extra work per Pinvoke usually makes fine-grained unmanaged calls unviable. On top of the base cost of calling into unmanaged code within the .NET runtime, each argument in an unmanaged function may need special marshaling. Using only [blittable types](https://docs.microsoft.com/en-us/dotnet/framework/interop/blittable-and-non-blittable-types) like fundamental value types, pointers and simple structs can avoid that extra cost, or at least put that marshaling cost under your control.

CoreRT works a little differently. Pinvoke calls to functions that are statically linked into the binary appear to be treated like internal calls. CoreRT also has a different runtime implementation of the before-and-after bookkeeping that does a bit less work. The result is that calls to unmanaged code in CoreRT can be made more efficiently (to make things concrete, I measured it as the difference between ~2000ns and ~70ns of overhead for an unmanaged call to a function like `int Add(int, int)` locally).
