dotnet clean
cargo clean

dotnet restore
dotnet test

# CoreCLR publish
dotnet publish dotnet/Db.Api/Db.Api.csproj -c Release -f netcoreapp2.2

# CoreRT publish
dotnet publish dotnet/Db.Api/Db.Api.csproj -c Release -f netcoreapp2.2 -r win-x64 /p:AotBuild=true
