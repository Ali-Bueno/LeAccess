using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Announcements;

internal static class UiScreenGeometry
{
    private enum ProjectionState { Unknown, Hidden, Ready }
    private static readonly Il2CppStructArray<Vector3> Corners = new(4);

    internal static bool IsVisible(GameObject element)
    {
        // A layout rectangle does not clip independently rendered descendants.
        if (!IsAlive(element.GetComponent<Graphic>())) return true;
        var rect = element.transform.TryCast<RectTransform>();
        var canvas = element.GetComponentInParent<Canvas>();
        // Layout-only roots may have no rectangle or a zero-sized anchor.
        if (!IsAlive(rect) || !IsAlive(canvas) || rect.rect.width == 0 || rect.rect.height == 0) return true;
        var projection = ReadViewport(canvas, out var camera, out var viewport);
        if (projection != ProjectionState.Ready) return projection == ProjectionState.Unknown;
        if (!TryBounds(rect, camera, out var bounds) || !Intersect(ref bounds, viewport)) return false;

        var graphic = element.GetComponent<MaskableGraphic>();
        if (IsAlive(graphic) && !graphic.maskable) return true;
        var ownCanvas = element.GetComponent<Canvas>();
        if (IsAlive(ownCanvas) && ownCanvas.overrideSorting) return true;

        for (var parent = rect.parent; IsAlive(parent); parent = parent.parent)
        {
            var clip = parent.GetComponent<RectMask2D>();
            if (IsAlive(clip) && clip.isActiveAndEnabled &&
                (!TryBounds(clip.rectTransform, camera, out var clipping, clip.padding) ||
                 !Intersect(ref bounds, clipping))) return false;
            var mask = parent.GetComponent<Mask>();
            if (IsAlive(mask) && mask.MaskEnabled() && IsAlive(mask.graphic) && mask.graphic.isActiveAndEnabled &&
                (!TryBounds(mask.rectTransform, camera, out var masking) ||
                 !Intersect(ref bounds, masking))) return false;
            // Unity's clipping/stencil ancestry ends at an override-sorting canvas.
            var boundary = parent.GetComponent<Canvas>();
            if (IsAlive(boundary) && boundary.overrideSorting) break;
        }
        return true;
    }

    internal static bool IsVisibleGraphic(Component element)
    {
        var graphic = element.TryCast<Graphic>();
        if (!IsAlive(graphic) || !graphic.isActiveAndEnabled ||
            !float.IsFinite(graphic.color.a) || graphic.color.a <= 0) return false;
        var renderer = graphic.canvasRenderer;
        if (IsAlive(renderer) && (renderer.cull || !float.IsFinite(renderer.GetAlpha()) || renderer.GetAlpha() <= 0))
            return false;
        var rect = graphic.rectTransform;
        if (!IsAlive(graphic.canvas)) return true;
        if (!IsAlive(rect)) return false;
        if (rect.rect.width > 0 && rect.rect.height > 0) return true;
        // Overflowing text in a layout that sizes one axis keeps a zero rectangle yet renders
        // (main-menu PlayOfflineButton label height 0); its own text bounds prove presence.
        var text = graphic.TryCast<TMP_Text>();
        if (!IsAlive(text)) return false;
        var size = text.textBounds.size;
        return float.IsFinite(size.x) && float.IsFinite(size.y) && size.x > 0 && size.y > 0;
    }

    internal static bool HasVisiblePresentation(GameObject element)
    {
        var graphics = element.GetComponentsInChildren<Graphic>(true);
        if (graphics is null || graphics.WasCollected) return false;
        if (graphics.Length == 0) return true;
        foreach (var graphic in graphics)
            if (UiVisibility.IsVisibleGraphic(graphic)) return true;
        return false;
    }

    internal static bool TryGetScreenRect(RectTransform rect, out Rect bounds)
    {
        bounds = default;
        try
        {
            return IsAlive(rect) && ReadViewport(rect.GetComponentInParent<Canvas>(), out var camera, out _) == ProjectionState.Ready &&
                TryBounds(rect, camera, out bounds);
        }
        catch (System.Exception) { return false; }
    }

    private static ProjectionState ReadViewport(Canvas canvas, out Camera camera, out Rect viewport)
    {
        camera = null;
        viewport = default;
        if (!IsAlive(canvas)) return ProjectionState.Unknown;
        if (!canvas.isActiveAndEnabled) return ProjectionState.Hidden;
        var root = canvas.rootCanvas;
        if (!IsAlive(root)) return ProjectionState.Unknown;
        if (!root.isActiveAndEnabled) return ProjectionState.Hidden;
        // World-space worldCamera identifies an event camera, not the cameras rendering the canvas.
        if (root.renderMode == RenderMode.WorldSpace) return ProjectionState.Unknown;
        if (root.renderMode != RenderMode.ScreenSpaceOverlay) camera = root.worldCamera;
        if (IsAlive(camera))
        {
            if (!camera.isActiveAndEnabled) return ProjectionState.Hidden;
            viewport = camera.pixelRect;
        }
        else
        {
            camera = null;
            viewport = new Rect(0, 0, Screen.width, Screen.height);
        }
        if (!Finite(viewport.xMin, viewport.yMin) || !Finite(viewport.xMax, viewport.yMax))
            return ProjectionState.Unknown;
        return viewport.width > 0 && viewport.height > 0 ? ProjectionState.Ready : ProjectionState.Hidden;
    }

    private static bool TryBounds(RectTransform rect, Camera camera, out Rect bounds, Vector4 padding = default)
    {
        bounds = default;
        if (!IsAlive(rect)) return false;
        if (padding.x != 0 || padding.y != 0 || padding.z != 0 || padding.w != 0)
        {
            var local = rect.rect;
            float left = local.xMin + padding.x, bottom = local.yMin + padding.y;
            float right = local.xMax - padding.z, top = local.yMax - padding.w;
            if (right <= left || top <= bottom) return false;
            Corners[0] = rect.TransformPoint(new Vector3(left, bottom, 0));
            Corners[1] = rect.TransformPoint(new Vector3(left, top, 0));
            Corners[2] = rect.TransformPoint(new Vector3(right, top, 0));
            Corners[3] = rect.TransformPoint(new Vector3(right, bottom, 0));
        }
        else rect.GetWorldCorners(Corners);
        float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
        for (int i = 0; i < Corners.Length; i++)
        {
            var corner = Corners[i];
            if (!Finite(corner.x, corner.y) || !float.IsFinite(corner.z)) return false;
            if (camera != null)
            {
                float depth = camera.WorldToScreenPoint(corner).z;
                if (!float.IsFinite(depth) || depth <= 0) return false;
            }
            var point = RectTransformUtility.WorldToScreenPoint(camera, corner);
            if (!Finite(point.x, point.y)) return false;
            minX = System.MathF.Min(minX, point.x); maxX = System.MathF.Max(maxX, point.x);
            minY = System.MathF.Min(minY, point.y); maxY = System.MathF.Max(maxY, point.y);
        }
        if (maxX <= minX || maxY <= minY) return false;
        bounds = new Rect(minX, minY, maxX - minX, maxY - minY);
        return true;
    }

    private static bool Intersect(ref Rect bounds, Rect clip)
    {
        float left = System.MathF.Max(bounds.xMin, clip.xMin), right = System.MathF.Min(bounds.xMax, clip.xMax);
        float bottom = System.MathF.Max(bounds.yMin, clip.yMin), top = System.MathF.Min(bounds.yMax, clip.yMax);
        if (right <= left || top <= bottom) return false;
        bounds = new Rect(left, bottom, right - left, top - bottom);
        return true;
    }

    private static bool Finite(float x, float y) => float.IsFinite(x) && float.IsFinite(y);
}
