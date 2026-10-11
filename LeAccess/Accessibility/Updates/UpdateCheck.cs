using System;
using System.Net.Http;
using System.Threading.Tasks;
using BepInEx.Configuration;
using LeAccess.I18n;

namespace LeAccess.Accessibility.Updates;

/// <summary>Asks GitHub once per game start whether a newer mod release exists and says so (reference/updates.md).</summary>
internal static class UpdateCheck
{
    // GitHub's newest published release; GitHub itself skips drafts and pre-releases here.
    private const string LatestReleaseUrl = "https://api.github.com/repos/Ali-Bueno/LeAccess/releases/latest";
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);

    private readonly record struct Reply(string Tag, string Error);

    // Written by the worker, taken on the game thread: logging, text and speech stay on the game thread.
    private static Reply? _reply;
    private static readonly object Gate = new();

    internal static void Initialize(ConfigFile config)
    {
        var enabled = config.Bind("Updates", "CheckAtStartup", true,
            "When the game starts, ask GitHub whether a newer LeAccess release exists and say so. Takes effect at the next start.");
        if (enabled.Value) Task.Run(FetchAsync);
    }

    internal static void Update()
    {
        Reply reply;
        lock (Gate)
        {
            if (_reply is not { } taken) return;
            reply = taken;
            _reply = null;
        }
        string current = MyPluginInfo.PLUGIN_VERSION;
        if (reply.Error != null)
        {
            Plugin.Log.LogInfo($"[Updates] check failed: {reply.Error}");
            return;
        }
        if (!UpdateRelease.IsNewer(reply.Tag, current))
        {
            Plugin.Log.LogInfo($"[Updates] up to date: running {current}, latest release {reply.Tag ?? "unreadable"}");
            return;
        }
        Plugin.Log.LogInfo($"[Updates] newer release {reply.Tag}, running {current}");
        ScreenReader.SpeakQueued(ModText.Format("update.available", UpdateRelease.Display(reply.Tag), current));
    }

    private static async Task FetchAsync()
    {
        Reply reply;
        try
        {
            using var client = new HttpClient { Timeout = RequestTimeout };
            // GitHub's API rejects requests without a user agent.
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"LeAccess/{MyPluginInfo.PLUGIN_VERSION}");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            string json = await client.GetStringAsync(LatestReleaseUrl).ConfigureAwait(false);
            reply = new Reply(UpdateRelease.ReadTag(json), null);
        }
        catch (Exception ex)
        {
            reply = new Reply(null, ex.GetBaseException().Message);
        }
        lock (Gate) _reply = reply;
    }
}
