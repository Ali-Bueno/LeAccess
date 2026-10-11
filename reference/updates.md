# Update check (2026-10-11, user request)

User request: every time the game starts, say when a newer version of the mod exists.

## Behaviour

- `UpdateCheck.Initialize` (from `Plugin.Load`) starts one background request to
  `https://api.github.com/repos/Ali-Bueno/LeAccess/releases/latest` (15 s timeout, `User-Agent: LeAccess/<version>`,
  which GitHub requires). GitHub's `latest` already skips drafts and pre-releases; `UpdateRelease.ReadTag` ignores them
  too.
- The worker only fetches. The game thread (`MenuAccessibility.LateUpdate` → `UpdateCheck.Update`) logs, builds the
  text and speaks it, queued so it never cuts other speech: "Hay una nueva versión de LeAccess: 0.2.0. Tienes la 0.1.0.
  Descárgala desde la página del mod en GitHub." (`update.available`, ten catalogs). The answer usually arrives right
  after "mod loaded", before the title screen.
- Silent when up to date, offline, rate-limited or on any unexpected reply. Log lines: `[Updates] newer release …`,
  `[Updates] up to date: running …, latest release …`, `[Updates] check failed: …`.
- Option: mod menu, Updates page, "Buscar actualizaciones al iniciar" (`Updates/CheckAtStartup`, on); applies at the next
  start.
- Versions compare numerically (`v0.10.0` > `0.9.0`); a leading `v` and any `-`/`+` suffix are ignored; missing parts
  count as zero. Tests: `tests/LeAccess.Updates.Tests` (35 checks).

## Release rule

The running version is `<Version>` in `LeAccess/LeAccess.csproj` (`MyPluginInfo.PLUGIN_VERSION`). Every GitHub release
must be tagged `v<Version>` of the DLL it ships, keep the `LeAccess.zip` asset name and not be a pre-release (see
[STATUS.md](../STATUS.md)). If the tag is higher than the shipped DLL's version, everyone on that release hears the
notice at every start. Each release also adds its days to [CHANGELOG.md](../CHANGELOG.md) ("day — version"
headings, player-facing, from the commits), and the release notes summarize them. `v0.1.0` shipped a DLL that said 1.0.0 and has no check; the source moved to `0.2.0` on
2026-10-11 as the next release's number.
