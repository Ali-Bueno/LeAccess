# Reproducible test ZIP

`tools/build_test_package.ps1` creates a Windows x64 ZIP that the recipient
extracts directly beside `Last Epoch.exe`. It does not start or deploy the game,
publish anything, or copy the developer's installation. `GameDir` is passed
only to the source build so it can reference the installed interop assemblies.

```powershell
./tools/build_test_package.ps1
```

The default source archive is the already downloaded official
`%TEMP%/LeAccess-steam-port-20261001/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.788.zip`.
For another location, pass `-BepInExArchive`. Its required SHA256 is
`F4CC496BD098A0DF4164B81E3737297707F13A47C2478DBA2F60EEFAB784817A`.
The archive comes from the [official BepInEx builds](https://builds.bepinex.dev/projects/bepinex_be).
It already includes Doorstop 4.5.0 and .NET 6.0.7; the recipient does not need
a separate .NET install.

The default action builds Debug with `SkipPluginDeployment=true`. After the
maintainer's final build, use `-SkipBuild` to package exactly those artifacts:

```powershell
./tools/build_test_package.ps1 -SkipBuild
```

`-SkipBuild` intentionally does not rebuild or prove artifact freshness. Run it
only after a successful final build. Per the user's preference, the default output
is a timestamped `LeAccess-Steam-1.5.1-prueba-*.zip` in the repository root, even
when the command is invoked from another directory. `-OutputPath` can override it.
Outputs must end in `.zip` and not already exist. ZIPs in the repository root are
local build artifacts ignored by Git, per the user's request. The builder never
publishes an archive or creates a release.

## Contents and validation

All 228 files in the pinned official loader archive retain their original
contents. The additions are an explicit allowlist:

- Eight plugin DLLs: LeAccess, NAudio, NAudio.Core, NAudio.Wasapi, NAudio.WinMM,
  TolkDotNet, Tolk and nvdaControllerClient64.
- `LeAccess.InteropRepair.dll` under `BepInEx/patchers/LeAccess.InteropRepair`.
- The built `Translations/en.json` and `es.json`; map data remains embedded.
- A newly generated minimal BepInEx diagnostic configuration; LeAccess uses its
  own defaults. Existing personal configurations are never input files.
- Spanish `Leeme-LeAccess.txt`, dependency
  licenses/notices and a relative-path SHA256 manifest.

No game binaries/assets, interop cache, Unity-library cache, logs, saved games,
account data, probes or personal configuration are read into the package.
The final ZIP is checked against the full expected file list and hashes before
being moved to the output path. It has no wrapping directory. Entries have a
fixed timestamp and sorted paths, so identical inputs under the same PowerShell/
.NET compression implementation produce identical ZIP bytes.

Staging uses a uniquely named directory under the system TEMP directory. Before
recursive cleanup, the script verifies the resolved directory is the exact
generated path, remains a TEMP descendant, and is not a reparse point. It never
recursively removes an output directory or game directory.

Run the independent package checks after a current build:

```powershell
./tests/BuildTestPackage.Tests.ps1
```

Validated on 2026-10-02: **242 checks pass**. The package has 264
files, including all 228 original loader files and 21 license texts. Two builds
from identical inputs produce identical ZIP hashes. Validation confirms the default
repository-root location from another working directory and rejects a loader with
an unexpected hash. There is no BAT launcher; recipients start the game through Steam.

## Licenses and recipient limits

License texts in `packaging/test-package/licenses` were obtained from the
components' official repositories; `THIRD-PARTY-NOTICES.txt` records their source
locations, with the loader/runtime/interoperability commits matched to binary
version metadata. Tolk and its C# binding are
[LGPL-3.0](https://github.com/dkager/tolk/blob/master/LICENSE.txt); the NVDA client
uses [LGPL-2.1](https://github.com/dkager/tolk/blob/master/LICENSE-NVDA.txt).
The existing Tolk reference binaries' original build revision is not recorded;
the notice says so, retains source links, and identifies the distributed files
by hash. No upstream binaries or license texts are modified.

The recipient needs their own compatible Steam game and NVDA. First startup
generates local interop and may download Unity libraries, so it can require
Internet and several minutes. These generated files intentionally do not come
from the developer's machine. This tool checks package composition, not a clean
machine boot; actual first-run generation and speech still require that test.
