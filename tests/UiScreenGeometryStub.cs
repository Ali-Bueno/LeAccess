// These suites cover reader state/ownership with no rendered geometry. The real
// geometry, opacity and clipping implementation is linked by MenuReading.Tests.
namespace LeAccess.Accessibility.Announcements;

internal static class UiScreenGeometry
{
    internal static bool IsVisible(UnityEngine.GameObject element) => true;
    internal static bool IsVisibleGraphic(UnityEngine.Component element) => true;
    internal static bool HasVisiblePresentation(UnityEngine.GameObject element) => true;
}
