$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent

. $scriptDir/common.ps1

Clean-OutputDirs

cargo test --target x86_64-apple-darwin
if ($LastExitCode) { exit 1 }

dotnet test -v n
if ($LastExitCode) { exit 1 }

# CoreCLR publish
Clean-OutputDirs

dotnet publish `
    dotnet/Db.Api/Db.Api.csproj `
    -c Release `
    -f netcoreapp3.0 `
    -r osx-x64 `
    -v n
if ($LastExitCode) { exit 1 }

dotnet run `
    -p dotnet/Db.Tests.Integration/Db.Tests.Integration.csproj `
    -- dotnet/Db.Api/bin/Release/netcoreapp3.0/osx-x64/publish/Db.Api
if ($LastExitCode) { exit 1 }

# CoreRT publish
Clean-OutputDirs

dotnet publish `
    dotnet/Db.Api/Db.Api.csproj `
    -c Release `
    -f netcoreapp3.0 `
    -r osx-x64 `
    -v n `
    /p:AotBuild=true
if ($LastExitCode) { exit 1 }

dotnet run `
    -p dotnet/Db.Tests.Integration/Db.Tests.Integration.csproj `
    -- dotnet/Db.Api/bin/Release/netcoreapp3.0/osx-x64/publish/Db.Api
if ($LastExitCode) { exit 1 }
