using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.WorldCues;

// The shrine objects the active scene's ShrinesManager placed, shared by sounds, landmarks and the scanner.
internal static class ShrineObjects
{
    // Shrines can appear during play (Monolith), so their objects are looked up again at this pace.
    private const double RefreshSeconds = 5;
    private static readonly List<GameObject> Objects = new();
    private static int _scene;
    private static double _next;

    /// <summary>Whether the listener is part of a placed shrine object, above or below it.</summary>
    internal static bool Contains(WorldObjectClickListener listener)
    {
        if (!IsAlive(listener)) return false;
        Refresh(SceneManager.GetActiveScene(), Time.unscaledTimeAsDouble);
        var transform = listener.transform;
        foreach (var shrine in Objects)
            if (IsAlive(shrine) && (transform.IsChildOf(shrine.transform) || shrine.transform.IsChildOf(transform))) return true;
        return false;
    }

    internal static void Reset()
    {
        Objects.Clear();
        _scene = 0;
        _next = 0;
    }

    private static void Refresh(Scene scene, double now)
    {
        if ((int)scene.handle == _scene && now < _next) return;
        _scene = (int)scene.handle;
        _next = now + RefreshSeconds;
        Objects.Clear();
        if (!scene.IsValid() || !ShrinesManager.TryGetForScene(scene.name, out var manager) || !IsAlive(manager)) return;
        var objects = manager.GetAllShrineObjects();
        if (objects is null || objects.WasCollected) return;
        for (int index = 0; index < objects.Count; index++)
            if (IsAlive(objects[index])) Objects.Add(objects[index]);
    }
}
