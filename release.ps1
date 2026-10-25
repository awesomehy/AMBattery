param([switch]$SkipTests)

$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
if ($SkipTests) {
    & (Join-Path $PSScriptRoot 'build.ps1')
} else {
    & (Join-Path $PSScriptRoot 'test.ps1')
}

$main = Join-Path $PSScriptRoot 'build\AMBattery.exe'
$recovery = Join-Path $PSScriptRoot 'build\AMBattery.Recovery.exe'
$version = [Diagnostics.FileVersionInfo]::GetVersionInfo($main).FileVersion
if ([Diagnostics.FileVersionInfo]::GetVersionInfo($recovery).FileVersion -ne $version) {
    throw '主程序与恢复程序版本不一致'
}
$releaseVersion = ([version]$version).ToString(3)
$name = "AMBattery-$releaseVersion-win-x64"
$output = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$staging = Join-Path $output ($name + '-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path (Join-Path $staging 'licenses') -Force | Out-Null
Copy-Item -LiteralPath $main,$recovery -Destination $staging
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Destination (Join-Path $staging '使用说明.md')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'THIRD-PARTY-NOTICES.md') -Destination $staging
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets\licenses\lucide-LICENSE.txt') -Destination (Join-Path $staging 'licenses')

$archive = Join-Path $output ($name + '.zip')
Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $archive -Force
$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
$checksum = Join-Path $output 'SHA256SUMS.txt'
[IO.File]::WriteAllText($checksum, "$hash  $name.zip`n", [Text.UTF8Encoding]::new($false))
Write-Output $archive
Write-Output $checksum
