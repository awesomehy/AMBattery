$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
New-Item -ItemType Directory -Path (Join-Path $PSScriptRoot 'build') -Force | Out-Null
$sources = @('src\AssemblyInfo.cs','src\Program.cs','src\Settings.cs','src\BatteryRules.cs','src\HidDevice.cs','src\Native.cs','src\TaskbarController.cs','src\Widget.cs','src\SettingsForm.cs','src\MouseArtwork.cs','src\NotificationStatus.cs','src\UiTheme.cs','src\UiControls.cs','src\BrandArtwork.cs','src\TaskbarMouseArtwork.cs','src\DongleArtwork.cs')
$references = @('/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Runtime.Serialization.dll','/r:System.Core.dll')
$originalArtwork = '/resource:assets\am-infinity-mouse.png,AmBattery.MouseArtwork'
$brandArtwork = @('/resource:assets\brand\angry-miao.png,AmBattery.Brand.AngryMiao','/resource:assets\brand\no-wire.png,AmBattery.Brand.NoWire')
& $compiler /nologo /target:exe /platform:x64 /define:ASSET_BUILD /main:AmBattery.AssetBuilder /out:build\AssetBuilder.exe @references $originalArtwork @brandArtwork @sources src\AssetBuilder.cs
if ($LASTEXITCODE -ne 0) { throw '图片资源构建失败' }
& (Join-Path $PSScriptRoot 'build\AssetBuilder.exe')
if ($LASTEXITCODE -ne 0) { throw '图片资源生成失败' }
$artwork = '/resource:assets\am-infinity-mouse-ui.png,AmBattery.MouseArtwork'
$dongleArtwork = '/resource:assets\am-infinity-dongle-ui.png,AmBattery.DongleArtwork'
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /win32manifest:src\app.manifest /win32icon:assets\am-battery.ico @references $artwork @brandArtwork $dongleArtwork /out:build\AMBattery.exe @sources
if ($LASTEXITCODE -ne 0) { throw '编译失败' }
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /nowarn:0649 /r:System.Runtime.Serialization.dll /out:build\AMBattery.Recovery.exe src\AssemblyInfo.cs src\RecoveryWatcher.cs
if ($LASTEXITCODE -ne 0) { throw '恢复守护程序编译失败' }
Write-Output (Join-Path $PSScriptRoot 'build\AMBattery.exe')
