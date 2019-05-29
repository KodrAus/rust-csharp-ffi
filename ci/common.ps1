Write-Output "dotnet: $(dotnet --version)"
Write-Output "rustc: $(rustc --version)"
Write-Output "node: $(node --version)"

function Clean-OutputDirs {
    Remove-Item -Recurse -Force dotnet/**/bin
    Remove-Item -Recurse -Force dotnet/**/obj

    if (Test-Path target) {
        Remove-Item -Recurse -Force target
    }
}
