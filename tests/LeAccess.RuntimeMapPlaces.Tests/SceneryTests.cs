using LeAccess.Accessibility.MapSections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

internal static class SceneryTests
{
    internal static void Run(Action<bool, string> check)
    {
        foreach (string name in new[] { "Tent_01", "Tent_02 (10)", "Tent_03 (1)" })
            check(SceneryFootprint.Classify(name, "Tents Folder") == "map.place.tents", "verified canvas tent family " + name);
        foreach (string name in new[] { "Wagon1B", "Wagon1B(Empty)", "Wagon1B(Empty) (2)" })
            check(SceneryFootprint.Classify("WagonBody", name) == "map.place.wagon", "verified complete wagon body " + name);
        check(SceneryFootprint.Classify("Eterra_Statue_Model", "Circle_Statue") == "map.place.eterra_statue", "actual small Eterra mesh rather than large circular platform");
        check(SceneryFootprint.Classify("Bridge_Gate_001 (1)", "Bridge") == "map.place.gate" &&
            SceneryFootprint.Classify("Bridge_Gate_002", "Bridge") == "map.place.gate", "authored bridge gateway families");
        foreach (string name in new[] { "Tents Folder", "Tentacle", "MerchantTent", "Tent_04", "BrokenTent_01", "Wagon_Cutted", "Wagon1B", "ShrineCenter", "Circle_Statue", "Circle_Fountain", "Campfire", "Tavern" })
            check(SceneryFootprint.Classify(name, "Foreground") is null, "no category invented from broad or unverified owner " + name);
        check(SceneryFootprint.Classify("WagonBody", "BrokenWagon1B") is null, "damaged wagon child is not complete wagon");

        RuntimeSceneryPlaces.Reset(); MapPlayerFloor.Reset();
        var scene = new Scene { handle = 77, name = "Z32", path = "Assets/Z32.unity", Valid = true, isLoaded = true };
        SceneManager.Scene = scene;
        GameObject Obj(string name, GameObject parent = null) => new() { name = name, scene = scene, Parent = parent };
        var player = Obj("Current player"); PlayerFinder.player = player;
        var agentObject = Obj("NavMesh", player);
        var agent = agentObject.Add(new NavMeshAgent { radius = .1f, height = 2, agentTypeID = 3, areaMask = 11 });
        player.Agent = agent; NavMesh.Climb = .2f; NavMesh.SampleAvailable = true; NavMesh.Blocked = false; NavMesh.Offset = default;
        NavMesh.BakedRadius = .5f; NavMesh.Sample = null;
        var foreground = Obj("Foreground", Obj("SceneObjects"));
        var tent = Obj("Bridge_Gate_001", foreground);
        var renderer = tent.Add(new MeshRenderer { localBounds = new(new(0, 2, 0), new(4, 4, 4)) });
        var world = new MapSectionWorldSnapshot(77, 77, "Z32", "Assets/Z32.unity", 2.5, 0, 0, .5, null);
        Physics.Hits = new(); Physics.FootHits.Clear(); Physics.Throw = false;
        bool Read(out RuntimeMapPlace place) => RuntimeSceneryPlaces.TryRead(world, out place);
        void Move(double x, double y = 0, double z = 0) => world = world with { X = x, Y = y, Z = z };
        check(Read(out var place) && place.TextKey == "map.place.gate", "no floor name or prop collider needed beside authored footprint");
        check(place.Identity == "77:scenery:" + tent.transform.GetInstanceID() && place.SpeechGroup == "map.place.gate", "actual owner identity with category deduplication");
        check(NavMesh.Radius == NavMesh.BakedRadius && NavMesh.Type == 3 && NavMesh.Mask == 11 && NavMesh.RayCalls > 0,
            "uses current player's native NavMesh filter and actual baked clearance for scenery approach");
        check(Physics.Mask == Physics.DefaultRaycastLayers && Physics.Query == QueryTriggerInteraction.Ignore && Physics.Origin.y == 1,
            "body midpoint sightline rejects intervening solid walls without trigger occlusion");
        int discoveries = foreground.Parent.Discoveries;
        Read(out _); check(foreground.Parent.Discoveries == discoveries, "authored renderer scan cached for the scene/player session");
        Move(2.8);
        NavMesh.Sample = (p, radius) =>
        {
            var projected = new Vector3(Math.Max(p.x, 2 + NavMesh.BakedRadius), p.y, p.z);
            return (SceneryFootprint.HorizontalDistance(p, projected) <= radius, projected);
        };
        check(Read(out _), "Z32 baked obstacle radius 0.5 does not reject an approach made by runtime radius 0.1 agent");
        NavMesh.BakedRadius = .1f;
        NavMesh.Sample = (p, radius) =>
        {
            var projected = new Vector3(Math.Max(p.x, 2.5f), p.y, p.z);
            return (SceneryFootprint.HorizontalDistance(p, projected) <= radius, projected);
        };
        check(!Read(out _), "regression reproduces old projection inside the 0.5 baked obstacle border");
        NavMesh.BakedRadius = .5f; NavMesh.Sample = null;
        Move(3); check(Read(out _), "exactly one body width from footprint accepted");
        Move(3.01); check(!Read(out _), "no large radius around tent center");
        Move(0); check(Read(out _), "standing on walkable footprint is nearby, never assumed inside");
        Move(2.5, 2); check(!Read(out _), "bridge above tall tent does not borrow lower floor's scenery");
        Move(2.5); tent.transform.position = new(0, 4, 0); check(!Read(out _), "prop on upper floor rejected"); tent.transform.position = default;
        renderer.enabled = false; check(!Read(out _), "disabled mesh not announced"); renderer.enabled = true;
        tent.Active = false; check(!Read(out _), "inactive phase geometry not announced"); tent.Active = true;
        renderer.Destroyed = true; check(!Read(out _), "destroyed cached renderer rejected"); renderer.Destroyed = false;
        foreground.name = "Background"; check(!Read(out _), "background branch rejected even after caching"); foreground.name = "Foreground";
        tent.name = "Tentacle"; check(!Read(out _), "renamed/reused component cannot retain cached category"); tent.name = "Bridge_Gate_001";
        NavMesh.Blocked = true; check(!Read(out _), "no announcement across unwalkable border"); NavMesh.Blocked = false;
        NavMesh.SampleAvailable = false; check(!Read(out _), "missing walkable origin or approach rejected"); NavMesh.SampleAvailable = true;
        NavMesh.Offset = new(0, 3, 0); check(!Read(out _), "NavMesh cannot snap onto another floor"); NavMesh.Offset = default;
        agentObject.Parent = null; check(!Read(out _), "cached agent reparented away from local player rejected"); agentObject.Parent = player;
        agent.Enabled = false;
        check(Read(out _), "native movement may disable the agent while its query settings remain owned and valid");
        check(!MapPlayerFloor.TryGetAgent(world, out _) && MapPlayerFloor.TryGetQueryAgent(world, out var queryAgent) && queryAgent.Pointer == agent.Pointer,
            "query availability does not authorize disabled agent to expand physical floor allowance");
        agentObject.Active = false; check(!Read(out _), "inactive query owner still rejected"); agentObject.Active = true;
        MapPlayerFloor.Reset();
        check(Read(out _), "query settings rediscovered even when component is disabled at discovery");
        agent.Enabled = true;
        agent.radius = 0; check(!Read(out _), "invalid clearance radius rejected"); agent.radius = .1f;
        agent.areaMask = 0; check(!Read(out _), "empty native walkable mask rejected"); agent.areaMask = 11;
        var wall = Obj("Wall", foreground).Add(new Collider());
        Physics.Hits = new(new RaycastHit { collider = wall, distance = .2f });
        check(!Read(out _), "wall blocks scenery even when NavMesh sample finds our side of it");
        wall.isTrigger = true; check(Read(out _), "quest trigger does not hide scenery"); wall.isTrigger = false;
        var ownCollision = tent.Add(new Collider()); Physics.Hits = new(new RaycastHit { collider = ownCollision, distance = .2f });
        check(Read(out _), "prop's own collision is not an intervening wall");
        Physics.Hits = new(new RaycastHit { collider = player.Add(new Collider()), distance = 0 });
        check(Read(out _), "local player's collision excluded"); Physics.Hits = new();

        tent.transform.Yaw = MathF.PI / 4;
        Move(2.7, 0, 2.7); check(!Read(out _), "rotated footprint does not use empty corners of world AABB");
        Move(0, 0, 3); check(Read(out _), "rotated real mesh edge remains reachable");
        tent.transform.Yaw = 0; tent.transform.lossyScale = new(2, 1, .5f);
        Move(4.5); check(Read(out _), "runtime nonuniform scale changes footprint size");
        Move(0, 0, 2.1); check(!Read(out _), "short scaled axis has its own actual boundary");
        tent.transform.lossyScale = new(0, 1, 1); check(!Read(out _), "zero mesh scale rejected");
        tent.transform.lossyScale = new(1, -1, 1); check(!Read(out _), "upside down mesh has no proven ground footprint");
        tent.transform.lossyScale = new(1, 1, 1); Move(2.5);
        var validBounds = renderer.localBounds;
        renderer.localBounds = new(new(float.NaN, 0, 0), new(4, 4, 4)); check(!Read(out _), "nonfinite bounds rejected");
        renderer.localBounds = new(new(0, 0, 0), new(0, 0, 0)); check(!Read(out _), "empty renderer bounds rejected"); renderer.localBounds = validBounds;

        var wagon = Obj("Wagon1B(Empty)", foreground);
        var body = Obj("WagonBody", wagon);
        body.Add(new MeshRenderer { localBounds = new(new(0, 1, 0), new(4, 2, 4)) });
        wagon.Active = false;
        RuntimeSceneryPlaces.Reset(); Read(out _);
        tent.Active = false; wagon.Active = true;
        check(Read(out place) && place.TextKey == "map.place.wagon" && place.Identity.EndsWith(":" + wagon.transform.GetInstanceID()),
            "inactive phase variants indexed once and announced only once active, using full wagon owner");
        body.Parent = foreground; check(!Read(out _), "reparented wagon part cannot retain old owner"); body.Parent = wagon;
        Physics.Hits = new(new RaycastHit { collider = wagon.Add(new Collider()), distance = .2f });
        check(Read(out _), "wagon parent collision belongs to same authored prop"); Physics.Hits = new();
        var foreign = scene; foreign.handle = 88; wagon.scene = foreign;
        check(!Read(out _), "foreign scene prop owner rejected"); wagon.scene = scene;
        world = world with { ScenePath = "Other" }; check(!Read(out _), "snapshot active scene path validated"); world = world with { ScenePath = scene.path };
        var replacement = Obj("Replacement player"); replacement.Agent = agent; PlayerFinder.player = replacement;
        world = world with { Session = 78 }; check(!Read(out _), "scene session replacement cannot use previous character's agent"); PlayerFinder.player = player;
        world = world with { Session = 79 }; check(Read(out _), "new session rediscovers current authored geometry");
        int beforeWarnings = LeAccess.Plugin.Log.Warnings.Count;
        Physics.Throw = true; check(!Read(out _) && !Read(out _), "native query exceptions fail closed"); Physics.Throw = false;
        check(LeAccess.Plugin.Log.Warnings.Count == beforeWarnings + 1, "repeated native failure warning bounded");
        wagon.Active = false; tent.Active = true; renderer.Destroyed = true;
        RuntimeSceneryPlaces.Reset(); check(!Read(out _), "destroyed renderers excluded from fresh index");
        int scans = foreground.Parent.Discoveries;
        Time.unscaledTimeAsDouble += 20; Read(out _);
        check(foreground.Parent.Discoveries == scans, "empty or unavailable geometry does not rescan entire scene periodically");
        RuntimeSceneryPlaces.Reset(); MapPlayerFloor.Reset();
    }
}
