using Il2CppInterop.Runtime.InteropTypes.Arrays;
using LE.UI.Minimap;
using UnityEngine;
using UnityEngine.UI;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Navigation.Guidance;

internal static class GuidanceMarkerVisibility
{
    private static readonly Il2CppStructArray<Vector3> ImageCorners = new(4);
    private static readonly Il2CppStructArray<Vector3> MapCorners = new(4);

    internal static bool IsVisible(MinimapIconUI ui, MinimapIconManager manager)
    {
        if (!IsAlive(ui) || !ui.isActiveAndEnabled || !ui.transform.IsChildOf(manager.transform)) return false;
        var mapRect = manager.minimap.transform.TryCast<RectTransform>();
        var canvas = manager.minimap.GetComponentInParent<Canvas>(true);
        if (!IsAlive(mapRect) || !IsAlive(canvas) || !canvas.isActiveAndEnabled) return false;
        mapRect.GetWorldCorners(MapCorners);
        return ImageVisible(ui.MainImage, ui, canvas) || ImageVisible(ui.ArrowImage, ui, canvas);
    }

    private static bool ImageVisible(Image image, MinimapIconUI owner, Canvas canvas)
    {
        if (!IsAlive(image) || !image.isActiveAndEnabled || !image.transform.IsChildOf(owner.transform) || !IsAlive(image.canvas) ||
            !image.canvas.isActiveAndEnabled || !IsAlive(image.canvasRenderer) || image.canvasRenderer.cull ||
            image.canvas.Pointer != canvas.Pointer ||
            !float.IsFinite(image.color.a) || image.color.a <= 0 ||
            !IsAlive(image.overrideSprite) || !IsAlive(image.rectTransform)) return false;
        float rendererAlpha = image.canvasRenderer.GetAlpha();
        if (!float.IsFinite(rendererAlpha) || rendererAlpha <= 0) return false;
        foreach (var group in image.GetComponentsInParent<CanvasGroup>(false))
        {
            if (!IsAlive(group) || !group.isActiveAndEnabled) continue;
            if (!float.IsFinite(group.alpha) || group.alpha <= 0) return false;
            if (group.ignoreParentGroups) break;
        }
        image.rectTransform.GetWorldCorners(ImageCorners);
        return TryBounds(ImageCorners, out var imageBounds) && TryBounds(MapCorners, out var mapBounds) &&
            imageBounds.Overlaps(mapBounds);
    }

    private static bool TryBounds(Il2CppStructArray<Vector3> corners, out Rect bounds)
    {
        bounds = default;
        float left = float.PositiveInfinity, bottom = float.PositiveInfinity;
        float right = float.NegativeInfinity, top = float.NegativeInfinity;
        foreach (var corner in corners)
        {
            if (!float.IsFinite(corner.x) || !float.IsFinite(corner.y)) return false;
            left = System.MathF.Min(left, corner.x); right = System.MathF.Max(right, corner.x);
            bottom = System.MathF.Min(bottom, corner.y); top = System.MathF.Max(top, corner.y);
        }
        if (right <= left || top <= bottom) return false;
        bounds = new Rect(left, bottom, right - left, top - bottom);
        return true;
    }
}
