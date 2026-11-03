param([string]$ApplicationPath = '', [string]$FfmpegPath = 'ffmpeg')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$projectRoot = Split-Path -Parent $PSScriptRoot
if (-not $ApplicationPath) { $ApplicationPath = Join-Path $projectRoot 'dist/SlavesEncourager.exe' }
$applicationFile = (Resolve-Path -LiteralPath $ApplicationPath).Path
$toolRoot = Join-Path $projectRoot '.build/readme-demo'
$frameRoot = Join-Path $toolRoot ('frames-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $toolRoot,$frameRoot | Out-Null
$framework = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$references = @('System.dll','System.Core.dll','System.Xaml.dll') | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
$references += @('WindowsBase.dll','PresentationCore.dll','PresentationFramework.dll') | ForEach-Object { '/reference:' + (Join-Path $framework ('WPF/' + $_)) }
$references += '/reference:' + $applicationFile
$renderer = Join-Path $toolRoot 'DemoRenderer.exe'
& (Join-Path $framework 'csc.exe') /nologo /codepage:65001 /utf8output /warn:4 /warnaserror+ /optimize+ /target:winexe ('/out:' + $renderer) @references (Join-Path $PSScriptRoot 'DemoRenderer.cs')
if ($LASTEXITCODE -ne 0) { throw 'Demo renderer compilation failed' }
Copy-Item -LiteralPath $applicationFile -Destination (Join-Path $toolRoot 'SlavesEncourager.exe') -Force
# 独立演示进程不触碰正在运行的程序或用户设置 / Isolated capture leaves running apps and user settings untouched.
$captureProcess = Start-Process -FilePath $renderer -ArgumentList ('"' + $frameRoot + '"') -WindowStyle Hidden -PassThru
if (-not $captureProcess.WaitForExit(120000)) { $captureProcess.Kill(); throw 'Demo capture timed out' }
if ($captureProcess.ExitCode -ne 0 -or -not (Test-Path -LiteralPath (Join-Path $frameRoot 'capture.txt'))) { throw "Demo capture failed; inspect $frameRoot" }
$pattern = Join-Path $frameRoot 'frame-%04d.png'
$palette = Join-Path $frameRoot 'palette.png'
$output = Join-Path $projectRoot 'docs/demo.gif'
& $FfmpegPath -hide_banner -loglevel error -y -framerate 20 -i $pattern -vf 'palettegen=max_colors=192:stats_mode=diff' -frames:v 1 $palette
if ($LASTEXITCODE -ne 0) { throw 'GIF palette generation failed' }
& $FfmpegPath -hide_banner -loglevel error -y -framerate 20 -i $pattern -i $palette -lavfi 'paletteuse=dither=sierra2_4a:diff_mode=rectangle' -loop 0 $output
if ($LASTEXITCODE -ne 0) { throw 'GIF encoding failed' }
Write-Output "Demo: $output"
Write-Output "Source frames: $frameRoot"
