using System.Text.Json;
using LeAccess.Accessibility.MapSections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

internal static class TentCanopyTests
{
    internal static void Run(Action<bool, string> check)
    {
        var root = new GameObject { name = "Tent_01" };
        var renderer = root.Add(new MeshRenderer { localBounds = new(new(0, 2, 0), new(4, 4, 4)) });
        var mesh = new Mesh { name = "Tent_01", vertexCount = 4, subMeshCount = 1 };
        var filter = root.Add(new MeshFilter { sharedMesh = mesh });
        string Profile(object[] outer, object[] holes) => JsonSerializer.Serialize(new {
            buildHash = "verified", profiles = new[] { new {
                name = "Tent_01", vertices = 4, submeshes = 1, min = new[] {-2,0,-2}, max = new[] {2,4,2},
                roofFloor = 2, components = new[] {new {outer, holes}}
            }}
        });
        object[] square = {new[] {-2,-2}, new[] {2,-2}, new[] {2,2}, new[] {-2,2}};
        string json = Profile(square, Array.Empty<object>());
        TentCanopy.Load(json, "verified");
        bool At(float x, float y = 0, float z = 0, float radius = .5f) =>
            TentCanopy.Contains(renderer, new(x,y,z), new(x,y,z), radius);
        check(At(0), "actual canopy covers full body at current position");
        check(!At(1.5f) && !At(2) && !At(2.5f), "boundary, partial body, and nearby footprint do not imply tent presence");
        check(!At(0, 2), "different floor above canopy is outside");
        check(!At(0, 1.5f), "body touching lower canopy plane fails conservatively");
        check(!TentCanopy.Contains(renderer, new(0,0,0), new(2,0,0), .5f), "NavMesh projection outside tent cannot authorize location");
        root.transform.Yaw = MathF.PI/4;
        check(At(0) && !At(2,0,2), "rotation retains exact footprint rather than world AABB corners");
        root.transform.Yaw = 0;
        root.transform.lossyScale = new(2,1,.5f);
        check(At(0,0,0,.4f) && !At(0,0,.7f,.4f), "nonuniform scale uses actual world body radius for edges");
        root.transform.lossyScale = new(1,1,1); root.transform.Yaw = 0;
        object[] concave = {new[] {-2,-2},new[] {2,-2},new[] {2,0},new[] {0,0},new[] {0,2},new[] {-2,2}};
        TentCanopy.Load(Profile(concave, Array.Empty<object>()), "verified");
        check(!At(1,0,1) && At(-1,0,1,.25f), "nonconvex empty corner stays outside actual canopy");
        object[] hole = {new[] {-.5,-.5},new[] {.5,-.5},new[] {.5,.5},new[] {-.5,.5}};
        TentCanopy.Load(Profile(square, new object[] {hole}), "verified");
        check(!At(0) && !At(.75f,0,0,.4f) && At(1.25f,0,0,.25f), "canopy holes and adjacent body overlap stay outside");
        TentCanopy.Load(json, "other-build"); check(!At(0), "unknown build cannot borrow canopy profiles");
        TentCanopy.Load(json, "verified");
        mesh.vertexCount++; check(!At(0) && !TentCanopy.IsAvailable(renderer), "replaced mesh geometry fails closed"); mesh.vertexCount--;
        mesh.subMeshCount++; check(!At(0), "changed submesh topology rejected"); mesh.subMeshCount--;
        mesh.name = "UnverifiedTent"; check(!At(0), "mesh identity guard"); mesh.name = "Tent_01";
        renderer.localBounds = new(new(0,2,0),new(6,4,4)); check(!At(0), "renderer bounds must match extracted mesh");
        renderer.localBounds = new(new(0,2,0),new(4,4,4));
        filter.sharedMesh = null; check(!At(0), "missing mesh has no proximity fallback"); filter.sharedMesh = mesh;
        root.transform.lossyScale = new(1,-1,1); check(!At(0), "inverted canopy rejected"); root.transform.lossyScale = new(1,1,1);
        root.Active = false; check(!At(0), "inactive model does not define current tent"); root.Active = true;

        using var stream = typeof(TentCanopy).Assembly.GetManifestResourceStream("LeAccess.TentCanopies.json");
        using var reader = new StreamReader(stream);
        string installed = reader.ReadToEnd();
        using var data = JsonDocument.Parse(installed);
        TentCanopy.Initialize(data.RootElement.GetProperty("buildHash").GetString());
        var locations = new Dictionary<string, Vector3> {
            ["Tent_01"] = new(-.7260318f,0,-.21210042f),
            ["Tent_02"] = new(2.7865384f,0,.067580886f),
            ["Tent_03"] = new(-2.2824349f,0,-.07114514f)
        };
        var scene = new Scene {handle=99,name="Unmapped",path="Assets/Unmapped.unity",Valid=true,isLoaded=true};
        SceneManager.Scene=scene;root.scene=scene;
        var player=new GameObject {scene=scene,name="Current player"};
        var nav = player.Add(new NavMeshAgent {radius=.1f,height=2});player.Agent=nav;PlayerFinder.player=player;
        NavMesh.Sample=null;NavMesh.SampleAvailable=true;NavMesh.Offset=default;NavMesh.Blocked=false;NavMesh.BakedRadius=.5f;
        Physics.Hits=new();Physics.FootHits.Clear();
        foreach (var profile in data.RootElement.GetProperty("profiles").EnumerateArray())
        {
            Vector3 V(string property) {var a = profile.GetProperty(property);return new(a[0].GetSingle(),a[1].GetSingle(),a[2].GetSingle());}
            var min=V("min");var max=V("max");
            renderer.localBounds.min=min;renderer.localBounds.max=max;
            mesh.name=profile.GetProperty("name").GetString();mesh.vertexCount=profile.GetProperty("vertices").GetInt32();mesh.subMeshCount=profile.GetProperty("submeshes").GetInt32();
            root.name=mesh.name;
            var point=locations[mesh.name];point.y=min.y;
            check(At(point.x,point.y,point.z), "installed " + mesh.name + " source-linked covered point has genuine canopy coverage");
            check(!At(max.x+1,min.y), "installed " + mesh.name + " approach stays silent");
            RuntimeSceneryPlaces.Reset();MapPlayerFloor.Reset();
            var world=new MapSectionWorldSnapshot(99,99,scene.name,scene.path,point.x,point.y,point.z,.5,null);
            check(RuntimeSceneryPlaces.TryRead(world,out var place) && place.TextKey=="map.place.tent" &&
                place.SpeechGroup==place.Identity && RuntimeSceneryPlaces.HasValidObservation,
                "installed tent requires exact canopy and retains own entry identity");
            nav.radius=0;
            check(!RuntimeSceneryPlaces.TryRead(world,out _) && !RuntimeSceneryPlaces.HasValidObservation,
                "missing authoritative navigation settings cannot establish departure");nav.radius=.1f;
            mesh.vertexCount++;
            check(!RuntimeSceneryPlaces.TryRead(world,out _) && !RuntimeSceneryPlaces.HasValidObservation,
                "replaced previous tent geometry cannot establish departure");mesh.vertexCount--;
            TentCanopy.Load(json,"unknown-build");
            check(!RuntimeSceneryPlaces.TryRead(world,out _) && !RuntimeSceneryPlaces.HasValidObservation,
                "lost canopy resource cannot establish departure");
            TentCanopy.Initialize(data.RootElement.GetProperty("buildHash").GetString());
            var otherFloor=world with {Y=point.y-2};
            check(!RuntimeSceneryPlaces.TryRead(otherFloor,out _), "same canopy projection cannot announce tent from lower floor");
            otherFloor=world with {Y=point.y+10};
            check(!RuntimeSceneryPlaces.TryRead(otherFloor,out _), "same canopy projection cannot announce tent from upper floor");
            world=world with {X=max.x+5};
            check(!RuntimeSceneryPlaces.TryRead(world,out _) && RuntimeSceneryPlaces.HasValidObservation,
                "real outside position with unchanged verified tent authorizes departure");
        }
    }
}
