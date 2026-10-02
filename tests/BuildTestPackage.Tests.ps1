[CmdletBinding()]
param([string]$BepInExArchive = (Join-Path $env:TEMP 'LeAccess-steam-port-20261001/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.788.zip'))
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.IO.Compression.FileSystem
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$builder = Join-Path $repo 'tools/build_test_package.ps1'
$tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\', '/')
$testRoot = Join-Path $tempRoot ('LeAccess-package-tests-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($testRoot) | Out-Null
$checks = 0
$repoPackage = $null
function Check([bool]$Value, [string]$Message) { $script:checks++; if (-not $Value) { throw $Message } }
function Hash-Entry($Entry) {
    $stream = $Entry.Open(); $sha = [Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-', '') }
    finally { $sha.Dispose(); $stream.Dispose() }
}
try {
    $first = Join-Path $testRoot 'first.zip'
    $second = Join-Path $testRoot 'second.zip'
    & $builder -OutputPath $first -BepInExArchive $BepInExArchive -SkipBuild | Out-Null
    & $builder -OutputPath $second -BepInExArchive $BepInExArchive -SkipBuild | Out-Null
    Check ((Get-FileHash -LiteralPath $first).Hash -eq (Get-FileHash -LiteralPath $second).Hash) 'Repeated identical inputs must produce identical ZIP bytes.'
    $zip = [IO.Compression.ZipFile]::OpenRead($first)
    $official = [IO.Compression.ZipFile]::OpenRead($BepInExArchive)
    try {
        foreach ($entry in $official.Entries) {
            if ($entry.FullName.EndsWith('/')) { continue }
            $copy = $zip.GetEntry($entry.FullName)
            Check ($null -ne $copy -and (Hash-Entry $entry) -eq (Hash-Entry $copy)) ('Official file changed: ' + $entry.FullName)
        }
        $names = @($zip.Entries | Where-Object { -not $_.FullName.EndsWith('/') } | ForEach-Object { $_.FullName })
        Check ($null -ne $zip.GetEntry('winhttp.dll') -and $null -ne $zip.GetEntry('dotnet/coreclr.dll')) 'Loader/runtime must be at game-root-relative paths.'
        Check (@($names | Where-Object { $_ -match '^BepInEx/plugins/LeAccess/[^/]+\.dll$' }).Count -eq 8) 'Exactly eight plugin DLLs are required.'
        Check ($null -ne $zip.GetEntry('BepInEx/patchers/LeAccess.InteropRepair/LeAccess.InteropRepair.dll')) 'Interop repair must be packaged.'
        Check (-not ($names -match '(?i)(^|/)(interop|cache|unity-libs|logs|saves|accounts?)(/|$)|LogOutput|GameAssembly|Last Epoch\.exe|Probe|\.pdb$')) 'Private/generated/game/probe artifacts must be absent.'
        Check (@($names | Where-Object { $_ -like 'BepInEx/config/*' }).Count -eq 1) 'Only the generated BepInEx diagnostic config belongs in the ZIP.'
        foreach ($language in @('en', 'es')) {
            $entry = $zip.GetEntry("BepInEx/plugins/LeAccess/Translations/$language.json")
            $source = Join-Path $repo "LeAccess/bin/Debug/net6.0/Translations/$language.json"
            Check ((Hash-Entry $entry) -eq (Get-FileHash -LiteralPath $source).Hash) 'Catalog must come from this build.'
        }
        $entry = $zip.GetEntry('BepInEx/config/BepInEx.cfg'); $reader = [IO.StreamReader]::new($entry.Open())
        try { $config = $reader.ReadToEnd() } finally { $reader.Dispose() }
        Check ($config.Contains('LogLevels = All') -and $config -notmatch '[A-Z]:\\|Users[/\\]|token|password') 'Generated diagnostic config must not contain machine/account data.'
        Check (-not ($names -match '(?i)\.bat$')) 'Launch through Steam; no BAT launcher belongs in the package.'
        Check ($null -ne $zip.GetEntry('LeAccess-SHA256.txt') -and $null -ne $zip.GetEntry('Leeme-LeAccess.txt')) 'Instructions and manifest are required.'
    }
    finally { $official.Dispose(); $zip.Dispose() }
    Push-Location $testRoot
    try { $defaultPackage = & $builder -BepInExArchive $BepInExArchive -SkipBuild }
    finally { Pop-Location }
    $repoPackage = $defaultPackage.Path
    Check ([IO.Path]::GetDirectoryName($repoPackage) -eq $repo) 'Default output must be the repository root even when invoked from elsewhere.'
    Check ((Get-FileHash -LiteralPath $repoPackage).Hash -eq (Get-FileHash -LiteralPath $first).Hash) 'Repository-local package must preserve the same ZIP contents.'
    $fake = Join-Path $testRoot 'wrong-loader.zip'; [IO.File]::WriteAllText($fake, 'wrong loader')
    $rejected = $false
    try { & $builder -OutputPath (Join-Path $testRoot 'must-not-create.zip') -BepInExArchive $fake -SkipBuild | Out-Null }
    catch { $rejected = $_.Exception.Message -like '*archive hash*' }
    Check $rejected 'Unexpected loader contents must be rejected.'
    Write-Output "Package tests passed: $checks checks. No game launch or deployment."
}
finally {
    if ($repoPackage -and (Test-Path -LiteralPath $repoPackage -PathType Leaf)) {
        $resolvedPackage = [IO.Path]::GetFullPath($repoPackage)
        if ([IO.Path]::GetDirectoryName($resolvedPackage) -ne $repo -or
            [IO.Path]::GetFileName($resolvedPackage) -notlike 'LeAccess-Steam-1.5.1-prueba-*.zip') {
            throw 'Refusing to remove an unexpected package output.'
        }
        Remove-Item -LiteralPath $resolvedPackage
    }
    $resolved = [IO.Path]::GetFullPath((Get-Item -LiteralPath $testRoot).FullName)
    if ($resolved -ne $testRoot -or -not $resolved.StartsWith($tempRoot + [IO.Path]::DirectorySeparatorChar,
        [StringComparison]::OrdinalIgnoreCase) -or
        ([IO.File]::GetAttributes($resolved) -band [IO.FileAttributes]::ReparsePoint)) { throw 'Unsafe test cleanup path.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
