using System;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using LeAccess.Accessibility.Navigation;
using UnityEngine;

namespace LeAccess.Accessibility.Audio;

/// <summary>
/// Smooth sine wave generator with sample-level volume interpolation.
/// Prevents audio clicks/pops when values change.
/// </summary>
internal class SmoothSineGenerator : ISampleProvider
{
    private readonly WaveFormat _waveFormat;
    private double _phase;
    private double _frequency;
    private float _targetVolume;
    private float _currentVolume;

    private const float VolumeSmoothing = 0.02f;

    public WaveFormat WaveFormat => _waveFormat;

    public float Volume
    {
        get => _targetVolume;
        set => _targetVolume = Math.Max(0, Math.Min(1, value));
    }

    public SmoothSineGenerator(double frequency, float volume = 0f, int sampleRate = 44100)
    {
        _waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
        _frequency = frequency;
        _targetVolume = volume;
        _currentVolume = volume;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        double sampleRate = _waveFormat.SampleRate;

        for (int i = 0; i < count; i++)
        {
            _currentVolume += (_targetVolume - _currentVolume) * VolumeSmoothing;

            double triangle = 2.0 * Math.Abs(2.0 * _phase - 1.0) - 1.0;
            double sine = Math.Sin(2 * Math.PI * _phase);
            double mixed = triangle * 0.7 + sine * 0.3;

            buffer[offset + i] = (float)(_currentVolume * mixed);

            _phase += _frequency / sampleRate;
            if (_phase >= 1.0)
                _phase -= 1.0;
        }

        return count;
    }
}

/// <summary>
/// Directional audio channel: generator + panning.
/// </summary>
internal class WallChannel
{
    public SmoothSineGenerator Generator;
    public PanningSampleProvider Panning;
}

/// <summary>
/// Wall proximity audio system using NavMesh boundary detection.
/// Uses the game's NavMeshHelper.getFurthestPointTowardsPoint() to detect
/// how far the player can walk in each direction before hitting a boundary.
/// This catches all real movement obstacles (walls, cliffs, barriers)
/// that Physics.Raycast would miss.
///
/// - Forward: 500 Hz (center)
/// - Back: 180 Hz (center)
/// - Left: 300 Hz (panned left)
/// - Right: 300 Hz (panned right)
/// </summary>
public static class WallSonar
{
    private const float MaxRange = 12f;
    private const float MinVolumeDistance = 0.5f;
    private const float BaseVolume = 0.05f;
    private const float ProbeInterval = 0.1f;

    private const double FreqForward = 500;
    private const double FreqBack = 180;
    private const double FreqSides = 300;

    private static WaveOutEvent _waveOut;
    private static MixingSampleProvider _mixer;
    private static WallChannel[] _channels;
    private static bool _initialized;

    private static float[] _targetVolumes = new float[4];
    private static float[] _currentVolumes = new float[4];

    private static float _timeSinceLastProbe;
    private static int _debugCounter;

    private enum Dir { Forward = 0, Back = 1, Left = 2, Right = 3 }

    public static void Initialize()
    {
        try
        {
            var format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            _mixer = new MixingSampleProvider(format) { ReadFully = true };

            _channels = new WallChannel[4];
            CreateChannel(Dir.Forward, FreqForward, 0f);
            CreateChannel(Dir.Back, FreqBack, 0f);
            CreateChannel(Dir.Left, FreqSides, -1f);
            CreateChannel(Dir.Right, FreqSides, 1f);

            _waveOut = new WaveOutEvent { DesiredLatency = 100 };
            _waveOut.Init(_mixer);
            _waveOut.Play();

            _initialized = true;
            Plugin.Log.LogInfo($"WallSonar initialized (NavMesh mode). WaveOut: {_waveOut.PlaybackState}");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"WallSonar initialization failed: {ex}");
            _initialized = false;
        }
    }

    private static void CreateChannel(Dir direction, double frequency, float pan)
    {
        var generator = new SmoothSineGenerator(frequency, 0f);
        var panning = new PanningSampleProvider(generator) { Pan = pan };
        _mixer.AddMixerInput(panning);

        _channels[(int)direction] = new WallChannel
        {
            Generator = generator,
            Panning = panning
        };
    }

    public static void Update()
    {
        if (!_initialized) return;

        try
        {
            _debugCounter++;

            var playerGO = PlayerFinder.getPlayer();
            if (playerGO == null)
            {
                SilenceAll();
                return;
            }

            var playerTransform = playerGO.transform;
            if (playerTransform == null)
            {
                SilenceAll();
                return;
            }

            // Mute when UI is open
            if (!Plugin.ContinuousSonarEnabled || NavigationAudioState.IsSuspended)
            {
                SilenceAll();
                return;
            }

            // Probe walls at fixed interval (NavMesh queries are heavier than raycasts)
            _timeSinceLastProbe += Time.deltaTime;
            if (_timeSinceLastProbe >= ProbeInterval)
            {
                _timeSinceLastProbe = 0f;
                ProbeWalls(playerTransform);
            }

            // Smooth volume transitions every frame
            SmoothVolumes();
        }
        catch (Exception ex)
        {
            if (_debugCounter % 300 == 0)
                Plugin.Log.LogWarning($"WallSonar error: {ex.Message}");
        }
    }

    private static void ProbeWalls(Transform playerTransform)
    {
        Vector3 pos = playerTransform.position;

        // Use camera for forward direction (better in 3rd person)
        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;

        var cam = Camera.main;
        if (cam != null)
        {
            forward = cam.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.001f)
            {
                forward = forward.normalized;
                right = new Vector3(forward.z, 0f, -forward.x);
            }
        }

        ProbeDirection((int)Dir.Forward, pos, forward);
        ProbeDirection((int)Dir.Back, pos, -forward);
        ProbeDirection((int)Dir.Left, pos, -right);
        ProbeDirection((int)Dir.Right, pos, right);
    }

    /// <summary>
    /// Probe a single direction using dual NavMesh detection.
    /// Step 1: NavMesh.Raycast (straight line) to find boundary distance.
    /// Step 2: NavMesh path-following to find how far the path system can reach.
    /// If the path extends significantly past the raycast boundary,
    /// the boundary is passable (bridge transition, ramp, etc.) → no wall sound.
    /// If the path also stops near the boundary → real wall → play sound.
    /// </summary>
    private static void ProbeDirection(int dirIndex, Vector3 origin, Vector3 direction)
    {
        try
        {
            Vector3 target = origin + direction * MaxRange;

            // Step 1: Straight-line raycast on NavMesh - stops at first boundary
            Vector3 rayPoint = NavMeshHelper.getFurthestPointTowardsPointWithRayCast(origin, target);
            float rayDist = HorizontalDistance(origin, rayPoint);

            if (rayDist >= MaxRange - 0.3f)
            {
                // No boundary in range
                _targetVolumes[dirIndex] = 0f;
                return;
            }

            // Step 2: Path-following probe - can navigate around obstacles
            Vector3 pathPoint = NavMeshHelper.getFurthestPointTowardsPoint(origin, target);
            float pathDist = HorizontalDistance(origin, pathPoint);

            // If path extends significantly past the raycast boundary,
            // the boundary is navigable (bridge, ramp, transition)
            if (pathDist > rayDist + 2f)
            {
                _targetVolumes[dirIndex] = 0f;
                return;
            }

            // Real wall - both raycast and path agree on the boundary
            float normalizedDist = Mathf.Clamp01(
                (rayDist - MinVolumeDistance) / (MaxRange - MinVolumeDistance));
            float volumeMultiplier = 1f - (normalizedDist * normalizedDist);
            _targetVolumes[dirIndex] = BaseVolume * volumeMultiplier;

            if (_debugCounter % 50 == 0)
                Plugin.Log.LogDebug(
                    $"WallSonar {(Dir)dirIndex}: WALL at {rayDist:F1}m (path={pathDist:F1}m), vol={_targetVolumes[dirIndex]:F3}");
        }
        catch
        {
            _targetVolumes[dirIndex] = 0f;
        }
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        float dx = b.x - a.x;
        float dz = b.z - a.z;
        return (float)Math.Sqrt(dx * dx + dz * dz);
    }

    private static void SmoothVolumes()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < 4; i++)
        {
            _currentVolumes[i] = Mathf.Lerp(_currentVolumes[i], _targetVolumes[i], dt * 10f);
            if (_currentVolumes[i] < 0.005f)
                _currentVolumes[i] = 0f;
            _channels[i].Generator.Volume = _currentVolumes[i];
        }
    }

    private static void SilenceAll()
    {
        if (_channels == null) return;
        for (int i = 0; i < 4; i++)
        {
            _targetVolumes[i] = 0f;
            _currentVolumes[i] = 0f;
            if (_channels[i]?.Generator != null)
                _channels[i].Generator.Volume = 0f;
        }
    }

    public static void Cleanup()
    {
        try
        {
            _waveOut?.Stop();
            _waveOut?.Dispose();
            _waveOut = null;
            _mixer = null;
            _channels = null;
            _initialized = false;
            Plugin.Log.LogInfo("WallSonar cleaned up");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"WallSonar cleanup error: {ex.Message}");
        }
    }
}
