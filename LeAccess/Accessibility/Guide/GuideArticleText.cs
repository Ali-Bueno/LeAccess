using System.Collections.Generic;
using System.Linq;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;

namespace LeAccess.Accessibility.Guide;

/// <summary>Builds the spoken text of a game-guide article and of its category headers.</summary>
internal static class GuideArticleText
{
    /// <summary>The title then every non-empty paragraph, without markup or repeats.</summary>
    internal static string Compose(string title, IEnumerable<string> paragraphs) =>
        ScreenContentTracker.Combine(new[] { TextUtils.CleanText(title) }
            .Concat(paragraphs.Select(TextUtils.CleanText)));

    internal static string Category(string name, bool expanded) =>
        ModText.Format("guide.category", name, ModText.Get(expanded ? "guide.expanded" : "guide.collapsed"));
}
