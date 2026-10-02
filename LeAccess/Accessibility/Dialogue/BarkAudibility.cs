using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Dialogue;

internal static class BarkAudibility
{
    private static readonly Il2CppStructArray<Vector3> Corners = new(4);

    internal static bool Allows(FloatingBark bark, FloatingTextLineUI ui, out bool onScreen,
        out bool inRange, out bool ownVoice, out bool ownsLabel)
    {
        onScreen = inRange = ownVoice = ownsLabel = false;
        var player = PlayerFinder.player;
        var ownedUi = IsAlive(ui) ? ui.TryCast<FloatingBarkUI>() : null;
        if (!IsAlive(ownedUi) || !IsAlive(ownedUi.bark) || ownedUi.bark.Pointer != bark.Pointer ||
            !bark.gameObject.scene.isLoaded) return false;
        var label = ui.textComponent;
        if (!IsAlive(label) || !label.transform.IsChildOf(ui.transform)) return false;
        ownsLabel = true;

        onScreen = IsOnScreen(bark, label);
        if (!IsAlive(player) || !player.activeInHierarchy ||
            (int)bark.gameObject.scene.handle != (int)player.scene.handle) return onScreen;
        ownVoice = bark.transform.IsChildOf(player.transform);
        if (!ownVoice)
        {
            var listener = bark.GetComponentInParent<WorldObjectClickListener>();
            if (IsAlive(listener) && listener.isActiveAndEnabled && listener.gameObject.activeInHierarchy &&
                (int)listener.gameObject.scene.handle == (int)player.scene.handle)
            {
                float range = listener.interactionRange;
                Vector3 destination = listener.transform.position;
                float distance = (destination - player.transform.position).sqrMagnitude;
                inRange = float.IsFinite(range) && range > 0 && float.IsFinite(distance) &&
                    distance <= (double)range * range;
            }
        }
        return ownVoice || onScreen || inRange;
    }

    private static bool IsOnScreen(FloatingBark bark, TMP_Text label)
    {
        var mainCamera = CameraManager.getMainCamera();
        if (!IsAlive(mainCamera) || !mainCamera.isActiveAndEnabled) return false;
        // Floating UI is already projected; only depth, not the NPC's screen X/Y,
        // can reject its source before checking the actual label rectangle.
        float sourceDepth = mainCamera.WorldToScreenPoint(bark.transform.position + bark.offset).z;
        if (!float.IsFinite(sourceDepth) || sourceDepth <= 0) return false;
        var rect = label.rectTransform;
        var canvas = label.GetComponentInParent<Canvas>();
        if (!IsAlive(rect) || !IsAlive(canvas) || !canvas.isActiveAndEnabled) return false;
        canvas = canvas.rootCanvas;
        if (!IsAlive(canvas) || !canvas.isActiveAndEnabled) return false;
        Camera renderCamera = null;
        Rect viewport;
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            viewport = new Rect(0, 0, Screen.width, Screen.height);
        else
        {
            renderCamera = canvas.worldCamera;
            if (!IsAlive(renderCamera) || !renderCamera.isActiveAndEnabled) return false;
            viewport = renderCamera.pixelRect;
        }
        if (!Finite(viewport.xMin, viewport.yMin) || !Finite(viewport.xMax, viewport.yMax) ||
            viewport.width <= 0 || viewport.height <= 0) return false;
        rect.GetWorldCorners(Corners);
        float left = float.PositiveInfinity, bottom = float.PositiveInfinity;
        float right = float.NegativeInfinity, top = float.NegativeInfinity;
        for (int i = 0; i < Corners.Length; i++)
        {
            Vector3 corner = Corners[i];
            if (!Finite(corner.x, corner.y) || !float.IsFinite(corner.z)) return false;
            if (renderCamera != null)
            {
                float depth = renderCamera.WorldToScreenPoint(corner).z;
                if (!float.IsFinite(depth) || depth <= 0) return false;
            }
            var pixel = RectTransformUtility.WorldToScreenPoint(renderCamera, corner);
            if (!Finite(pixel.x, pixel.y)) return false;
            left = System.MathF.Min(left, pixel.x); right = System.MathF.Max(right, pixel.x);
            bottom = System.MathF.Min(bottom, pixel.y); top = System.MathF.Max(top, pixel.y);
        }
        return right > left && top > bottom && right > viewport.xMin && left < viewport.xMax &&
            top > viewport.yMin && bottom < viewport.yMax;
    }

    private static bool Finite(float x, float y) => float.IsFinite(x) && float.IsFinite(y);
}
