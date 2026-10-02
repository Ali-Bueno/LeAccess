using System;
using System.IO;
using System.Runtime.InteropServices;
using DavyKager;

namespace LeAccess.Accessibility;

/// <summary>
/// Wrapper for Tolk screen reader library.
/// Supports NVDA, JAWS, Window-Eyes, and other screen readers.
/// </summary>
public static class ScreenReader
{
    private static bool _initialized;
    private static bool _available;
    private static int _lastSpeechFrame = -1;
    private static IntPtr _nvdaClient;

    internal static bool SpokeThisFrame => _lastSpeechFrame == UnityEngine.Time.frameCount;

    public static string DetectedScreenReader { get; private set; } = "None";

    /// <summary>
    /// Initialize the screen reader connection.
    /// </summary>
    public static bool Initialize()
    {
        if (_initialized) return _available;

        try
        {
            // Tolk loads this client by basename; Unity's working directory is
            // the game root, while our bundled DLL belongs beside this plugin.
            string client = Path.Combine(Path.GetDirectoryName(typeof(ScreenReader).Assembly.Location),
                "nvdaControllerClient64.dll");
            if (_nvdaClient == IntPtr.Zero && File.Exists(client))
                _nvdaClient = NativeLibrary.Load(client);
            Tolk.Load();
            _initialized = true;

            var name = Tolk.DetectScreenReader();
            if (!string.IsNullOrEmpty(name))
            {
                DetectedScreenReader = name;
                _available = true;
                Plugin.Log.LogInfo($"Tolk initialized. Detected: {name}");
            }
            else
            {
                Plugin.Log.LogWarning("Tolk loaded but no screen reader detected");
                _available = false;
            }
        }
        catch (DllNotFoundException ex)
        {
            Plugin.Log.LogError($"Tolk DLL not found: {ex.Message}");
            _available = false;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Failed to initialize Tolk: {ex.Message}");
            _available = false;
        }

        return _available;
    }

    /// <summary>
    /// Speak text through the screen reader.
    /// </summary>
    /// <param name="text">Text to speak</param>
    /// <param name="interrupt">If true, interrupts current speech</param>
    public static void Speak(string text, bool interrupt = true)
    {
        if (!_available || string.IsNullOrEmpty(text)) return;

        try
        {
            Tolk.Output(text, interrupt);
            _lastSpeechFrame = UnityEngine.Time.frameCount;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Tolk.Output failed: {ex.Message}");
        }
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
        if (!_available) return;

        try
        {
            Tolk.Silence();
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Tolk.Silence failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Send text to braille display.
    /// </summary>
    public static void Braille(string text)
    {
        if (!_available || string.IsNullOrEmpty(text)) return;

        try
        {
            Tolk.Braille(text);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Tolk.Braille failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if a screen reader is available.
    /// </summary>
    public static bool IsAvailable() => _available;

    /// <summary>
    /// Check if the screen reader is currently speaking.
    /// </summary>
    public static bool IsSpeaking()
    {
        if (!_available) return false;

        try
        {
            return Tolk.IsSpeaking();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Shutdown and cleanup.
    /// </summary>
    public static void Shutdown()
    {
        if (!_initialized && _nvdaClient == IntPtr.Zero) return;

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
        if (_nvdaClient != IntPtr.Zero)
        {
            NativeLibrary.Free(_nvdaClient);
            _nvdaClient = IntPtr.Zero;
        }
    }
}
