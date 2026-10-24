param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceDir = Join-Path $root "src"
$buildDir = Join-Path $root "build"
$outDir = Join-Path $buildDir "out"
$distDir = Join-Path $root "dist"
$refDir = Join-Path $buildDir "ref"
$sdkPackageVersion = "10.0.28000.1721"

$frameworkDir = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319"
$csc = Join-Path $frameworkDir "csc.exe"
if (-not (Test-Path $csc)) {
    throw "找不到 .NET Framework 编译器：$csc"
}

$requiredRefs = @(
    "Windows.WinMD",
    "Windows.Foundation.FoundationContract.winmd",
    "Windows.Foundation.UniversalApiContract.winmd"
)

$missingRefs = $requiredRefs | Where-Object { -not (Test-Path (Join-Path $refDir $_)) }
if ($missingRefs.Count -gt 0) {
    New-Item -ItemType Directory -Force -Path $refDir | Out-Null
    $cacheDir = Join-Path $env:TEMP ("screenocr-winmd-" + $sdkPackageVersion)
    $cacheRef = Join-Path $cacheDir "ref"
    $cacheMissing = $requiredRefs | Where-Object { -not (Test-Path (Join-Path $cacheRef $_)) }

    if ($cacheMissing.Count -gt 0) {
        Write-Host "正在下载 Windows SDK 引用元数据（仅编译时需要）..." -ForegroundColor Cyan
        New-Item -ItemType Directory -Force -Path $cacheDir | Out-Null
        $packagePath = Join-Path $cacheDir "sdk-contracts.zip"
        $url = "https://api.nuget.org/v3-flatcontainer/microsoft.windows.sdk.contracts/$sdkPackageVersion/microsoft.windows.sdk.contracts.$sdkPackageVersion.nupkg"
        Invoke-WebRequest -Uri $url -OutFile $packagePath -UseBasicParsing
        $expanded = Join-Path $cacheDir "package"
        if (Test-Path $expanded) {
            Remove-Item $expanded -Recurse -Force
        }
        Expand-Archive -Path $packagePath -DestinationPath $expanded -Force
        Copy-Item (Join-Path $expanded "ref\netstandard2.0\*") $cacheRef -Force
    }

    foreach ($ref in $requiredRefs) {
        Copy-Item (Join-Path $cacheRef $ref) (Join-Path $refDir $ref) -Force
    }
}

New-Item -ItemType Directory -Force -Path $outDir, $distDir | Out-Null
$outExe = Join-Path $outDir "ScreenOcr.exe"
$distExe = Join-Path $distDir "屏幕识字.exe"

$sources = Get-ChildItem (Join-Path $sourceDir "*.cs") | Sort-Object Name | ForEach-Object { $_.FullName }
$references = @(
    (Join-Path $frameworkDir "System.dll"),
    (Join-Path $frameworkDir "System.Core.dll"),
    (Join-Path $frameworkDir "System.Drawing.dll"),
    (Join-Path $frameworkDir "System.Windows.Forms.dll"),
    (Join-Path $frameworkDir "System.Runtime.dll"),
    (Join-Path $frameworkDir "System.Runtime.WindowsRuntime.dll"),
    (Join-Path $refDir "Windows.WinMD"),
    (Join-Path $refDir "Windows.Foundation.FoundationContract.winmd"),
    (Join-Path $refDir "Windows.Foundation.UniversalApiContract.winmd")
)

$cscArgs = @(
    "/nologo",
    "/utf8output",
    "/codepage:65001",
    "/langversion:5",
    "/target:winexe",
    "/platform:anycpu",
    "/optimize+",
    "/debug-",
    "/warn:4",
    "/win32icon:$buildDir\app.ico",
    "/win32manifest:$sourceDir\app.manifest",
    "/out:$outExe"
)
foreach ($reference in $references) {
    $cscArgs += "/reference:$reference"
}
$cscArgs += $sources

Write-Host "正在编译..." -ForegroundColor Cyan
& $csc @cscArgs
if ($LASTEXITCODE -ne 0) {
    throw "编译失败（ExitCode=$LASTEXITCODE）。"
}

Copy-Item $outExe $distExe -Force
Write-Host ""
Write-Host "编译完成：$distExe" -ForegroundColor Green
Get-Item $distExe | Select-Object FullName, Length, LastWriteTime
