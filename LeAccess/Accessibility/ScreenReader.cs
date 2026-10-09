using System;
using System.IO;
using System.Runtime.InteropServices;
using DavyKager;

namespace LeAccess.Accessibility;

/// <summary>Centralized output through the established Tolk transport.</summary>
public static class ScreenReader
{
    // A stopped/restarting reader is checked at most once per second on the game thread.
    private const double ReaderProbeIntervalSeconds = 1;
    private static bool _initialized;
    private static bool _available;
    private static bool _connectionRequested;
    private static double _nextProbeTime;
    private static string _lastDetectedName;
    private static string _lastFailure;
    private static int _lastSpeechFrame = -1;
    private static IntPtr _nvdaClient;

    internal static bool SpokeThisFrame => _lastSpeechFrame == UnityEngine.Time.frameCount;

    /// <summary>Accepted outputs so far, so a reader can tell whether anything else was said since its own speech.</summary>
    internal static int SpeechCount { get; private set; }

    public static string DetectedScreenReader { get; private set; } = "None";

    /// <summary>
    /// Initialize the screen reader connection.
    /// </summary>
    public static bool Initialize()
    {
        if (!_connectionRequested) _nextProbeTime = 0;
        _connectionRequested = true;
        return RefreshAvailability();
    }

    private static bool RefreshAvailability()
    {
        if (!_connectionRequested) return false;
        double now = UnityEngine.Time.unscaledTimeAsDouble;
        if (now < _nextProbeTime) return _available;
        _nextProbeTime = now + ReaderProbeIntervalSeconds;

        try
        {
            if (!_initialized)
            {
                // Tolk loads this client by basename; Unity's working directory is
                // the game root, while our bundled DLL belongs beside this plugin.
                string client = Path.Combine(Path.GetDirectoryName(typeof(ScreenReader).Assembly.Location),
                    "nvdaControllerClient64.dll");
                if (_nvdaClient == IntPtr.Zero && File.Exists(client))
                    _nvdaClient = NativeLibrary.Load(client);
                Tolk.Load();
                _initialized = Tolk.IsLoaded();
                if (!_initialized) { FailConnection("Tolk.Load did not initialize the library"); return false; }
            }
            string name = Tolk.DetectScreenReader();
            _available = !string.IsNullOrEmpty(name);
            DetectedScreenReader = _available ? name : "None";
            if (_lastDetectedName != DetectedScreenReader)
            {
                _lastDetectedName = DetectedScreenReader;
                if (_available) Plugin.Log.LogInfo($"Tolk available. Detected: {name}");
                else Plugin.Log.LogWarning("Tolk loaded but no screen reader detected");
            }
        }
        catch (Exception ex)
        {
            FailConnection($"Tolk initialization/detection failed: {ex.GetType().Name}: {ex.Message}");
        }
        return _available;
    }

    /// <summary>
    /// Speak text through the screen reader.
    /// </summary>
    /// <param name="text">Text to speak</param>
    /// <param name="interrupt">If true, interrupts current speech</param>
    public static void Speak(string text, bool interrupt = true)
        => TrySpeak(text, interrupt);

    /// <summary>
    /// Return whether Tolk accepted speech or braille output, not whether playback completed.
    /// Failed output is not queued; callers retain responsibility for current content ownership.
    /// </summary>
    public static bool TrySpeak(string text, bool interrupt = true)
    {
        if (string.IsNullOrEmpty(text) || !IsAvailable()) return false;

        try
        {
            if (Tolk.Output(text, interrupt))
            {
                _lastSpeechFrame = UnityEngine.Time.frameCount;
                SpeechCount++;
                _lastFailure = null;
                return true;
            }
            else FailConnection("Tolk.Output returned false");
        }
        catch (Exception ex)
        {
            FailConnection($"Tolk.Output failed: {ex.GetType().Name}: {ex.Message}");
        }
        return false;
    }

    /// <summary>
    /// Speak text without interrupting current speech.
    /// </summary>
    public static void SpeakQueued(string text)
    {
        Speak(text, interrupt: false);
    }

    /// <summary>
    /// Stop current speech.
    /// </summary>
    public static void Silence()
    {
        if (!IsAvailable()) return;

        try
        {
            Tolk.Silence();
        }
        catch (Exception ex)
        {
            FailConnection($"Tolk.Silence failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Send text to braille display.
    /// </summary>
    public static void Braille(string text)
    {
        if (string.IsNullOrEmpty(text) || !IsAvailable()) return;

        try
        {
            Tolk.Braille(text);
        }
        catch (Exception ex)
        {
            FailConnection($"Tolk.Braille failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if a screen reader is available.
    /// </summary>
    public static bool IsAvailable() => RefreshAvailability();

    /// <summary>
    /// Check if the screen reader is currently speaking.
    /// </summary>
    public static bool IsSpeaking()
    {
        if (!IsAvailable()) return false;

        try
        {
            return Tolk.IsSpeaking();
        }
        catch (Exception ex)
        {
            FailConnection($"Tolk.IsSpeaking failed: {ex.GetType().Name}: {ex.Message}");
            return false;
        }
    }

    private static void FailConnection(string failure)
    {
        _available = false;
        DetectedScreenReader = "None";
        _nextProbeTime = UnityEngine.Time.unscaledTimeAsDouble + ReaderProbeIntervalSeconds;
        if (_lastFailure == failure) return;
        _lastFailure = failure;
        Plugin.Log.LogError(failure);
    }

    /// <summary>
    /// Shutdown and cleanup.
    /// </summary>
    public static void Shutdown()
    {
        try
        {
            if (_initialized) Tolk.Unload();
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Tolk.Unload failed: {ex.Message}");
        }

        _initialized = false;
        _available = false;
        _connectionRequested = false;
        _nextProbeTime = 0;
        _lastSpeechFrame = -1;
        _lastDetectedName = _lastFailure = null;
        DetectedScreenReader = "None";
        if (_nvdaClient != IntPtr.Zero)
        {
            try { NativeLibrary.Free(_nvdaClient); }
            catch (Exception ex) { Plugin.Log.LogError($"NVDA client unload failed: {ex.Message}"); }
            _nvdaClient = IntPtr.Zero;
        }
    }
}
