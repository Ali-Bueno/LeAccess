using LeAccess.Accessibility.Audio;

int checks = 0;
void Check(bool result, string message)
{ checks++; if (!result) throw new InvalidOperationException(message); }
const int Rate = WorldLoopBank.SampleRate;

// Every supplied loop decodes whole, peak-normalized with D2R's level trims.
foreach (var kind in new[] { NaturalCueKind.Gold, NaturalCueKind.Equipment, NaturalCueKind.Chest,
    NaturalCueKind.Stash, NaturalCueKind.Portal, NaturalCueKind.ZoneTransition, NaturalCueKind.Potion, NaturalCueKind.Tome })
{
    var clip = WorldLoopBank.Get(kind);
    Check(clip.Length > 0 && WorldLoopBank.Has(kind), $"{kind} loop loads");
    float peak = clip.Max(Math.Abs);
    float expected = 0.9f * MathF.Pow(10, (kind == NaturalCueKind.Chest ? 4 : kind == NaturalCueKind.Stash ? -4 : 0) / 20f);
    Check(Math.Abs(peak - expected) < 1e-3, $"{kind} loop peaks at its trimmed level");
}
Check(WorldLoopBank.Get(NaturalCueKind.Portal).Length > 9 * Rate, "The portal loop is the whole recording, not a 300 ms excerpt");
Check(WorldLoopBank.Get(NaturalCueKind.Stash).Length == Rate, "The short stash recording is padded to repeat once a second");
Check(!WorldLoopBank.Has(NaturalCueKind.Discovery) && WorldLoopBank.Get((NaturalCueKind)99).Length == 0,
    "Kinds without a loop and invalid kinds are empty");
Check(WorldLoopBank.LastFailure == null, "No loop failed to load");

// Mixer with a known ramp clip.
float[] ramp = Enumerable.Range(0, 100).Select(i => i / 100f).ToArray();
var clips = new Dictionary<NaturalCueKind, float[]> { [NaturalCueKind.Gold] = ramp, [NaturalCueKind.Portal] = ramp };
float[] Clip(NaturalCueKind kind) => clips.TryGetValue(kind, out var value) ? value : Array.Empty<float>();
float[] Read(WorldLoopMixer mixer, int frames)
{ var buffer = new float[frames * 2]; Check(mixer.Read(buffer, 0, buffer.Length) == buffer.Length, "The stream never ends"); return buffer; }

var mixer = new WorldLoopMixer(Clip);
Check(mixer.WaveFormat.SampleRate == Rate && mixer.WaveFormat.Channels == 2, "Stereo float at the loop rate");
Check(Read(mixer, 64).All(x => x == 0), "No voices mix silence");
mixer.SetVoices(new[] { new LoopVoice(1, NaturalCueKind.Gold, 0.5f, 0.25f, 1) });
var first = Read(mixer, Rate / 2);
Check(Math.Abs(first[0]) < 1e-6 && Math.Abs(first[2]) < 0.01, "A new voice fades in instead of clicking");
int settled = (Rate / 2 - 150) * 2;
int frameAt = Rate / 2 - 150;
Check(Math.Abs(first[settled] - ramp[frameAt % 100] * 0.5f) < 1e-3 && Math.Abs(first[settled + 1] - ramp[frameAt % 100] * 0.25f) < 1e-3,
    "A settled voice plays its clip at its stereo gains");
var next = Read(mixer, 10);
int continued = Rate / 2;
Check(Math.Abs(next[0] - ramp[continued % 100] * 0.5f) < 1e-3, "Playback position continues across reads (seamless loop)");
Check(mixer.ActiveVoices == 1, "One voice");

// Same id again keeps the voice; changing gains glides.
mixer.SetVoices(new[] { new LoopVoice(1, NaturalCueKind.Gold, 0.1f, 0.1f, 1) });
Check(mixer.ActiveVoices == 1, "An updated target reuses its voice and loop position");
var glide = Read(mixer, 4);
Check(glide[0] > ramp[(continued + 10) % 100] * 0.1f, "Gain changes glide rather than jump");

// Absent voices fade out and are released.
mixer.SetVoices(Array.Empty<LoopVoice>());
Read(mixer, Rate / 2);
Check(mixer.ActiveVoices == 0, "A removed voice fades out and is released");
mixer.SetVoices(new[] { new LoopVoice(1, NaturalCueKind.Gold, 0.5f, 0.5f, 1) });
mixer.Silence();
Read(mixer, Rate / 2);
Check(mixer.ActiveVoices == 0, "Silence fades every voice out");

// Unknown clips, invalid values and clamping.
mixer.SetVoices(new[] { new LoopVoice(2, NaturalCueKind.Chest, 0.5f, 0.5f, 1) });
Check(mixer.ActiveVoices == 0, "A kind without a loaded clip creates no voice");
mixer.SetVoices(new[] { new LoopVoice(3, NaturalCueKind.Gold, float.NaN, float.PositiveInfinity, float.NaN) });
Check(Read(mixer, Rate / 4).All(x => x == 0), "Non-finite gains are silent");
mixer.SetVoices(Enumerable.Range(10, 6).Select(i => new LoopVoice(i, NaturalCueKind.Gold, 1, 1, 1)).ToList());
var loud = Read(mixer, Rate / 4);
Check(loud.All(x => x >= -1 && x <= 1) && loud.Max() == 1, "The summed mix is clamped to full scale");
mixer.Silence(); Read(mixer, Rate / 2);

// Pitch: a slower rate advances half as far.
var slow = new WorldLoopMixer(Clip);
slow.SetVoices(new[] { new LoopVoice(4, NaturalCueKind.Portal, 1, 1, 0.5f) });
var pitched = Read(slow, Rate);
int at = Rate - 1100;
Check(Math.Abs(pitched[at * 2] - ramp[(int)(at * 0.5) % 100]) < 0.02, "Rear pitch slows playback through the loop");
Check(slow.FramesMixed == Rate, "Mixed frames are counted for diagnostics");

Console.WriteLine($"World loop tests passed: {checks} checks.");
