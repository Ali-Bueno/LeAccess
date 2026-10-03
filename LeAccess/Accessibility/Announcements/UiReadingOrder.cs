using System;
using System.Collections.Generic;
using UnityEngine;

namespace LeAccess.Accessibility.Announcements;

internal static class UiReadingOrder
{
    internal readonly record struct Fragment(Component Label, string Text);
    private readonly record struct Positioned<T>(T Value, Rect Bounds, int Index);

    internal static List<string> Read(List<Fragment> fragments) =>
        Order(fragments, fragment => fragment.Label).ConvertAll(fragment => fragment.Text);

    internal static List<T> Order<T>(List<T> items, Func<T, Component> label)
    {
        var result = new List<T>();
        var run = new List<Positioned<T>>();
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var component = label(item);
            if (!Navigation.NavigationObjectLifetime.IsAlive(component)) continue;
            if (UiScreenGeometry.TryGetScreenRect(component.transform.TryCast<RectTransform>(), out var bounds))
                run.Add(new Positioned<T>(item, bounds, i));
            else
            {
                Append(run, result);
                result.Add(item);
            }
        }
        Append(run, result);
        return result;
    }

    private static void Append<T>(List<Positioned<T>> remaining, List<T> result)
    {
        remaining.Sort((a, b) => a.Bounds.yMax != b.Bounds.yMax
            ? b.Bounds.yMax.CompareTo(a.Bounds.yMax) : a.Index.CompareTo(b.Index));
        var row = new List<Positioned<T>>();
        while (remaining.Count != 0)
        {
            row.Clear();
            var first = remaining[0];
            float bottom = first.Bounds.yMin, top = first.Bounds.yMax;
            for (int i = 0; i < remaining.Count;)
            {
                var item = remaining[i];
                float overlapBottom = Math.Max(bottom, item.Bounds.yMin);
                float overlapTop = Math.Min(top, item.Bounds.yMax);
                if (row.Count == 0 || overlapBottom < overlapTop)
                {
                    row.Add(item); remaining.RemoveAt(i);
                    bottom = overlapBottom; top = overlapTop;
                }
                else i++;
            }
            row.Sort((a, b) => a.Bounds.xMin != b.Bounds.xMin
                ? a.Bounds.xMin.CompareTo(b.Bounds.xMin) : a.Index.CompareTo(b.Index));
            foreach (var item in row) result.Add(item.Value);
        }
    }
}
