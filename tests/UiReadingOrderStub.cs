// State-only suites omit geometry. ScreenContent.Tests covers production ordering
// with assigned screen rectangles; MenuReading.Tests covers the actual projection.
namespace LeAccess.Accessibility.Announcements;

internal static class UiReadingOrder
{
    internal readonly record struct Fragment(UnityEngine.Component Label, string Text);
    internal static System.Collections.Generic.List<string> Read(System.Collections.Generic.List<Fragment> fragments) =>
        fragments.Select(fragment => fragment.Text).ToList();
}
