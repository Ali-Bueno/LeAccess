using System.Collections.Generic;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.I18n;

namespace LeAccess.Accessibility.Panels;

/// <summary>What the Weaver tree node and the Forgotten Knights timeline readers say; the game's own text goes in as arguments.</summary>
/// <remarks>See reference/faction-vendor-panels.md#weaver-tree.</remarks>
internal static class FactionTreeText
{
    internal readonly record struct Neighbour(string Name, string DirectionKey);

    /// <summary>The state of the selected node and of the tree's points, to notice what a point changed.</summary>
    internal readonly record struct NodeState(long Node, int Points, int Max, int Unspent, bool RequirementsMet, string Pending);

    internal static string Node(string name, int points, int max, bool requirementsMet, string effect) =>
        Combine(name, ModText.Format("node.points", points, max), requirementsMet ? null : ModText.Get("node.locked"), effect);

    /// <summary>After a point is spent or returned on the same node: its points, then what the tree has left.</summary>
    internal static string Changes(NodeState before, NodeState now) => Combine(
        now.Points != before.Points ? ModText.Format("node.points", now.Points, now.Max) : null,
        now.RequirementsMet != before.RequirementsMet && !now.RequirementsMet ? ModText.Get("node.locked") : null,
        now.Unspent != before.Unspent ? PointsLeft(now.Unspent) : null,
        now.Pending != before.Pending ? now.Pending : null);

    internal static string TreeIntro(int unspent, string pending) =>
        Combine(ModText.Get("weaver.tree"), PointsLeft(unspent), pending);

    internal static string PointsLeft(int unspent) => ModText.Format("weaver.points_left", unspent);

    /// <summary>The held-detail reading: the effect lines, the requirements and the connected nodes.</summary>
    internal static string Details(IEnumerable<string> effect, IEnumerable<string> requirements, IEnumerable<Neighbour> neighbours)
    {
        var linked = new List<string>();
        foreach (var neighbour in neighbours)
            linked.Add(ModText.Format("weaver.neighbour", neighbour.Name, ModText.Get(neighbour.DirectionKey)));
        return Combine(Combine(effect), Combine(requirements),
            linked.Count == 0 ? null : ModText.Format("weaver.connected", string.Join("; ", linked)));
    }

    internal static string Timeline(string island, string harbinger, bool defeated, int position, int count) =>
        Combine(island, harbinger, ModText.Get(defeated ? "weaver.timeline_defeated" : "weaver.timeline_pending"),
            count > 1 && position > 0 ? ModText.Format("list.position", position, count) : null);

    internal static string TimelinesIntro(IEnumerable<string> gameLines, int defeated, int count) =>
        Combine(Combine(gameLines), count > 0 ? ModText.Format("weaver.timelines_done", defeated, count) : null);

    private static string Combine(params string[] parts) => Combine((IEnumerable<string>)parts);

    private static string Combine(IEnumerable<string> parts)
    {
        string text = ScreenContentTracker.Combine(parts);
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}
