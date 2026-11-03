param([switch]$Native, [switch]$VisualCheck)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $projectRoot
if (-not $Native) {
    & docker build -f Dockerfile.windows -t slaves-encourager-build .
    if ($LASTEXITCODE -ne 0) { throw 'Windows container build unavailable. See AGENTS.md for the existing-toolchain fallback.' }
    $containerId = & docker create slaves-encourager-build
    try {
        New-Item -ItemType Directory -Force -Path dist | Out-Null
        & docker cp "${containerId}:C:/src/dist/." dist
        if ($LASTEXITCODE -ne 0) { throw 'Artifact extraction failed' }
    } finally { & docker rm $containerId | Out-Null }
    exit 0
}
$framework = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'The existing .NET Framework compiler was not found. No software was installed.' }
New-Item -ItemType Directory -Force -Path dist,.build | Out-Null
$utf8 = New-Object System.Text.UTF8Encoding($false, $true)
Get-ChildItem -LiteralPath src,locales,scripts,tests -Recurse -File | Where-Object { $_.Extension -in '.cs','.xaml','.ps1','.csproj','.manifest','.tsv','.py' } | ForEach-Object {
    $bytes = [System.IO.File]::ReadAllBytes($_.FullName)
    if ($bytes.Length -ge 3 -and $bytes[0] -eq 239 -and $bytes[1] -eq 187 -and $bytes[2] -eq 191) { throw "BOM found: $($_.Name)" }
    $null = $utf8.GetString($bytes)
    if ($_.Extension -in '.xaml','.csproj','.manifest') { $null = [xml]$utf8.GetString($bytes) }
}
& (Join-Path $PSScriptRoot 'create-icon.ps1')
$references = @('System.dll','System.Core.dll','System.Xaml.dll','System.Runtime.Serialization.dll','System.Xml.dll','System.Windows.Forms.dll','System.Drawing.dll') | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
$references += @('WindowsBase.dll','PresentationCore.dll','PresentationFramework.dll') | ForEach-Object { '/reference:' + (Join-Path $framework ('WPF/' + $_)) }
$sourceRoot = Join-Path $projectRoot 'src/SlavesEncourager'
$resources = @('/resource:src/SlavesEncourager/Main.xaml,Main.xaml','/resource:src/SlavesEncourager/Settings.xaml,Settings.xaml','/resource:assets/app.png,AppIcon.png','/resource:locales/catalog.tsv,Locales.tsv')
$arguments = @('/nologo','/utf8output','/codepage:65001','/warn:4','/warnaserror+','/optimize+','/target:winexe','/platform:anycpu','/win32icon:assets/app.ico','/win32manifest:src/SlavesEncourager/app.manifest','/out:dist/SlavesEncourager.exe')
$sources = @('Engine.cs','FlipDisplay.cs','EditorControls.cs','HomeTextEditor.cs','Localization.cs','Program.cs','AssemblyInfo.cs') | ForEach-Object { Join-Path $sourceRoot $_ }
& $compiler @arguments @references @resources @sources
if ($LASTEXITCODE -ne 0) { throw 'Application build failed' }
& $compiler /nologo /utf8output /codepage:65001 /warn:4 /warnaserror+ /target:exe /out:.build/EngineTests.exe @references /resource:locales/catalog.tsv,Locales.tsv (Join-Path $sourceRoot 'Engine.cs') (Join-Path $sourceRoot 'Localization.cs') (Join-Path $projectRoot 'tests/EngineTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed' }
$testOutput = & .\.build\EngineTests.exe 2>&1
$testExit = $LASTEXITCODE
[System.IO.File]::WriteAllLines((Join-Path $projectRoot '.build/tests.txt'), [string[]]$testOutput, $utf8)
$testOutput | Write-Output
if ($testExit -ne 0) { throw 'Behavior checks failed' }
[System.IO.File]::WriteAllText((Join-Path $projectRoot 'dist/SlavesEncourager.exe.config'), '<?xml version="1.0" encoding="utf-8"?><configuration><startup><supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.8"/></startup></configuration>', $utf8)
$appHash = (Get-FileHash -LiteralPath dist/SlavesEncourager.exe -Algorithm SHA256).Hash
[System.IO.File]::WriteAllText((Join-Path $projectRoot '.build/build.txt'), "Build: PASS`nCompiler: existing .NET Framework C#`nWarnings: 0 (warnaserror)`nUTF-8/XML: PASS`nSHA256: $appHash`n", $utf8)
Write-Output "Built dist/SlavesEncourager.exe | SHA256 $appHash"
if ($VisualCheck) {
    foreach ($language in @('zh-Hans','zh-Hant','en','ja','es','ko')) {
        $previewDir = Join-Path $projectRoot ('.build/previews/' + $language)
        New-Item -ItemType Directory -Force -Path $previewDir | Out-Null
        $resultPath = Join-Path $previewDir 'render-result.txt'
        if (Test-Path -LiteralPath $resultPath) { Remove-Item -LiteralPath $resultPath }
        $previewProcess = Start-Process -FilePath (Join-Path $projectRoot 'dist/SlavesEncourager.exe') -ArgumentList @('--preview-dir', ('"' + $previewDir + '"'),'--language',$language) -WindowStyle Hidden -PassThru
        if (-not $previewProcess.WaitForExit(20000)) { $previewProcess.Kill(); throw "Visual verification timed out: $language" }
        if ($previewProcess.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $resultPath)) { throw "XAML or render verification failed: $language (inspect render-error.txt)" }
        Write-Output "Visual verification: $language PASS"
    }
}
