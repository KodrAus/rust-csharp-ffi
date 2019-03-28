$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent

. $scriptDir/common.ps1

Clean-OutputDirs
dotnet test

# CoreCLR publish
Clean-OutputDirs
dotnet publish dotnet/Db.Api/Db.Api.csproj -c Release -f netcoreapp2.2

# CoreRT publish
Clean-OutputDirs
dotnet publish dotnet/Db.Api/Db.Api.csproj -c Release -f netcoreapp2.2 -r win-x64 /p:AotBuild=true
