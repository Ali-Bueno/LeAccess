using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LeAccess.Accessibility.Navigation.Guidance;

namespace LeAccess.Accessibility.Combat;

internal readonly record struct EnemyContact(int Id, string Name, EnemyClass Class, Vector3 Position)
{
    internal bool Boss => Class == EnemyClass.Boss;
}

// Sorts, limits and words the on-demand enemy scan; see reference/enemy-scan.md.
internal static class EnemyScanReport
{
    internal const int MaxListed = 3;

    internal static string Build(IEnumerable<EnemyContact> contacts, Vector3 player, Vector3 forward, Vector3 right,
        EnemyScanText text)
    {
        var nearest = contacts.Where(contact => GuidanceGeometry.Finite(contact.Position))
            .Select(contact => (Contact: contact, Distance: Vector3.DistanceSquared(contact.Position, player)))
            .Where(item => float.IsFinite(item.Distance))
            .OrderBy(item => item.Distance).ThenBy(item => item.Contact.Id).ToList();
        if (nearest.Count == 0) return text("enemy_scan.none");
        var parts = new List<string>
        {
            text(nearest.Count == 1 ? "enemy_scan.count_one" : "enemy_scan.count_many", nearest.Count)
        };
        foreach (var (contact, _) in nearest.Take(MaxListed)) parts.Add(Describe(contact, player, forward, right, text));
        return string.Join(". ", parts);
    }

    private static string Describe(EnemyContact contact, Vector3 player, Vector3 forward, Vector3 right, EnemyScanText text)
    {
        var delta = contact.Position - player;
        string name = string.IsNullOrWhiteSpace(contact.Name) ? text("enemy_scan.unnamed") : contact.Name;
        name = EnemyClassifier.Decorate(name, contact.Class, text);
        string direction = text(GuidanceGeometry.Direction(delta, forward, right) ?? "enemy_scan.direction_here");
        int units = (int)Math.Round(new Vector2(delta.X, delta.Z).Length(), MidpointRounding.AwayFromZero);
        return text(contact.Boss ? "enemy_scan.entry_boss" : "enemy_scan.entry", name, direction, units);
    }
}
