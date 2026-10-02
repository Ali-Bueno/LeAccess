using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using LeAccess.Accessibility.MapSections;
using LeAccess.I18n;

internal static class GeneratedData
{
    internal static void Check(Action<bool,string> check)
    {
        using var resource=Assembly.GetExecutingAssembly().GetManifestResourceStream("LeAccess.MapSections.generated.json");
        check(resource!=null,"Generated catalog is embedded with production resource name");
        string json=new StreamReader(resource).ReadToEnd();
        var timer=Stopwatch.StartNew();
        var catalog=MapSectionCatalog.Parse(json);
        timer.Stop();
        Console.WriteLine($"Generated catalog parse: {timer.Elapsed.TotalMilliseconds:F2} ms, {json.Length:N0} characters.");
        using var document=JsonDocument.Parse(json);
        var maps=document.RootElement.GetProperty("maps").EnumerateArray().ToArray();
        check(maps.Length==57,"Evidence-backed generated catalog has fifty-seven guarded layouts");
        int sections=0,components=0; SectionMap largest=null;
        foreach(var data in maps)
        {
            string scene=data.GetProperty("scene").GetString(),hash=data.GetProperty("buildHash").GetString();
            check(hash==SceneEvidence.BuildHash,"Every generated profile binds the independently verified installed build");
            var map=catalog.Find(scene,hash);
            check(map!=null && scene!="Z12","Generated exact scene/build profile exists without duplicate manual pilot");
            check(map.ScenePath=="Assets/BundledScenes/"+scene+".unity","Generated scene asset path is exact");
            check(catalog.Find(scene,"wrong-build")==null,"Generated profile rejects another build");
            check(catalog.Find(scene,SceneEvidence.PreviousBuildHash)==null,"Refreshed generated profile rejects previous build");
            check(map.Anchors.Length>=3 && map.Anchors.Select(a=>a.Path).Distinct().Count()==map.Anchors.Length,
                "Generated map has unique runtime anchor paths");
            check(map.Anchors.All(a=>a.Tolerance>0 && double.IsFinite(a.X+a.Y+a.Z)),"Generated anchors are finite and guarded");
            var a=map.Anchors[0]; var b=map.Anchors[1]; var c=map.Anchors[2];
            check(Math.Abs((b.X-a.X)*(c.Z-a.Z)-(b.Z-a.Z)*(c.X-a.X))>0,"Generated anchor basis is not collinear");
            check(map.Sections.Select(s=>s.Source.Path).Distinct().Count()==map.Sections.Count,"Generated section source paths are unique");
            if(largest==null || largest.Sections.Count<map.Sections.Count) largest=map;
            foreach(var sectionData in data.GetProperty("sections").EnumerateArray())
            {
                sections++;
                var section=map.Sections.Single(s=>s.Id==sectionData.GetProperty("id").GetString());
                check(section.Source!=null && section.Source.Matrix.Count==16 && section.Source.Matrix.All(double.IsFinite),
                    "Every generated section has finite row-major source matrix");
                check(section.Source.Tolerance==.01 && section.Source.Matrix[12]==0 && section.Source.Matrix[13]==0 &&
                    section.Source.Matrix[14]==0 && section.Source.Matrix[15]==1,"Source is guarded affine world transform");
                check(section.Source.Colliders.Count>0 && section.Source.Colliders.Select(c=>c.Transform.Path).Distinct().Count()==
                    section.Source.Colliders.Count,"Every generated structural floor has unique contributing collider guards");
                foreach(var collider in section.Source.Colliders)
                    check(collider.Transform.Matrix.Count==16 && collider.Transform.Matrix.All(double.IsFinite) &&
                        collider.Transform.Tolerance==.01 && collider.Center.Count==3 && collider.Center.All(double.IsFinite) &&
                        collider.Size.Count==3 && collider.Size.All(v=>double.IsFinite(v) && v!=0),"Generated collider signed local dimensions and world transform valid");
                check(section.SpeechGroup==section.TextKey,"Generated physical pieces share category speech group");
                foreach(string language in new[]{"en","es"})
                {
                    ModText.SetLanguage(language);
                    string expected=(section.TextKey,language) switch {
                        ("map.section.bridge","en")=>"Bridge", ("map.section.bridge","es")=>"Puente",
                        ("map.section.stairs","en")=>"Stairs", ("map.section.stairs","es")=>"Escaleras",
                        ("map.section.walkway","en")=>"Walkway", ("map.section.walkway","es")=>"Pasarela",
                        ("map.section.cave_entrance","en")=>"Cave entrance", ("map.section.cave_entrance","es")=>"Entrada de cueva",
                        _=>null
                    };
                    check(expected!=null && ModText.Get(section.TextKey)==expected,"Every generated category resolves exactly in "+language);
                }
                foreach(var component in sectionData.GetProperty("components").EnumerateArray())
                {
                    components++;
                    double y=(component.GetProperty("minY").GetDouble()+component.GetProperty("maxY").GetDouble())/2;
                    foreach(var point in component.GetProperty("polygon").EnumerateArray())
                        check(section.Contains(point.GetProperty("x").GetDouble(),y,point.GetProperty("z").GetDouble()),
                            "Parsed union preserves every authored clipped component boundary");
                }
            }
        }
        check(sections==748 && components==6118,"Generated evidence counts match final extraction");
        // Independently sampled installed BoxCollider faces intersecting installed navigation cells;
        // Collider path IDs 29484 (ArenaLobby), 26124 (B7S10), 40378 (B50), 46111 (M_A10).
        const string build=SceneEvidence.BuildHash;
        var golden=new[] {
            (Scene:"ArenaLobby",Id:"ArenaLobby_bridge_156",Key:"map.section.bridge",X:1954.20684460711,Y:30.206069946289062,Z:2004.1718521446815),
            (Scene:"B7S10",Id:"B7S10_stairs_67",Key:"map.section.stairs",X:-23.849960327148438,Y:.023736953735351562,Z:33.55998229980469),
            (Scene:"B50",Id:"B50_walkway_1297",Key:"map.section.walkway",X:148.54028432389248,Y:.12885665893554688,Z:-3.9139613409909177),
            (Scene:"M_A10",Id:"M_A10_bridge_84",Key:"map.section.bridge",X:34.88509679522942,Y:-.9951515197753906,Z:2.031566578310219)
        };
        foreach(var sample in golden)
        {
            var selected=catalog.Find(sample.Scene,build).FindSection(sample.X,sample.Y,sample.Z);
            check(selected?.Id==sample.Id && selected.TextKey==sample.Key,"Independent installed collider/nav point selects expected physical category "+sample.Scene);
            check(!selected.Contains(sample.X,sample.Y+1000,sample.Z),"Independent floor point cannot select another vertical floor");
        }
        // Independent physical X extents of every contributing collider plus one world unit.
        // Other nearby sections may legitimately contain these points; test the exact source owner.
        var outsideX=new[]{1958.346866,-20.030682,156.690234,45.12367798510517};
        for(int i=0;i<golden.Length;i++)
        {
            var sample=golden[i];var owner=catalog.Find(sample.Scene,build).Sections.Single(s=>s.Id==sample.Id);
            check(!owner.Contains(outsideX[i],sample.Y,sample.Z),"Independent physical collider bounds exclude source-owner exterior "+sample.Scene);
        }
        ModText.SetLanguage("en");
        timer.Restart();
        for(int i=0;i<1000;i++) largest.FindSection(largest.Anchors[0].X+i*.01,largest.Anchors[0].Y,largest.Anchors[0].Z);
        timer.Stop();
        Console.WriteLine($"Largest generated profile {largest.Scene}: {largest.Sections.Count} sections; 1,000 lookups {timer.Elapsed.TotalMilliseconds:F2} ms.");
    }
}
