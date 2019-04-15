$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent

. $scriptDir/common.ps1

Clean-OutputDirs

dotnet test
if ($LastExitCode) { exit 1 }

# CoreCLR publish
Clean-OutputDirs

dotnet publish dotnet/Db.Api/Db.Api.csproj -c Release -f netcoreapp2.2 -r win-x64
if ($LastExitCode) { exit 1 }

dotnet run -p dotnet/Db.Tests.Integration/Db.Tests.Integration.csproj -- dotnet/Db.Api/bin/Release/netcoreapp2.2/win-x64/publish/Db.Api.exe
if ($LastExitCode) { exit 1 }

# CoreRT publish
Clean-OutputDirs

dotnet publish dotnet/Db.Api/Db.Api.csproj -c Release -f netcoreapp2.2 -r win-x64 /p:AotBuild=true
if ($LastExitCode) { exit 1 }

dotnet run -p dotnet/Db.Tests.Integration/Db.Tests.Integration.csproj -- dotnet/Db.Api/bin/Release/netcoreapp2.2/publish/win-x64/Db.Api.exe
if ($LastExitCode) { exit 1 }
