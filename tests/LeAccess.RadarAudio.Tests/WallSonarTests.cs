using System.Reflection;
using LeAccess.Accessibility.Audio;
using LeAccess.Accessibility.Navigation;

internal static class WallSonarTests
{
    internal static int Run()
    {
        int checks = 0;
        void Check(bool condition,string message)
        { checks++; if(!condition)throw new InvalidOperationException(message); }
        void SetField(string name,object value) => typeof(WallSonar).GetField(name,BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,value);
        WallChannel[] Prepare()
        {
            WallSonar.Cleanup();
            PlayerFinder.Throw=false; PlayerFinder.Player=new();
            GameplayInputState.Owned=true; NavigationAudioState.IsSuspended=false;
            LeAccess.Plugin.ContinuousSonarEnabled=true;
            var channels=Enumerable.Range(0,4).Select(_=>new WallChannel { Generator=new SmoothSineGenerator(440,0.05f) }).ToArray();
            SetField("_channels",channels); SetField("_initialized",true);
            SetField("_targetVolumes",Enumerable.Repeat(0.05f,4).ToArray());
            SetField("_currentVolumes",Enumerable.Repeat(0.05f,4).ToArray());
            return channels;
        }
        var active=Prepare();
        GameplayInputState.Owned=false; WallSonar.Update();
        Check(active.All(channel=>channel.Generator.Volume==0),"An unpaused inventory/menu relinquishing gameplay input immediately mutes continuous sonar");
        GameplayInputState.Owned=true; WallSonar.Update();
        Check(active.Any(channel=>channel.Generator.Volume>0),"Continuous sonar resumes fresh probes after gameplay regains input");
        active=Prepare(); PlayerFinder.Throw=true; WallSonar.Update();
        Check(active.All(channel=>channel.Generator.Volume==0),"A failed native player query cancels old looping wall tones");
        PlayerFinder.Throw=false; WallSonar.Update();
        Check(active.Any(channel=>channel.Generator.Volume>0),"Continuous sonar recovers after a temporary native failure");
        WallSonar.Cleanup();
        return checks;
    }
}
