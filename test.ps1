$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
. (Join-Path $PSScriptRoot 'build.ps1')
$checks = @(
    @{ Main = 'AmBattery.Tests'; Source = 'src\Tests.cs'; Output = 'build\Tests.exe' },
    @{ Main = 'AmBattery.PopupRegression'; Source = 'src\PopupRegression.cs'; Output = 'build\PopupRegression.exe' },
    @{ Main = 'AmBattery.NotificationRegression'; Source = 'src\NotificationRegression.cs'; Output = 'build\NotificationRegression.exe' },
    @{ Main = 'AmBattery.UiRegression'; Source = 'src\UiRegression.cs'; Output = 'build\UiRegression.exe' },
    @{ Main = 'AmBattery.RecoveryRegression'; Source = 'src\RecoveryRegression.cs'; Output = 'build\RecoveryRegression.exe' }
)
foreach ($check in $checks) {
    & $compiler /nologo /target:exe /platform:x64 "/main:$($check.Main)" "/out:$($check.Output)" @references $artwork @brandArtwork $dongleArtwork @sources $check.Source
    if ($LASTEXITCODE -ne 0) { throw "编译检查失败：$($check.Main)" }
    & (Join-Path $PSScriptRoot $check.Output)
    if ($LASTEXITCODE -ne 0) { throw "检查失败：$($check.Main)" }
}
