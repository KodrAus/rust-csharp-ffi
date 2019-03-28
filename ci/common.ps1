function Clean-OutputDirs {
    Remove-Item -Recurse -Force dotnet/**/bin
    Remove-Item -Recurse -Force dotnet/**/obj

    if (Test-Path target) {
        Remove-Item -Recurse -Force target
    }
}
