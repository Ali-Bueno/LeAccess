namespace LeAccess.Accessibility.Audio;

/// <summary>One loop that should sound now: identity, stereo gains already spatialized, playback rate.</summary>
internal readonly record struct LoopVoice(int Id, NaturalCueKind Kind, float Left, float Right, float Pitch);
