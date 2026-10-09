using LeAccess.Accessibility.Npcs;
using UnityEngine;

// Notes, lore records and paintings share the NPC interaction but have no character model.
internal static class ReadableTests
{
    internal static int Run()
    {
        int checks = 0;
        void Check(bool value, string name) { checks++; if (!value) throw new Exception(name); }
        var note = new GameObject();
        var listener = note.Add(new WorldObjectClickListener());
        listener._NPCinteraction = note.Add(new RelayNPCInteraction());
        Check(WorldServiceReader.IsNpc(listener) && WorldServiceReader.IsReadable(listener) &&
            !WorldServiceReader.IsCharacterNpc(listener) && !NpcWorldProbe.IsNpc(listener),
            "A conversation object without a model is readable and gets no NPC cue");

        var stash = new GameObject();
        var stashListener = stash.Add(new WorldObjectClickListener());
        stashListener._NPCinteraction = stash.Add(new RelayNPCInteraction());
        stash.Add(new OpenStash());
        Check(WorldServiceReader.ReadServiceKey(stashListener) == "service.stash" && !WorldServiceReader.IsReadable(stashListener),
            "A service without a model keeps its service identity");

        note.Child().Add(new SkinnedMeshRenderer());
        Check(!WorldServiceReader.IsReadable(listener) && NpcWorldProbe.IsNpc(listener),
            "A model that loads after its listener makes it a character");
        return checks;
    }
}
