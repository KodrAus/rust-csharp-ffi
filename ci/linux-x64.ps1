function Clean-OutputDirs {
    Remove-Item -Recurse -Force dotnet/**/bin
    Remove-Item -Recurse -Force dotnet/**/obj

    if (Test-Path target) {
        Remove-Item -Recurse -Force target
    }
}

Clean-OutputDirs
dotnet test

# CoreCLR publish
Clean-OutputDirs
dotnet publish dotnet/Db.Api/Db.Api.csproj -c Release -f netcoreapp2.2

# CoreRT publish
Clean-OutputDirs
dotnet publish dotnet/Db.Api/Db.Api.csproj -c Release -f netcoreapp2.2 -r linux-x64 /p:AotBuild=true
