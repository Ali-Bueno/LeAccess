# Game guide reading (Mecánicas)

`GameGuidePanel` owns a `GuideManager` (`_guideManager`). Topic buttons are `GuideEntry`
(`title`), grouped by `GuideCategory` (`headerButton`, `expanded`). Choosing an entry runs
`GuideManager.OpenEntry`, which fills `activeEntryTitle.title` and `_activeEntryParagraphs`
(one TMP per paragraph). The forge's and stash's help buttons open the same panel
(`StashPanelV2.OnOpenGuideClicked`); the forge also pops a `GameGuideTooltip` hint
(`isGameGuideActive`, `guideText`).

`Guide/GuideAccessibility` (polled from `MenuAccessibility`):
- Finds the manager from the focused control (`GameGuidePanel` parent), keeps it while visible.
- Speaks the article once, queued behind the focus label: title then paragraphs, one utterance
  (`GuideArticleText.Compose`), after two stable frames. Rereads when the entry changes.
- Manual details gesture (right stick up / key) rereads the whole open article.
- Category headers say "Habilidades, desplegado|plegado" (`guide.*` keys).
- The tooltip hint is spoken once while it is shown.

Not verified in game: the prefab hierarchy was not inspected (the guide prefab is not in
PermaLoad); ownership relies on the interop fields above. Listening check pending.
