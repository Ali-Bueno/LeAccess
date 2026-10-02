[CmdletBinding()]
param(
    [string]$OutputPath = (Join-Path (Split-Path -Parent $PSScriptRoot) ('LeAccess-Steam-1.5.1-prueba-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.zip')),
    [string]$BepInExArchive = (Join-Path $env:TEMP 'LeAccess-steam-port-20261001/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.788.zip'),
    [switch]$SkipBuild,
    [string]$GameDir = 'D:\games\steam\steamapps\common\Last Epoch'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.IO.Compression.FileSystem
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output = [IO.Path]::GetFullPath($OutputPath)
$loaderArchive = [IO.Path]::GetFullPath($BepInExArchive)
$loaderHash = 'F4CC496BD098A0DF4164B81E3737297707F13A47C2478DBA2F60EEFAB784817A'
$tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\', '/')
$stage = Join-Path $tempRoot ('LeAccess-test-package-' + [Guid]::NewGuid().ToString('N'))
$payload = Join-Path $stage 'payload'
$createdStage = $false
$expected = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::OrdinalIgnoreCase)
$utf8 = [Text.UTF8Encoding]::new($false)
# A fixed ZIP timestamp makes identical inputs produce identical output bytes.
$zipTimestamp = [DateTimeOffset]::new(2000, 1, 1, 0, 0, 0, [TimeSpan]::Zero)

function Is-Descendant([string]$Path, [string]$Parent) {
    return $Path.StartsWith($Parent.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar,
        [StringComparison]::OrdinalIgnoreCase)
}

function Get-PayloadPath([string]$Relative) {
    if ([string]::IsNullOrWhiteSpace($Relative) -or $Relative.Contains(':') -or
        $Relative -match '(^|[\\/])\.\.([\\/]|$)' -or [IO.Path]::IsPathRooted($Relative)) {
        throw "Unsafe package entry: $Relative"
    }
    $resolved = [IO.Path]::GetFullPath((Join-Path $payload $Relative))
    if (-not (Is-Descendant $resolved $payload)) { throw "Entry escapes staging: $Relative" }
    return $resolved
}

function Add-PackageFile([string]$Source, [string]$Relative) {
    if (-not (Test-Path -LiteralPath $Source -PathType Leaf)) { throw "Required input missing: $Source" }
    if ($expected.ContainsKey($Relative)) { throw "Duplicate package entry: $Relative" }
    $destination = Get-PayloadPath $Relative
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
    Copy-Item -LiteralPath $Source -Destination $destination
    $expected.Add($Relative, (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash)
}

function Add-PackageText([string]$Text, [string]$Relative) {
    if ($expected.ContainsKey($Relative)) { throw "Duplicate package entry: $Relative" }
    $destination = Get-PayloadPath $Relative
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
    [IO.File]::WriteAllText($destination, $Text.Replace("`r`n", "`n").Replace("`n", "`r`n"), $utf8)
    $expected.Add($Relative, (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash)
}

function Get-EntryHash($Entry) {
    $stream = $Entry.Open()
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-', '') }
    finally { $sha.Dispose(); $stream.Dispose() }
}

if ([IO.Path]::GetExtension($output) -ine '.zip') { throw 'OutputPath must end with .zip.' }
if ($output -ieq $loaderArchive) { throw 'OutputPath cannot overwrite the official loader archive.' }
if (Test-Path -LiteralPath $output) { throw "Output already exists; choose a new path: $output" }
if (-not (Test-Path -LiteralPath $loaderArchive -PathType Leaf)) { throw "Loader archive missing: $loaderArchive" }
if ((Get-FileHash -LiteralPath $loaderArchive -Algorithm SHA256).Hash -ne $loaderHash) {
    throw 'Unexpected BepInEx archive hash. Use the official Unity.IL2CPP win-x64 6.0.0-be.788 ZIP.'
}

if (-not $SkipBuild) {
    & dotnet build (Join-Path $repo 'LeAccess/LeAccess.csproj') -c Debug '-p:SkipPluginDeployment=true' "-p:GameDir=$GameDir" -v:q
    if ($LASTEXITCODE -ne 0) { throw "Source build failed: $LASTEXITCODE" }
}

try {
    [IO.Directory]::CreateDirectory($payload) | Out-Null
    $createdStage = $true
    $sourceZip = [IO.Compression.ZipFile]::OpenRead($loaderArchive)
    $loaderFiles = 0
    try {
        foreach ($entry in $sourceZip.Entries) {
            $relative = $entry.FullName.Replace('\', '/')
            $destination = Get-PayloadPath $relative
            if ($relative.EndsWith('/')) { [IO.Directory]::CreateDirectory($destination) | Out-Null; continue }
            if ($expected.ContainsKey($relative)) { throw "Duplicate loader entry: $relative" }
            [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $destination)
            $expected.Add($relative, (Get-EntryHash $entry))
            $loaderFiles++
        }
    }
    finally { $sourceZip.Dispose() }

    $build = Join-Path $repo 'LeAccess/bin/Debug/net6.0'
    foreach ($name in @('LeAccess.dll', 'NAudio.dll', 'NAudio.Core.dll', 'NAudio.Wasapi.dll', 'NAudio.WinMM.dll', 'TolkDotNet.dll')) {
        Add-PackageFile (Join-Path $build $name) "BepInEx/plugins/LeAccess/$name"
    }
    foreach ($name in @('Tolk.dll', 'nvdaControllerClient64.dll')) {
        Add-PackageFile (Join-Path $repo "references/tolk/$name") "BepInEx/plugins/LeAccess/$name"
    }
    Add-PackageFile (Join-Path $repo 'LeAccess.InteropRepair/bin/Debug/net6.0/LeAccess.InteropRepair.dll') `
        'BepInEx/patchers/LeAccess.InteropRepair/LeAccess.InteropRepair.dll'
    foreach ($language in @('en', 'es')) {
        $catalog = Join-Path $build "Translations/$language.json"
        Get-Content -LiteralPath $catalog -Raw | ConvertFrom-Json | Out-Null
        Add-PackageFile $catalog "BepInEx/plugins/LeAccess/Translations/$language.json"
    }
    $packageAssets = Join-Path $repo 'packaging/test-package'
    Add-PackageFile (Join-Path $packageAssets 'Leeme-LeAccess.txt') 'Leeme-LeAccess.txt'
    Add-PackageFile (Join-Path $packageAssets 'THIRD-PARTY-NOTICES.txt') 'LeAccess-Licencias/THIRD-PARTY-NOTICES.txt'
    $licenses = @(Get-ChildItem -LiteralPath (Join-Path $packageAssets 'licenses') -File -Filter '*.txt')
    if ($licenses.Count -eq 0) { throw 'Dependency license texts are missing.' }
    foreach ($license in $licenses) { Add-PackageFile $license.FullName ('LeAccess-Licencias/' + $license.Name) }
    Add-PackageText @'
# Fresh diagnostic defaults. No settings from the developer's installation.
[Logging.Console]
Enabled = false

[Logging.Disk]
Enabled = true
AppendLog = false
LogLevels = All
WriteUnityLog = true

[IL2CPP]
UpdateInteropAssemblies = true
'@ 'BepInEx/config/BepInEx.cfg'

    $manifest = @('LeAccess test package; Windows x64; Last Epoch Steam 1.5.1 (build 25672295)',
        "BepInEx 6.0.0-be.788 archive SHA256: $loaderHash", '', 'SHA256  Relative file path', '')
    foreach ($relative in ($expected.Keys | Sort-Object -Culture en-US)) { $manifest += $expected[$relative] + '  ' + $relative }
    Add-PackageText ($manifest -join "`n") 'LeAccess-SHA256.txt'

    $temporaryZip = Join-Path $stage 'package.zip'
    $archive = [IO.Compression.ZipFile]::Open($temporaryZip, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($directory in (Get-ChildItem -LiteralPath $payload -Directory -Recurse | Sort-Object FullName)) {
            $relative = $directory.FullName.Substring($payload.Length + 1).Replace('\', '/') + '/'
            $archive.CreateEntry($relative).LastWriteTime = $zipTimestamp
        }
        foreach ($relative in ($expected.Keys | Sort-Object -Culture en-US)) {
            $entry = $archive.CreateEntry($relative, [IO.Compression.CompressionLevel]::Optimal)
            $entry.LastWriteTime = $zipTimestamp
            $inputStream = [IO.File]::OpenRead((Get-PayloadPath $relative))
            $outputStream = $entry.Open()
            try { $inputStream.CopyTo($outputStream) }
            finally { $outputStream.Dispose(); $inputStream.Dispose() }
        }
    }
    finally { $archive.Dispose() }

    $archive = [IO.Compression.ZipFile]::OpenRead($temporaryZip)
    try {
        $files = @($archive.Entries | Where-Object { -not $_.FullName.EndsWith('/') })
        if ($files.Count -ne $expected.Count) { throw 'Final ZIP file count does not match the allowlist.' }
        foreach ($entry in $files) {
            if (-not $expected.ContainsKey($entry.FullName)) { throw "Unexpected output entry: $($entry.FullName)" }
            if ((Get-EntryHash $entry) -ne $expected[$entry.FullName]) { throw "Output hash mismatch: $($entry.FullName)" }
        }
    }
    finally { $archive.Dispose() }
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($output)) | Out-Null
    Move-Item -LiteralPath $temporaryZip -Destination $output
    [pscustomobject]@{ Path = $output; Files = $expected.Count; OfficialLoaderFiles = $loaderFiles;
        Bytes = (Get-Item -LiteralPath $output).Length; SHA256 = (Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash }
}
finally {
    if ($createdStage -and (Test-Path -LiteralPath $stage)) {
        $resolvedStage = [IO.Path]::GetFullPath((Get-Item -LiteralPath $stage).FullName)
        if (-not (Is-Descendant $resolvedStage $tempRoot) -or $resolvedStage -ne $stage -or
            ([IO.File]::GetAttributes($resolvedStage) -band [IO.FileAttributes]::ReparsePoint)) {
            throw "Refusing to remove unverified staging directory: $resolvedStage"
        }
        Remove-Item -LiteralPath $resolvedStage -Recurse -Force
    }
}
