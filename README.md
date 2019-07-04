# A hybrid Rust + C# example [![Build Status](https://dev.azure.com/kodraus/rust-csharp-ffi/_apis/build/status/KodrAus.rust-csharp-ffi?branchName=master)](https://dev.azure.com/kodraus/rust-csharp-ffi/_build/latest?definitionId=2&branchName=master)

This repository contains an example Rust + C# hybrid application, based on [this blog post](https://blog.getseq.net/rust-at-datalust-how-we-integrate-rust-with-csharp/) and discussed [in this session from NDC 2019 (YouTube)](https://www.youtube.com/watch?v=0B1U3fVCIX0). It's an ASP.NET Core web API over an embedded Rust database called [`sled`](https://github.com/spacejam/sled).

It can be run as a typical .NET application, or it can be compiled ahead of time into a single native binary for [CoreRT](https://github.com/dotnet/corert).

## Contents

- [Getting started](#getting-started)
- [Building](#building)
- [Debugging](#debugging)
- [Project structure](#project-structure)
- [Notes](#notes)

## Getting started

### Using VS Code + Docker

This repository includes a [development container](https://code.visualstudio.com/docs/remote/containers) that includes all the system dependencies needed to build and debug.

Use the _coreclr watch_ and _ng watch_ tasks to run the UI and API projects. The UI will listen on `localhost:4200` and the API will listen on `localhost:5000`.

Use the _linux-x64 lldb corert launch_ task to begin a native debugging session.

> Note: native compilation can be a very intensive process. If you run the dev container but hit issues with slow or cancelled builds on platforms without native Docker support, try increasing resource limits set on your Docker host.

### Locally

In a local environment, this project requires:

- A recent [Rust nightly toolchain](https://rustup.rs).
- A [.NET Core SDK](https://dotnet.microsoft.com/download) supporting `netcoreapp3.0`.
- A recent [Node](https://nodejs.org) with the [Angular CLI](https://angular.io/cli).

Building with CoreRT additionally requires a native C++ toolchain. See [the list of CoreRT prerequisites](https://github.com/dotnet/corert/blob/master/samples/prerequisites.md).

## Building

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

## Debugging

Since this codebase contains both managed and unmanaged code we've got a few options for debugging. Each has a corresponding task for VS Code:

- `coreclr launch`: Debug the CoreCLR runtime using the managed debugger. We get the best C# debugging experience, but no visibility into Rust.
- `linux-x64 lldb coreclr launch`: Debug the CoreCLR runtime using LLDB + SOS. We get a better Rust debugging experience, but have to use specific commands from the SOS plugin in LLDB to make sense of the JIT'd managed code.
- `linux-x64 lldb corert launch`: Debug the CoreRT runtime using LLDB. This gives us the best of both worlds so both Rust and C# can be natively debugged using LLDB.

### Notes for `linux-x64 lldb coreclr launch`

When debugging CoreCLR using LLDB, we need [a plugin](https://github.com/dotnet/diagnostics#using-sos) to make sense of managed code. This plugin ships with Windows and Linux by default. A few handy commands:

#### `bpmd`

Set a breakpoint in managed code.

<details>
    
```
bpmd /workspaces/rust-csharp-ffi/dotnet/Db.Storage/Store.cs:25

MethodDesc = 00007FFF7EDF3050
Setting breakpoint: breakpoint set --address 0x00007FFF7E461907 [Db.Storage.Store.Open(System.String)]
Setting breakpoint: breakpoint set --address 0x00007FFF7E46191E [Db.Storage.Store.Open(System.String)]
Adding pending breakpoints...
```

</details>


#### `clrstack`

Get a backtrace of managed calls.

<details>
    
```
clrstack

OS Thread Id: 0x4e41 (1)
        Child SP               IP Call Site
00007FFFFFFFD040 00007ffbdb889664 [InlinedCallFrame: 00007fffffffd040] Db.Storage.Native.Bindings._db_store_open(IntPtr, UIntPtr, Db.Storage.Native.StoreHandle ByRef)
00007FFFFFFFD040 00007fff7e461ac1 [InlinedCallFrame: 00007fffffffd040] Db.Storage.Native.Bindings._db_store_open(IntPtr, UIntPtr, Db.Storage.Native.StoreHandle ByRef)
00007FFFFFFFD030 00007FFF7E461AC1 ILStubClass.IL_STUB_PInvoke(IntPtr, UIntPtr, Db.Storage.Native.StoreHandle ByRef)
00007FFFFFFFD0E0 00007FFF7E4619C5 Db.Storage.Native.Bindings.db_store_open(IntPtr, UIntPtr, Db.Storage.Native.StoreHandle ByRef, Boolean) [/workspaces/rust-csharp-ffi/dotnet/Db.Storage/Native/Bindings.cs @ 42]
00007FFFFFFFD130 00007FFF7E461903 Db.Storage.Store.Open(System.String) [/workspaces/rust-csharp-ffi/dotnet/Db.Storage/Store.cs @ 23]
00007FFFFFFFD1D0 00007FFF7E45E09A Db.Api.Startup.ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection) [/workspaces/rust-csharp-ffi/dotnet/Db.Api/Startup.cs @ 33]
00007FFFFFFFD588 00007ffff63054af [HelperMethodFrame_PROTECTOBJ: 00007fffffffd588] System.RuntimeMethodHandle.InvokeMethod(System.Object, System.Object[], System.Signature, Boolean, Boolean)
00007FFFFFFFD700 00007FFF7D34D6E4 System.Reflection.RuntimeMethodInfo.Invoke(System.Object, System.Reflection.BindingFlags, System.Reflection.Binder, System.Object[], System.Globalization.CultureInfo)
00007FFFFFFFD750 00007FFF7D97D29E Microsoft.AspNetCore.Hosting.Internal.ConfigureServicesBuilder.InvokeCore(System.Object, Microsoft.Extensions.DependencyInjection.IServiceCollection)
00007FFFFFFFD7A0 00007FFF7D9899DE Microsoft.AspNetCore.Hosting.Internal.ConfigureServicesBuilder+<>c__DisplayClass9_0.<Invoke>g__Startup|0(Microsoft.Extensions.DependencyInjection.IServiceCollection)
00007FFFFFFFD7B0 00007FFF7D98D056 Microsoft.AspNetCore.Hosting.Internal.StartupLoader+ConfigureServicesDelegateBuilder`1+<>c__DisplayClass15_0[[System.__Canon, System.Private.CoreLib]].<BuildStartupServicesFilterPipeline>g__RunPipeline|0(Microsoft.Extensions.DependencyInjection.IServiceCollection)
00007FFFFFFFD7F0 00007FFF7D97D168 Microsoft.AspNetCore.Hosting.Internal.ConfigureServicesBuilder.Invoke(System.Object, Microsoft.Extensions.DependencyInjection.IServiceCollection)
00007FFFFFFFD820 00007FFF7D98999E Microsoft.AspNetCore.Hosting.Internal.ConfigureServicesBuilder+<>c__DisplayClass8_0.<Build>b__0(Microsoft.Extensions.DependencyInjection.IServiceCollection)
00007FFFFFFFD830 00007FFF7D98CEB8 Microsoft.AspNetCore.Hosting.Internal.StartupLoader+ConfigureServicesDelegateBuilder`1+<>c__DisplayClass14_0[[System.__Canon, System.Private.CoreLib]].<ConfigureServices>g__ConfigureServicesWithContainerConfiguration|0(Microsoft.Extensions.DependencyInjection.IServiceCollection)
00007FFFFFFFD870 00007FFF7D9817CB Microsoft.AspNetCore.Hosting.Internal.ConventionBasedStartup.ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection)
00007FFFFFFFD890 00007FFF7D9808A2 Microsoft.AspNetCore.Hosting.Internal.WebHost.EnsureApplicationServices()
00007FFFFFFFD8B0 00007FFF7D98078D Microsoft.AspNetCore.Hosting.Internal.WebHost.Initialize()
00007FFFFFFFD8E0 00007FFF7D9798B9 Microsoft.AspNetCore.Hosting.WebHostBuilder.Build()
00007FFFFFFFD930 00007FFF7D775BEA Db.Api.Program.Main(System.String[]) [/workspaces/rust-csharp-ffi/dotnet/Db.Api/Program.cs @ 23]
00007FFFFFFFDC68 00007ffff63054af [GCFrame: 00007fffffffdc68] 
00007FFFFFFFE150 00007ffff63054af [Frame: 00007fffffffe150] 
```
    
</details>

#### `clru`

Annotate the JIT'd code for a managed frame with its original source.

<details>
    
```
clru 00007FFF7E461903

Normal JIT generated code
Db.Storage.Store.Open(System.String)
Begin 00007FFF7E4617C0, size 19c

/workspaces/rust-csharp-ffi/dotnet/Db.Storage/Store.cs @ 15:
00007fff7e4617c0 55                   push    rbp
00007fff7e4617c1 4155                 push    r13
00007fff7e4617c3 4881ec88000000       sub     rsp, 0x88
00007fff7e4617ca 488dac2490000000     lea     rbp, [rsp + 0x90]
00007fff7e4617d2 4c8bef               mov     r13, rdi
00007fff7e4617d5 488d7d80             lea     rdi, [rbp - 0x80]
00007fff7e4617d9 b91c000000           mov     ecx, 0x1c
00007fff7e4617de 33c0                 xor     eax, eax
00007fff7e4617e0 f3                   rep     
00007fff7e4617e1 ab                   stosd   dword ptr es:[rdi], eax
00007fff7e4617e2 498bfd               mov     rdi, r13
00007fff7e4617e5 48897df0             mov     qword ptr [rbp - 0x10], rdi
00007fff7e4617e9 48b8b02ddf7eff7f0000 movabs  rax, 0x7fff7edf2db0
00007fff7e4617f3 833800               cmp     dword ptr [rax], 0x0
00007fff7e4617f6 7405                 je      0x7fff7e4617fd
00007fff7e4617f8 e89396e177           call    0x7ffff627ae90 (JitHelp: CORINFO_HELP_DBG_IS_JUST_MY_CODE)
00007fff7e4617fd 90                   nop     

/workspaces/rust-csharp-ffi/dotnet/Db.Storage/Store.cs @ 16:
00007fff7e4617fe 48837df000           cmp     qword ptr [rbp - 0x10], 0x0
00007fff7e461803 0f94c0               sete    al
00007fff7e461806 0fb6c0               movzx   eax, al
00007fff7e461809 8945e4               mov     dword ptr [rbp - 0x1c], eax
00007fff7e46180c 837de400             cmp     dword ptr [rbp - 0x1c], 0x0
00007fff7e461810 7441                 je      0x7fff7e461853
00007fff7e461812 48bfd0f87e7dff7f0000 movabs  rdi, 0x7fff7d7ef8d0
00007fff7e46181c e83f04e177           call    0x7ffff6271c60 (JitHelp: CORINFO_HELP_NEWSFAST)
00007fff7e461821 48894588             mov     qword ptr [rbp - 0x78], rax
00007fff7e461825 bfc5010000           mov     edi, 0x1c5
00007fff7e46182a 48be9827df7eff7f0000 movabs  rsi, 0x7fff7edf2798
00007fff7e461834 e8070ae177           call    0x7ffff6272240 (JitHelp: CORINFO_HELP_STRCNS)
00007fff7e461839 48894580             mov     qword ptr [rbp - 0x80], rax
00007fff7e46183d 488b7580             mov     rsi, qword ptr [rbp - 0x80]
00007fff7e461841 488b7d88             mov     rdi, qword ptr [rbp - 0x78]
00007fff7e461845 e8364f30ff           call    0x7fff7d766780 (System.ArgumentNullException..ctor(System.String), mdToken: 0000000006000DCC)
00007fff7e46184a 488b7d88             mov     rdi, qword ptr [rbp - 0x78]
00007fff7e46184e e80d70e177           call    0x7ffff6278860 (JitHelp: CORINFO_HELP_THROW)

/workspaces/rust-csharp-ffi/dotnet/Db.Storage/Store.cs @ 17:
00007fff7e461853 e8d03530ff           call    0x7fff7d764e28 (System.Text.Encoding.get_UTF8(), mdToken: 00000000060024D7)
00007fff7e461858 488945b8             mov     qword ptr [rbp - 0x48], rax
00007fff7e46185c 488b7db8             mov     rdi, qword ptr [rbp - 0x48]
00007fff7e461860 488b75f0             mov     rsi, qword ptr [rbp - 0x10]
00007fff7e461864 488b45b8             mov     rax, qword ptr [rbp - 0x48]
00007fff7e461868 488b00               mov     rax, qword ptr [rax]
00007fff7e46186b 488b4058             mov     rax, qword ptr [rax + 0x58]
00007fff7e46186f ff5010               call    qword ptr [rax + 0x10]
00007fff7e461872 488945b0             mov     qword ptr [rbp - 0x50], rax
00007fff7e461876 488b7db0             mov     rdi, qword ptr [rbp - 0x50]
00007fff7e46187a 48897de8             mov     qword ptr [rbp - 0x18], rdi

/workspaces/rust-csharp-ffi/dotnet/Db.Storage/Store.cs @ 20:
00007fff7e46187e 90                   nop     

/workspaces/rust-csharp-ffi/dotnet/Db.Storage/Store.cs @ 21:
00007fff7e46187f 488b7de8             mov     rdi, qword ptr [rbp - 0x18]
00007fff7e461883 48897dd0             mov     qword ptr [rbp - 0x30], rdi
00007fff7e461887 48837de800           cmp     qword ptr [rbp - 0x18], 0x0
00007fff7e46188c 740a                 je      0x7fff7e461898
00007fff7e46188e 488b7dd0             mov     rdi, qword ptr [rbp - 0x30]
00007fff7e461892 837f0800             cmp     dword ptr [rdi + 0x8], 0x0
00007fff7e461896 750b                 jne     0x7fff7e4618a3
00007fff7e461898 33ff                 xor     edi, edi
00007fff7e46189a 8bff                 mov     edi, edi
00007fff7e46189c 48897dd8             mov     qword ptr [rbp - 0x28], rdi
00007fff7e4618a0 90                   nop     
00007fff7e4618a1 eb29                 jmp     0x7fff7e4618cc
00007fff7e4618a3 488b7dd0             mov     rdi, qword ptr [rbp - 0x30]
00007fff7e4618a7 33c0                 xor     eax, eax
00007fff7e4618a9 3b4708               cmp     eax, dword ptr [rdi + 0x8]
00007fff7e4618ac 7205                 jb      0x7fff7e4618b3
00007fff7e4618ae e89d74e177           call    0x7ffff6278d50 (JitHelp: CORINFO_HELP_RNGCHKFAIL)
00007fff7e4618b3 8bf0                 mov     esi, eax
00007fff7e4618b5 488d7c3710           lea     rdi, [rdi + rsi + 0x10]
00007fff7e4618ba 4889bd78ffffff       mov     qword ptr [rbp - 0x88], rdi
00007fff7e4618c1 488bbd78ffffff       mov     rdi, qword ptr [rbp - 0x88]
00007fff7e4618c8 48897dd8             mov     qword ptr [rbp - 0x28], rdi

/workspaces/rust-csharp-ffi/dotnet/Db.Storage/Store.cs @ 22:
00007fff7e4618cc 90                   nop     

/workspaces/rust-csharp-ffi/dotnet/Db.Storage/Store.cs @ 23:
00007fff7e4618cd 488b7dd8             mov     rdi, qword ptr [rbp - 0x28]
00007fff7e4618d1 e82a1c30ff           call    0x7fff7d763500 (System.IntPtr.op_Explicit(Void*), mdToken: 00000000060012F4)
00007fff7e4618d6 488945a8             mov     qword ptr [rbp - 0x58], rax
00007fff7e4618da 488b7de8             mov     rdi, qword ptr [rbp - 0x18]
00007fff7e4618de 8b7f08               mov     edi, dword ptr [rdi + 0x8]
00007fff7e4618e1 4863ff               movsxd  rdi, edi
00007fff7e4618e4 e81f2230ff           call    0x7fff7d763b08 (System.UIntPtr.op_Explicit(UInt64), mdToken: 0000000006001912)
00007fff7e4618e9 488945a0             mov     qword ptr [rbp - 0x60], rax
00007fff7e4618ed 488d55c8             lea     rdx, [rbp - 0x38]
00007fff7e4618f1 488b7da8             mov     rdi, qword ptr [rbp - 0x58]
00007fff7e4618f5 488b75a0             mov     rsi, qword ptr [rbp - 0x60]
00007fff7e4618f9 b901000000           mov     ecx, 0x1
00007fff7e4618fe e895faffff           call    0x7fff7e461398 (Db.Storage.Native.Bindings.db_store_open(IntPtr, UIntPtr, Db.Storage.Native.StoreHandle ByRef, Boolean), mdToken: 000000000600002D)
>>> 00007fff7e461903 894598               mov     dword ptr [rbp - 0x68], eax
00007fff7e461906 90                   nop     

/workspaces/rust-csharp-ffi/dotnet/Db.Storage/Store.cs @ 25:
00007fff7e461907 48bf1831df7eff7f0000 movabs  rdi, 0x7fff7edf3118
00007fff7e461911 e84a03e177           call    0x7ffff6271c60 (JitHelp: CORINFO_HELP_NEWSFAST)
00007fff7e461916 48894590             mov     qword ptr [rbp - 0x70], rax
00007fff7e46191a 488b7d90             mov     rdi, qword ptr [rbp - 0x70]
00007fff7e46191e e88dc2ffff           call    0x7fff7e45dbb0 (Db.Storage.Store..ctor(), mdToken: 0000000006000025)
00007fff7e461923 488b7d90             mov     rdi, qword ptr [rbp - 0x70]
00007fff7e461927 488d7f10             lea     rdi, [rdi + 0x10]
00007fff7e46192b 488b75f0             mov     rsi, qword ptr [rbp - 0x10]
00007fff7e46192f e86c41ea77           call    0x7ffff6305aa0 (JitHelp: CORINFO_HELP_ASSIGN_REF)
00007fff7e461934 488b7d90             mov     rdi, qword ptr [rbp - 0x70]
00007fff7e461938 488d7f08             lea     rdi, [rdi + 0x8]
00007fff7e46193c 488b75c8             mov     rsi, qword ptr [rbp - 0x38]
00007fff7e461940 e85b41ea77           call    0x7ffff6305aa0 (JitHelp: CORINFO_HELP_ASSIGN_REF)
00007fff7e461945 488b4590             mov     rax, qword ptr [rbp - 0x70]
00007fff7e461949 488945c0             mov     qword ptr [rbp - 0x40], rax
00007fff7e46194d 90                   nop     
00007fff7e46194e eb00                 jmp     0x7fff7e461950

/workspaces/rust-csharp-ffi/dotnet/Db.Storage/Store.cs @ 32:
00007fff7e461950 488b45c0             mov     rax, qword ptr [rbp - 0x40]
00007fff7e461954 488d65f8             lea     rsp, [rbp - 0x8]
00007fff7e461958 415d                 pop     r13
00007fff7e46195a 5d                   pop     rbp
00007fff7e46195b c3                   ret
```

</details>

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
