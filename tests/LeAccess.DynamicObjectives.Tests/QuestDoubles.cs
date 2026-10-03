namespace BepInEx.Configuration
{
    internal sealed class ConfigEntry<T> { internal T Value; }
    internal sealed class ConfigFile
    {
        internal ConfigEntry<bool> Enabled;
        internal ConfigEntry<double> Interval;
        internal ConfigEntry<T> Bind<T>(string section,string name,T value,string description)
        {
            var entry=new ConfigEntry<T>{Value=value};
            if(entry is ConfigEntry<bool> enabled) Enabled=enabled;
            if(entry is ConfigEntry<double> interval) Interval=interval;
            return entry;
        }
    }
}
internal enum QuestState { Active,Completed,Failed,Hidden }
internal sealed class Quest : UnityEngine.Object
{
    internal int id;
    internal string displayName="Campaign quest";
    internal bool playerVisible=true;
    internal StatefulQuestList Owner;
    internal StatefulQuestList getLocalStatefulQuestList()=>Owner;
}
internal sealed class QuestStep : UnityEngine.Object { internal Quest quest; internal int id; }
internal sealed class Objective : UnityEngine.Object { internal Quest quest; internal QuestStep questStep; internal int id; internal bool isIncremental; }
internal sealed class StatefulObjective : UnityEngine.Object
{
    internal StatefulQuest statefulQuest;
    internal Objective objective;
    internal QuestState state=QuestState.Active;
    internal int progress;
    internal string GetObjectiveActiveText()=>"Collect keys: "+progress+"/3";
}
internal sealed class StatefulQuest : UnityEngine.Object
{
    internal Quest quest;
    internal QuestStep currentStep;
    internal NativeList<StatefulObjective> objectives=new();
    internal QuestState Status=QuestState.Active;
    internal QuestState getState()=>Status;
}
internal sealed class StatefulQuestList : UnityEngine.Object { internal NativeList<StatefulQuest> trackedQuests=new(); }
internal sealed class StatefulQuestListHolder : UnityEngine.Object
{ internal bool initialised=true,hasBeenNullified; internal StatefulQuestList statefulQuestList=new(); }
internal sealed class Provider : UnityEngine.Object
{
    internal StatefulQuestListHolder Campaign=new(),Monolith=new(),Stash=new();
    internal StatefulQuestListHolder getPlayerQuestListHolder()=>Campaign;
    internal StatefulQuestListHolder getPlayerMonolithQuestListHolder()=>Monolith;
    internal StatefulQuestListHolder getPlayerStashQuestListHolder()=>Stash;
}
internal static class Localization { internal static QuestLocalization Quests=new(); }
internal sealed class QuestLocalization : UnityEngine.Object
{ internal string GetQuestName(int id,string fallback)=>fallback; }
namespace LeAccess.I18n
{
    internal static class ModText
    { internal static string Format(string key,params object[] values)=>key+": "+string.Join(",",values); }
}
