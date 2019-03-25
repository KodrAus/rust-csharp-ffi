dotnet clean
cargo clean

dotnet restore
dotnet test

dotnet publish dotnet/Db.Api/Db.Api.csproj -c Release -f netcoreapp2.2 -r osx-x64 /p:AotBuild=true
