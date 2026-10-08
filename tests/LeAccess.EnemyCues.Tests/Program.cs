using System.Numerics;
using LeAccess.Accessibility.Audio;
using LeAccess.Accessibility.Combat;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.Navigation.Guidance;

internal static partial class Program
{
    private const float Range = 10;
    private static readonly Vector3 Player = new(5, 2, 5);
    private static readonly Vector3 Up = new(0, 0, 1);
    private static readonly Vector3 Right = new(1, 0, 0);
    private static int _checks;

    private static int Main()
    {
        try
        {
            Cadence();
            Bearing();
            SelectionStreams();
            SelectionSight();
            SelectionFilters();
            SelectionClasses();
            SelectionElites();
            TrackerCadence();
            TrackerFairness();
            TrackerContinuity();
            TrackerResets();
            SimulatedPulses();
            SharedArbitration();
            Synthesis();
            Console.WriteLine($"Enemy cue tests passed: {_checks} checks; no audio device opened.");
            return 0;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }

    private static void Cadence()
    {
        Close(0.15, EntityCueCadence.Interval(0, Range), "Contact uses D2R's 150 ms near interval.");
        Close(0.45, EntityCueCadence.Interval(Range, Range), "The range edge uses D2R's 450 ms far interval.");
        Close(0.30, EntityCueCadence.Interval(Range / 2, Range), "The interval shrinks linearly with distance.");
        Check(EntityCueCadence.Interval(2, Range) < EntityCueCadence.Interval(3, Range), "Closer enemies repeat faster.");
        Check(EnemyCueSynthesizer.DurationSeconds < EntityCueCadence.GapSeconds,
            "A full cue fits between rapid starts without an audio backlog.");
    }

    private static void Bearing()
    {
        GuidanceGeometry.Bearing(new Vector3(3, 0, 0), Up, Right, out float pan, out float ahead);
        Check(pan == 1 && ahead == 0, "An enemy to the screen right pans fully right.");
        GuidanceGeometry.Bearing(new Vector3(0, 7, -2), Up, Right, out pan, out ahead);
        Check(pan == 0 && ahead == -1, "An enemy screen-down is behind; height is ignored.");
        GuidanceGeometry.Bearing(new Vector3(-1, 0, 1), Up, Right, out pan, out ahead);
        Check(Math.Abs(pan + MathF.Sqrt(0.5f)) < 1e-6 && Math.Abs(ahead - MathF.Sqrt(0.5f)) < 1e-6,
            "A diagonal bearing splits between pan and forward.");
        GuidanceGeometry.Bearing(new Vector3(4, 0, 0), new Vector3(2, 0.5f, 0), new Vector3(0, 0, -3), out pan, out ahead);
        Check(pan == 0 && ahead == 1, "Rotating the camera rotates the bearing; axes need not be unit length.");
        GuidanceGeometry.Bearing(Vector3.Zero, Up, Right, out pan, out ahead);
        Check(pan == 0 && ahead == 0, "An enemy on the player has a centered bearing.");
        GuidanceGeometry.Bearing(new Vector3(float.NaN, 0, 1), Up, Right, out pan, out ahead);
        Check(pan == 0 && ahead == 0, "A non-finite offset is centered rather than NaN.");
        GuidanceGeometry.Bearing(new Vector3(1, 0, 0), Vector3.Zero, Right, out pan, out ahead);
        Check(pan == 0 && ahead == 0, "A missing camera axis is centered.");
    }

    private static EnemyContact At(int id, float dx, float dz, bool boss = false, float dy = 0,
        EnemyClass enemyClass = EnemyClass.Normal) =>
        new(id, null, boss ? EnemyClass.Boss : enemyClass, Player + new Vector3(dx, dy, dz));

    private static List<EnemyCue> Select(IReadOnlyList<EnemyContact> contacts, bool bosses = true, bool ordinary = true,
        Func<EnemyContact, TerrainSight> sight = null, float range = Range, Vector3? player = null, bool elites = true)
    {
        var selected = new List<EnemyCue>();
        new EnemyCueSelector().Select(contacts, player ?? Player, Up, Right, range, bosses, ordinary,
            sight ?? (_ => TerrainSight.Clear), selected, elites);
        return selected;
    }

    private static void SelectionStreams()
    {
        Check(Select(Array.Empty<EnemyContact>()).Count == 0, "No enemies give no streams.");
        var pack = new[] { At(5, 0, 5), At(1, 0, 1), At(4, 0, 4), At(2, 0, 2), At(3, 0, 3) };
        var selected = Select(pack);
        Check(selected.Select(cue => cue.Id).SequenceEqual(new[] { 1, 2, 3 }),
            "A pack is heard as its three nearest members, nearest first.");
        Check(selected.Select(cue => cue.Slot).SequenceEqual(new[] { 1, 2, 3 }), "Each ordinary rank has its own slot.");
        Check(selected.All(cue => cue.Kind == EnemyCueKind.Ordinary), "Ordinary enemies growl.");

        var withBoss = Select(pack.Append(At(9, 0, 8, boss: true)).ToArray());
        Check(withBoss.Count == EnemyCueSelector.MaxStreams, "A boss and a pack never exceed three streams.");
        Check(withBoss[0].Id == 9 && withBoss[0].Kind == EnemyCueKind.Boss && withBoss[0].Slot == EnemyCueSelector.BossSlot,
            "The boss is the highest priority stream even when farther than the pack.");
        Check(withBoss.Skip(1).Select(cue => cue.Id).SequenceEqual(new[] { 1, 2 }) &&
            withBoss.Skip(1).Select(cue => cue.Slot).SequenceEqual(new[] { 1, 2 }),
            "The streams the boss leaves go to the nearest ordinary ranks.");

        var bosses = Select(new[] { At(7, 0, 6, true), At(6, 0, 3, true), At(8, 0, 9, true) });
        Check(bosses.Count == 1 && bosses[0].Id == 6, "Several bosses make one stream: the nearest boss.");

        var ties = Select(new[] { At(12, 2, 0), At(11, 0, 2), At(10, -2, 0) });
        Check(ties.Select(cue => cue.Id).SequenceEqual(new[] { 10, 11, 12 }), "Equal distances rank by stable identity.");
        Check(Select(ties.Select(cue => At(cue.Id, 0, 1)).Reverse().ToArray())[0].Id == 10,
            "Input order does not change the ranking.");

        var placed = Select(new[] { At(1, -4, 0), At(2, 0, -6) });
        Check(placed[0].Pan == -1 && placed[0].Forward == 0, "A left enemy pans left on the camera's axes.");
        Check(placed[1].Forward == -1, "A screen-down enemy is marked behind for the lower pitch.");
        Close(EntityCueCadence.Interval(4, Range), placed[0].IntervalSeconds, "Each stream's interval follows its distance.");
        var raised = Select(new[] { At(1, 3, 0, dy: 4) });
        Close(5, raised[0].Distance, "Distance is the 3D distance the game's targeting range uses.");
        Check(raised[0].Pan == 1, "Height does not change the screen bearing.");
    }

    private static void SelectionSight()
    {
        var calls = new List<int>();
        TerrainSight Visible(EnemyContact contact)
        { calls.Add(contact.Id); return contact.Id % 2 == 0 ? TerrainSight.Clear : TerrainSight.Blocked; }
        var crowd = Enumerable.Range(1, 8).Select(id => At(id, 0, id)).ToArray();
        var selected = Select(crowd, sight: Visible);
        Check(selected.Select(cue => cue.Id).SequenceEqual(new[] { 2, 4, 6 }), "Enemies behind terrain are skipped.");
        Check(selected.Select(cue => cue.Slot).SequenceEqual(new[] { 1, 2, 3 }), "Ranks count only enemies in sight.");
        Check(calls.SequenceEqual(new[] { 1, 2, 3, 4, 5, 6 }), "Only the nearest candidates pay for a terrain ray.");

        calls.Clear();
        Select(Enumerable.Range(1, 20).Select(id => At(id, 0, id / 3f)).ToArray(), sight: Visible);
        Check(calls.Count == 6, "A large crowd still stops ray tests once three streams are filled.");

        var hiddenBoss = Select(new[] { At(1, 0, 2, true), At(3, 0, 5, true), At(2, 0, 1) },
            sight: contact => contact.Id != 1 ? TerrainSight.Clear : TerrainSight.Blocked);
        Check(hiddenBoss[0].Id == 3 && hiddenBoss[0].Kind == EnemyCueKind.Boss,
            "A hidden boss yields to the nearest boss in sight.");
        Check(Select(new[] { At(1, 0, 2, true) }, sight: _ => TerrainSight.Blocked).Count == 0, "Nothing in sight is silent.");

        var offMesh = Select(new[] { At(1, 0, 2, true), At(2, 0, 1), At(3, 0, 3) },
            sight: contact => contact.Id == 3 ? TerrainSight.Clear : TerrainSight.OffMesh);
        Check(offMesh.Select(cue => cue.Id).SequenceEqual(new[] { 1, 3 }),
            "A boss off the walkable surface stays audible; an ordinary enemy there does not.");
        var growlingBoss = Select(new[] { At(1, 0, 2, true) }, bosses: false, sight: _ => TerrainSight.OffMesh);
        Check(growlingBoss.Count == 1 && growlingBoss[0].Kind == EnemyCueKind.Ordinary,
            "A boss stays audible off the mesh even while it growls.");
        Check(EnemyCueSelector.InSight(At(1, 0, 1), TerrainSight.Clear) &&
            !EnemyCueSelector.InSight(At(1, 0, 1, true), TerrainSight.Blocked) &&
            !EnemyCueSelector.InSight(At(1, 0, 1, true), TerrainSight.Unknown) &&
            !EnemyCueSelector.InSight(At(1, 0, 1), TerrainSight.OffMesh),
            "Walls hide bosses too, and an unknown origin hides everything.");
    }

    private static void SelectionFilters()
    {
        var edge = Select(new[] { At(1, 0, Range), At(2, 0, Range + 0.01f) });
        Check(edge.Count == 1 && edge[0].Id == 1, "The range edge is included and beyond it is excluded.");
        Check(Select(new[] { new EnemyContact(1, null, EnemyClass.Normal, new Vector3(float.NaN, 0, 0)),
            new EnemyContact(2, null, EnemyClass.Normal, new Vector3(float.PositiveInfinity, 0, 0)) }).Count == 0,
            "Non-finite positions are ignored.");
        foreach (float range in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
            Check(Select(new[] { At(1, 0, 1) }, range: range).Count == 0, $"Invalid range {range} gives no streams.");
        Check(Select(new[] { At(1, 0, 1) }, player: new Vector3(float.NaN, 0, 0)).Count == 0,
            "An invalid player position gives no streams.");
        var selected = new List<EnemyCue> { new(9, EnemyCueKind.Boss, 1, 0, 0, 0.2, 0) };
        new EnemyCueSelector().Select(null, Player, Up, Right, Range, true, true, _ => TerrainSight.Clear, selected);
        Check(selected.Count == 0, "A missing registry clears the previous selection.");
    }

    private static void SelectionClasses()
    {
        var mixed = new[] { At(1, 0, 1, true), At(2, 0, 2), At(3, 0, 3) };
        var noBossClass = Select(mixed, bosses: false);
        Check(noBossClass.Select(cue => cue.Id).SequenceEqual(new[] { 1, 2, 3 }) &&
            noBossClass.All(cue => cue.Kind == EnemyCueKind.Ordinary),
            "With the boss class off, a boss growls among the ordinary ranks.");
        var bossesOnly = Select(mixed, ordinary: false);
        Check(bossesOnly.Count == 1 && bossesOnly[0].Id == 1, "With ordinary cues off, only the boss sounds.");
        Check(Select(mixed, bosses: false, ordinary: false).Count == 0, "Both classes off is silent.");
    }

    private static EnemyCue Cue(int id, int slot, double interval, EnemyCueKind kind = EnemyCueKind.Ordinary) =>
        new(id, kind, 1, 0, 1, interval, slot);

    private static void TrackerCadence()
    {
        var tracker = new EnemyCueTracker();
        var near = new[] { Cue(1, 1, 0.15) };
        Check(Take(tracker, near, 0) == 1, "The first cue plays immediately.");
        Check(Take(tracker, near, 0.149) == null, "A repeat waits for the stream's interval.");
        Check(Take(tracker, near, 0.15) == 1, "The repeat plays once the interval elapses.");
        var far = new[] { Cue(1, 1, 0.45) };
        Check(Take(tracker, far, 0.5) == null && Take(tracker, far, 0.61) == 1,
            "Moving away lengthens the wait for the same stream.");

        var gap = new EnemyCueTracker();
        var two = new[] { Cue(1, 1, 0.15), Cue(2, 2, 0.15) };
        Check(Take(gap, two, 0) == 1, "The nearest rank starts first.");
        Check(Take(gap, two, 0.05) == null, "Any two starts are at least 100 ms apart.");
        Check(Take(gap, two, 0.1) == 2, "The second stream starts after the gap.");
        Check(!gap.TryChoose(two, double.NaN, out _), "A non-finite clock plays nothing.");
        Check(!gap.TryChoose(null, 1, out _), "A missing selection plays nothing.");
    }

    private static void TrackerFairness()
    {
        var tracker = new EnemyCueTracker();
        var selection = new List<EnemyCue> { Cue(9, 0, 0.15, EnemyCueKind.Boss) };
        Check(Take(tracker, selection, 0) == 9, "A boss alone plays at once.");
        selection.Add(Cue(1, 1, 0.45));
        Check(Take(tracker, selection, 0.2) == 1, "A newly selected stream plays before a due repeat.");
        int boss = 0, ordinary = 0;
        for (int frame = 18; frame < 198; frame++)
        {
            int? id = Take(tracker, selection, frame / 60.0);
            if (id == 9) boss++; else if (id == 1) ordinary++;
        }
        Check(boss >= 10 && ordinary >= 5, $"A nearby boss does not starve a farther enemy (boss {boss}, ordinary {ordinary}).");
    }

    private static void TrackerContinuity()
    {
        var tracker = new EnemyCueTracker();
        Check(Take(tracker, new[] { Cue(1, 1, 0.3) }, 0) == 1, "The nearest enemy starts rank one.");
        var swapped = new[] { Cue(2, 1, 0.3) };
        Check(Take(tracker, swapped, 0.1) == null && Take(tracker, swapped, 0.2) == null,
            "A new nearest enemy inherits the rank's cadence instead of bursting.");
        Check(Take(tracker, swapped, 0.3) == 2, "The next pulse uses the newly selected enemy.");
        Check(Take(tracker, Array.Empty<EnemyCue>(), 0.4) == null, "An empty selection leaves nothing pending.");
        Check(Take(tracker, swapped, 0.5) == null && Take(tracker, swapped, 0.61) == 2,
            "A rank back after a one-poll absence keeps its cadence instead of jumping the queue.");

        var blink = new EnemyCueTracker();
        var pair = new[] { Cue(1, 1, 0.45), Cue(2, 2, 0.25) };
        Check(Take(blink, pair, 0) == 1 && Take(blink, pair, 0.1) == 2, "Both streams start.");
        Check(Take(blink, new[] { pair[1] }, 0.2) == null, "Rank one drops out for one poll.");
        Check(Take(blink, pair, 0.3) == null, "A briefly absent stream does not get first-cue priority on return.");
        Check(Take(blink, pair, 0.36) == 2 && Take(blink, pair, 0.47) == 1,
            "Both streams keep their own intervals across the absence.");

        var gone = new EnemyCueTracker();
        Take(gone, new[] { Cue(1, 1, 0.45) }, 0);
        Take(gone, Array.Empty<EnemyCue>(), 0.1);
        Take(gone, Array.Empty<EnemyCue>(), 0.1 + EntityCueCadence.FarIntervalSeconds);
        var second = new[] { Cue(1, 1, 5), Cue(2, 2, 5) };
        Check(Take(gone, second, 0.6) == 1, "A rank absent longer than the far interval is new again and plays first.");
    }

    private static void TrackerResets()
    {
        var tracker = new EnemyCueTracker();
        var near = new[] { Cue(1, 1, 0.45) };
        Take(tracker, near, 1);
        tracker.Reset();
        Check(Take(tracker, near, 1.05) == 1, "Suspension resets cadence and the start gap.");
        Check(Take(tracker, near, 0.5) == 1, "A clock that moves backward resets timing instead of stalling.");
        tracker.MarkPlayed(Cue(3, EnemyCueSelector.SlotCount, 0.2), 0.6);
        tracker.MarkPlayed(Cue(3, -1, 0.2), 0.6);
        Check(Take(tracker, new[] { Cue(4, 2, 0.2) }, 0.65) == 4, "Invalid slots are ignored.");
    }

    private static int? Take(EnemyCueTracker tracker, IReadOnlyList<EnemyCue> selected, double now)
    {
        if (!tracker.TryChoose(selected, now, out var cue)) return null;
        tracker.MarkPlayed(cue, now);
        return cue.Id;
    }

    // Snapshots every 100 ms, start checks every frame, playback through the shared reservation.
    private static void SimulatedPulses()
    {
        const double Frame = 1.0 / 60;
        const double BufferMilliseconds = 50;
        WorldAudioReservation.Reset();
        var tracker = new EnemyCueTracker();
        var selector = new EnemyCueSelector();
        var selected = new List<EnemyCue>();
        var pack = new[] { At(1, 0, 0.5f), At(2, 0.6f, 0), At(3, -0.7f, 0) };
        var starts = new Dictionary<int, List<double>>();
        double nextPoll = 0;
        for (int frame = 1; frame <= 180; frame++)
        {
            double now = frame * Frame;
            if (now >= nextPoll)
            {
                nextPoll = now + EntityCueCadence.PollSeconds;
                selector.Select(pack, Player, Up, Right, Range, true, true, _ => TerrainSight.Clear, selected);
            }
            if (!tracker.TryChoose(selected, now, out var cue) ||
                !WorldAudioReservation.TryBegin(WorldAudioSource.Enemy, now, EnemyCueSynthesizer.DurationSeconds, BufferMilliseconds))
                continue;
            tracker.MarkPlayed(cue, now);
            (starts.TryGetValue(cue.Slot, out var list) ? list : starts[cue.Slot] = new()).Add(now);
        }
        Check(starts.Count == 3, "Three nearby enemies each get their own stream.");
        var all = starts.Values.SelectMany(times => times).OrderBy(time => time).ToList();
        Check(all.Zip(all.Skip(1)).All(pair => pair.Second - pair.First >= EnemyCueSynthesizer.DurationSeconds + BufferMilliseconds / 1000 - 1e-9),
            "Starts never overlap the reserved cue and buffer time.");
        Check(starts.Values.All(times => times.Zip(times.Skip(1)).All(pair => pair.Second - pair.First >= 0.15)),
            "No stream repeats faster than its near interval.");
        Check(starts.Values.All(times => times.Count >= 6), "Each nearby enemy keeps repeating, alternating with the others.");
        WorldAudioReservation.Reset();
    }

    private static void SharedArbitration()
    {
        WorldAudioReservation.Reset();
        Check(WorldAudioReservation.TryBegin(WorldAudioSource.Terrain, 0, 0.12, 100), "A radar knock takes the channel.");
        Check(!WorldAudioReservation.TryBegin(WorldAudioSource.Enemy, 0.05, EnemyCueSynthesizer.DurationSeconds, 50),
            "An enemy cue waits instead of overlapping the knock.");
        Check(!WorldAudioReservation.TryBegin(WorldAudioSource.Npc, 0.1, NpcCueSynthesizer.DurationSeconds, 50),
            "An NPC tap queues behind the waiting enemy.");
        Check(!WorldAudioReservation.TryBegin(WorldAudioSource.Npc, 0.23, NpcCueSynthesizer.DurationSeconds, 50),
            "The NPC cannot jump the earlier enemy request.");
        Check(WorldAudioReservation.TryBegin(WorldAudioSource.Enemy, 0.23, EnemyCueSynthesizer.DurationSeconds, 50),
            "The enemy plays when the knock and its buffer end.");
        Check(!WorldAudioReservation.TryBegin(WorldAudioSource.Enemy, 0.38, EnemyCueSynthesizer.DurationSeconds, 50),
            "A repeating enemy cannot take the next turn from the waiting NPC.");
        Check(WorldAudioReservation.TryBegin(WorldAudioSource.Npc, 0.38, NpcCueSynthesizer.DurationSeconds, 50),
            "The NPC tap follows the enemy cue.");
        Check(WorldAudioReservation.TryBegin(WorldAudioSource.Terrain, 0.4, 0.12, 100),
            "A radar knock never waits behind other world sounds.");
        WorldAudioReservation.Cancel(WorldAudioSource.Enemy);
        Check(!WorldAudioReservation.TryBegin(WorldAudioSource.Enemy, 0.52, EnemyCueSynthesizer.DurationSeconds, 50),
            "Other sounds still wait for the knock and its buffer.");
        Check(WorldAudioReservation.TryBegin(WorldAudioSource.Enemy, 0.63, EnemyCueSynthesizer.DurationSeconds, 50),
            "The enemy cue plays once the knock ends.");
        WorldAudioReservation.Reset();
    }
}
