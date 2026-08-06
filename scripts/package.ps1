[CmdletBinding()]
param(
    [ValidatePattern('^[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?$')]
    [string] $Version = '0.1.0-dev',

    [ValidateSet('win-x64', 'win-arm64')]
    [string] $Runtime = 'win-x64',

    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [string] $OutputDirectory = 'artifacts'
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot '..'))

if ([System.IO.Path]::IsPathRooted($OutputDirectory)) {
    throw 'OutputDirectory must be relative to the repository root.'
}

$outputRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $repositoryRoot $OutputDirectory))
$repositoryPrefix = $repositoryRoot.TrimEnd(
    [System.IO.Path]::DirectorySeparatorChar) +
    [System.IO.Path]::DirectorySeparatorChar

if (-not $outputRoot.StartsWith(
        $repositoryPrefix,
        [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'OutputDirectory must remain inside the repository.'
}

$packageName = "BootForge-$Version-$Runtime"
$publishDirectory = Join-Path $outputRoot $packageName
$archivePath = Join-Path $outputRoot "$packageName.zip"
$checksumPath = "$archivePath.sha256"
$projectPath = Join-Path $repositoryRoot 'src\BootForge.App\BootForge.App.csproj'

New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null

foreach ($path in @($publishDirectory, $archivePath, $checksumPath)) {
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Recurse -Force
    }
}

$publishArguments = @(
    'publish'
    $projectPath
    '--configuration', $Configuration
    '--runtime', $Runtime
    '--self-contained', 'true'
    '--output', $publishDirectory
    "-p:Version=$Version"
    '-p:PublishSingleFile=true'
    '-p:IncludeNativeLibrariesForSelfExtract=true'
    '-p:PublishTrimmed=false'
    '-p:DebugType=None'
    '-p:DebugSymbols=false'
)

& dotnet @publishArguments

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Compress-Archive `
    -Path (Join-Path $publishDirectory '*') `
    -DestinationPath $archivePath `
    -CompressionLevel Optimal

$checksum = Get-FileHash -LiteralPath $archivePath -Algorithm SHA256
$checksumLine = '{0}  {1}' -f `
    $checksum.Hash.ToLowerInvariant(), `
    (Split-Path $archivePath -Leaf)
Set-Content -LiteralPath $checksumPath -Value $checksumLine -Encoding ascii

Write-Host "Package:  $archivePath"
Write-Host "Checksum: $checksumPath"
