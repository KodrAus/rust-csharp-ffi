dotnet clean
cargo clean

dotnet restore
dotnet test

# CoreCLR publish
dotnet publish dotnet/Db.Api/Db.Api.csproj -c Release -f netcoreapp2.2

# CoreRT publish
dotnet publish dotnet/Db.Api/Db.Api.csproj -c Release -f netcoreapp2.2 -r linux-x64 /p:AotBuild=true
