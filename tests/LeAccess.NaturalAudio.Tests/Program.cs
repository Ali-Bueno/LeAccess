using System.Text;
using LeAccess.Accessibility.Audio;
using LeAccess.Accessibility.WorldCues;

int checks = 0;
void Check(bool value, string message)
{ checks++; if (!value) throw new InvalidOperationException(message); }
void Reject(byte[] value, string message)
{
    bool rejected = false;
    try { PcmWaveReader.ReadMono48(new MemoryStream(value)); }
    catch (InvalidDataException) { rejected = true; }
    Check(rejected, message);
}
byte[] Wave(short[] samples, Action<BinaryWriter> extra = null)
{
    using var stream = new MemoryStream();
    using var writer = new BinaryWriter(stream, Encoding.ASCII, true);
    writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(0); writer.Write(Encoding.ASCII.GetBytes("WAVE"));
    extra?.Invoke(writer);
    writer.Write(Encoding.ASCII.GetBytes("fmt ")); writer.Write(16);
    writer.Write((short)1); writer.Write((short)1); writer.Write(48000); writer.Write(96000);
    writer.Write((short)2); writer.Write((short)16);
    writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(samples.Length * 2);
    foreach (short sample in samples) writer.Write(sample);
    stream.Position = 4; writer.Write((int)stream.Length - 8);
    return stream.ToArray();
}

if (args.Contains("--missing"))
{
    Check(!NaturalCueBank.TryBuild(NaturalCueKind.Gold, 0, 1, 1, out var absent) && absent.Length == 0,
        "A missing resource returns an empty, non-throwing fallback signal");
    string failure = NaturalCueBank.LastFailure;
    Check(failure?.StartsWith("gold:") == true && NaturalCueBank.Duration(NaturalCueKind.Gold) == 0,
        "Missing-resource diagnostics identify the failed asset and reserve no audio");
    Check(!NaturalCueBank.TryBuild(NaturalCueKind.Gold, 0, 1, 1, out _) && NaturalCueBank.LastFailure == failure,
        "Missing resource failure is cached instead of repeatedly reading/throwing");
    foreach (WorldCueCategory category in Enum.GetValues<WorldCueCategory>())
    {
        var fallback = WorldCueSynthesizer.Synthesize(category, -1, -1, 1, NaturalCueKind.Chest);
        Check(fallback.Length == Math.Round(WorldCueSynthesizer.Duration(category, NaturalCueKind.Chest) * 96000) &&
            fallback.Any(x => x != 0) && fallback.All(float.IsFinite), $"{category} absent recording retains audible synthesized fallback");
        Check(fallback.Where((_, i) => i % 2 == 1).All(x => x == 0), $"{category} fallback preserves direction");
    }
}
else
{
    // Excerpts, except the D2R discovery and room-entry cues, which play whole (frames of the shipped WAVs).
    double[] durations = { .30, .30, .24, .24, .30, .30, 136768 / 48000.0, .35, 44480 / 48000.0 };
    var signals = new List<float[]>();
    foreach (var loopOnly in Enum.GetValues<NaturalCueKind>().Where(k => k >= NaturalCueKind.Potion))
        Check(!NaturalCueBank.TryBuild(loopOnly, 0, 1, 1, out _), $"{loopOnly} has no excerpt; it only loops");
    // The short fallback cues are cut from the loop recordings; only the one-shots ship at the root.
    var assembly = typeof(NaturalCueBank).Assembly;
    Check(assembly.GetManifestResourceNames().Where(n => !n.Contains(".Loops.") && n.EndsWith(".wav")).OrderBy(n => n)
        .SequenceEqual(new[] { "LeAccess.Sounds.discovery.wav", "LeAccess.Sounds.inside_unexplored_room.wav",
            "LeAccess.Sounds.low_life_start.wav" }),
        "No root recording duplicates a loop");
    foreach (var (kind, name, seconds) in new[] { (NaturalCueKind.Gold, "gold", NaturalCueBank.GoldSeconds),
        (NaturalCueKind.Equipment, "equipment", NaturalCueBank.EquipmentSeconds),
        (NaturalCueKind.Chest, "chest", NaturalCueBank.ContainerSeconds),
        (NaturalCueKind.Portal, "portal", NaturalCueBank.TransitionSeconds),
        (NaturalCueKind.UnexploredRoom, "unexplored_room", NaturalCueBank.RouteSeconds) })
    {
        using var stream = assembly.GetManifestResourceStream($"LeAccess.Sounds.Loops.{name}.wav");
        var loop = PcmWaveReader.ReadMono48(stream, NaturalCueBank.MaximumLoopFrames);
        int frames = (int)Math.Round(seconds * NaturalCueBank.SampleRate);
        Check(loop.Length > frames, $"The {name} loop is longer than its fallback cue");
        float peak = loop.Take(frames).Max(Math.Abs);
        var cue = NaturalCueBank.Build(kind, 0, 1, 1);
        int middle = frames / 2;
        float expected = loop[middle] * 0.24f / peak * (float)new CuePlacement(0, 1).Left;
        Check(Math.Abs(cue[middle * 2] - expected) < 1e-5, $"The {kind} fallback is the first {seconds} s of the {name} loop");
    }
    foreach (NaturalCueKind kind in Enum.GetValues<NaturalCueKind>().Where(k => k < NaturalCueKind.Potion))
    {
        Check(NaturalCueBank.TryBuild(kind, 0, 1, 1, out var center), $"{kind} loads from the embedded resource");
        signals.Add(center);
        Check(NaturalCueBank.Duration(kind) == durations[(int)kind] && center.Length == durations[(int)kind] * 96000,
            $"{kind} uses its exact bounded 48 kHz stereo duration");
        Check(center.All(x => float.IsFinite(x) && Math.Abs(x) <= .24) && center.Any(x => Math.Abs(x) > .02),
            $"{kind} is audible, bounded and finite");
        Check(center[0] == 0 && center[1] == 0 && center[^1] == 0 && center[^2] == 0,
            $"{kind} fades both edges to silence");
        Check(center.Where((_, i) => i % 2 == 0).SequenceEqual(center.Where((_, i) => i % 2 == 1)),
            $"{kind} centers equally between both speakers");
        var left = NaturalCueBank.Build(kind, -1, 1, 1);
        var right = NaturalCueBank.Build(kind, 1, 1, 1);
        Check(left.Where((_, i) => i % 2 == 1).All(x => x == 0) && right.Where((_, i) => i % 2 == 0).All(x => x == 0),
            $"{kind} respects full stereo direction");
        Check(left.Where((_, i) => i % 2 == 0).SequenceEqual(right.Where((_, i) => i % 2 == 1)),
            $"{kind} mirrors left and right without changing its timbre");
        double energy = center.Sum(x => (double)x * x), sideEnergy = left.Sum(x => (double)x * x);
        Check(Math.Abs(energy / sideEnergy - 1) < .000001, $"{kind} preserves energy across pan positions");
        var rear = NaturalCueBank.Build(kind, 0, -1, 1);
        Check(rear.Length == center.Length && !rear.SequenceEqual(center), $"{kind} rear pitch changes without changing reservation length");
        Check(NaturalCueBank.Build(kind, 0, 1, .5f).SequenceEqual(center.Select(x => x * .5f)), $"{kind} gain follows user volume linearly");
        Check(NaturalCueBank.Build(kind, float.NaN, float.NaN, float.NaN).All(x => x == 0), $"{kind} invalid gain is silent");
        Check(NaturalCueBank.Build(kind, float.NaN, float.NaN, 1).SequenceEqual(center), $"{kind} invalid direction safely centers in front");
        Check(NaturalCueBank.Build(kind, -2, -2, CuePlacement.MaxGain + 1).SequenceEqual(NaturalCueBank.Build(kind, -1, -1, CuePlacement.MaxGain)), $"{kind} clamps finite inputs");
        center[center.Length / 2] = 100;
        Check(NaturalCueBank.Build(kind, 0, 1, 1).All(x => Math.Abs(x) <= .24), $"{kind} callers cannot mutate the cached recording");
    }
    Check(NaturalCueBank.LastFailure == null, "All shipped assets decoded without fallback");
    Check(signals.Select(x => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
        x.SelectMany(BitConverter.GetBytes).ToArray()))).Distinct().Count() == signals.Count,
        "Every semantic recording remains distinct");
}
Check(!NaturalCueBank.TryBuild((NaturalCueKind)(-1), 0, 1, 1, out _) &&
    NaturalCueBank.Build((NaturalCueKind)99, 0, 1, 1).Length == 0, "Invalid cue identifiers are safe");

var pcm = Wave(new short[] { short.MinValue, -1000, 0, 1000, short.MaxValue });
var decoded = PcmWaveReader.ReadMono48(new MemoryStream(pcm));
Check(decoded.SequenceEqual(new[] { -1f, -1000 / 32768f, 0, 1000 / 32768f, short.MaxValue / 32768f }),
    "PCM16 decoding preserves signed endpoint values");
var padded = Wave(new short[] { 10, 20 }, writer =>
{ writer.Write(Encoding.ASCII.GetBytes("JUNK")); writer.Write(3); writer.Write(new byte[] { 1, 2, 3, 0 }); });
Check(PcmWaveReader.ReadMono48(new MemoryStream(padded)).Length == 2, "Odd unknown chunks skip their RIFF padding correctly");
Reject(Array.Empty<byte>(), "Empty assets fail safely");
Reject(pcm.Take(20).ToArray(), "Truncated RIFF data is rejected");
var bad = pcm.ToArray(); bad[0] = 0; Reject(bad, "Non-RIFF asset is rejected");
bad = pcm.ToArray(); bad[20] = 3; Reject(bad, "Float/compressed formats cannot masquerade as PCM16");
bad = pcm.ToArray(); bad[22] = 2; Reject(bad, "Stereo sources cannot enter the mono asset bank");
bad = pcm.ToArray(); bad[24] = 0; Reject(bad, "Wrong sample rate is rejected");
bad = pcm.ToArray(); bad[28] = 1; Reject(bad, "Inconsistent byte rate is rejected");
bad = pcm.ToArray(); bad[32] = 4; Reject(bad, "Inconsistent frame size is rejected");
bad = pcm.ToArray(); bad[34] = 8; Reject(bad, "Wrong PCM bit depth is rejected");
bad = pcm.ToArray(); bad[40] = 9; Reject(bad, "Incomplete PCM frames are rejected");
Reject(Wave(Array.Empty<short>()), "Zero-length sample data is rejected");
Reject(Wave(new short[48001]), "Oversized cues cannot allocate an unbounded playback buffer");
// Sound glossary: unique letter-only ids, text keys in the English catalog, every world kind present with its levels.
{
    using var catalog = typeof(SoundGlossary).Assembly.GetManifestResourceStream("LeAccess.Translations.en.json")!;
    var keys = System.Text.Json.JsonDocument.Parse(catalog).RootElement.EnumerateObject().Select(p => p.Name).ToHashSet();
    var entries = SoundGlossary.Entries;
    Check(entries.Count > 0 && entries.Select(e => e.Id).Distinct().Count() == entries.Count, "Glossary ids are unique");
    Check(entries.All(e => e.Id.Length > 0 && e.Id.All(char.IsLetter) && char.IsUpper(e.Id[0])), "Glossary ids are PascalCase letters");
    Check(entries.All(e => keys.Contains("sound." + e.Id) && keys.Contains("sound." + e.Id + ".details")), "Every sound has a name and details text");
    Check(keys.Where(k => k.StartsWith("sound.") && !k.EndsWith(".details")).All(k => entries.Any(e => "sound." + e.Id == k)),
        "No sound text is left without a glossary entry");
    Check(entries.All(e => !string.IsNullOrEmpty(e.VolumeSection) && !string.IsNullOrEmpty(e.VolumeKey)), "Every sound names its volume entry");
    foreach (var world in SoundLevels.WorldKinds)
    {
        var entry = entries.SingleOrDefault(e => e.Id == world.Key);
        Check(entry.Id != null && entry.VolumeSection == SoundLevels.VolumeSection && entry.VolumeKey == world.Key,
            $"{world.Kind} has a glossary entry with its own volume key");
        Check(world.HasRange == (entry.RangeKey != null) && (!world.HasRange || entry.RangeSection == SoundLevels.RangeSection && entry.RangeKey == world.Key),
            $"{world.Kind} lists a range only when it has one");
    }
    Check(Enum.GetValues<NaturalCueKind>().All(k => SoundLevels.WorldKinds.Any(w => w.Kind == k)), "Every natural cue kind has a config key");
    Check(SoundLevels.WorldKinds.Select(w => w.Kind).Distinct().Count() == SoundLevels.WorldKinds.Length &&
        SoundLevels.WorldKinds.Select(w => w.Key).Distinct().Count() == SoundLevels.WorldKinds.Length, "Kinds and keys are unique");
    Check(SoundLevels.EnemyKeys.All(k => entries.Any(e => e.VolumeSection == SoundLevels.EnemySection && e.VolumeKey == k)),
        "Each enemy type has a glossary entry");
    Check(entries.Where(e => e.RangeKey == null).All(e => e.VolumeSection != SoundLevels.RangeSection), "Only world kinds use range entries");
}

// Preview buffers: centred, faded at both ends, bounded to the requested length.
{
    var clip = Enumerable.Range(0, 480).Select(i => i % 2 == 0 ? 0.8f : -0.8f).ToArray();
    var loop = PreviewClips.Loop(clip, 0.5f, 1);
    Check(loop.Length == PreviewClips.SampleRate * PreviewClips.Channels, "A loop preview lasts the requested time (shorter loops repeat)");
    Check(Enumerable.Range(0, loop.Length / 2).All(i => loop[2 * i] == loop[2 * i + 1]), "A preview is centred");
    Check(Math.Abs(loop[0]) < 0.01f && Math.Abs(loop[^1]) < 0.01f && loop.Max() <= 0.8f * 0.5f + 1e-4f, "A preview fades in and out and keeps its gain");
    Check(PreviewClips.Loop(Array.Empty<float>(), 1, 1).Length == 0 && PreviewClips.Loop(clip, 0, 1).Length == 0 &&
        PreviewClips.Loop(clip, float.NaN, 1).Length == 0, "A silent or invalid preview is empty");
    var tone = PreviewClips.Tone(500, 0.05f, 0.5);
    Check(tone.Length == PreviewClips.SampleRate * PreviewClips.Channels / 2 && tone.Max() <= 0.05f && tone.Max() > 0.02f, "A tone preview holds its amplitude");
    Check(PreviewClips.Tone(0, 0.05f, 1).Length == 0 && PreviewClips.Tone(500, 0, 1).Length == 0, "An invalid tone is empty");
}

Console.WriteLine($"Natural audio tests passed: {checks} checks; no audio device opened.");
