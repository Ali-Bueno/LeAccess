using System.Text.Json;
using LeAccess.Accessibility.MapSections;

int checks = 0;
void Check(bool condition, string name) { checks++; if (!condition) throw new Exception(name); }
void Invalid(string json, string name)
{
    bool rejected = false;
    try { MapSectionCatalog.Parse(json); } catch (FormatException) { rejected = true; }
    Check(rejected,name);
}
object Section(string id, int priority = 0, double minY = 0, double maxY = 10,
    double[][] points = null, string key = null) => new
{
    id, textKey = key ?? "section." + id, priority, minY, maxY,
    polygon = (points ?? new[] { new[]{0d,0d},new[]{4d,0d},new[]{4d,4d},new[]{0d,4d} }).Select(p => new{x=p[0],z=p[1]}).ToArray()
};
string Catalog(params object[] sections) => JsonSerializer.Serialize(new { maps = new[] { new { scene = "town", buildHash = "verified", sections } } });
SectionMap Map(params object[] sections) => MapSectionCatalog.Parse(Catalog(sections)).Find("town","verified");
var square = Map(Section("square")).Sections[0];
Check(square.Id == "square" && square.TextKey == "section.square","section identity");
for (int ix = -2; ix <= 10; ix++)
    for (int iz = -2; iz <= 10; iz++)
    {
        double x = ix / 2d, z = iz / 2d;
        Check(square.Contains(x,5,z) == (x>=0 && x<=4 && z>=0 && z<=4),"square analytic sample " + x + "," + z);
    }
Check(square.Contains(0,0,0) && square.Contains(4,10,4),"XZ corners and Y edges inclusive");
Check(!square.Contains(2,-.001,2) && !square.Contains(2,10.001,2),"vertical bounds enforced");
Check(square.Contains(-.5,-.5,2,.5) && square.Contains(4.5,10.5,2,.5),"margin extends edges and Y");
Check(!square.Contains(-.501,5,2,.5) && !square.Contains(2,-.501,2,.5),"outside expanded margin");
Check(square.Contains(4.3,5,4.3,.5) && !square.Contains(4.5,5,4.5,.5),"corner margin is circular distance");
Check(!square.Contains(double.NaN,5,2) && !square.Contains(2,double.PositiveInfinity,2) &&
    !square.Contains(2,5,double.NegativeInfinity),"nonfinite position rejected");
foreach (double margin in new[] {-1d,double.NaN,double.PositiveInfinity})
{
    bool rejected = false;
    try { square.Contains(1,1,1,margin); } catch (ArgumentOutOfRangeException) { rejected = true; }
    Check(rejected,"invalid hysteresis margin " + margin);
}
var reverse = Map(Section("reverse",points:new[] {new[]{0d,4d},new[]{4d,4d},new[]{4d,0d},new[]{0d,0d}})).Sections[0];
Check(reverse.Contains(2,5,2) && reverse.Area == square.Area,"clockwise polygon valid");
var concave = Map(Section("concave",points:new[] {new[]{0d,0d},new[]{4d,0d},new[]{4d,1d},new[]{1d,1d},new[]{1d,4d},new[]{0d,4d}})).Sections[0];
Check(concave.Contains(.5,5,3) && concave.Contains(3,5,.5),"concave arms inside");
Check(!concave.Contains(2,5,2),"concave notch outside");
Check(concave.Contains(1,5,2) && concave.Contains(2,5,1),"concave boundary inclusive");
Check(concave.Contains(1.4,5,2,.5) && !concave.Contains(1.6,5,2,.5),"concave edge margin");
var triangle = Map(Section("triangle",points:new[] {new[]{0d,0d},new[]{4d,0d},new[]{0d,4d}})).Sections[0];
Check(triangle.Contains(2,5,2) && !triangle.Contains(2.01,5,2),"diagonal boundary exact");
var floor = Map(Section("floor",minY:3,maxY:3)).Sections[0];
Check(floor.Contains(1,3,1) && !floor.Contains(1,3.01,1),"zero-height plane supported");
var smallPoints = new[] {new[]{1d,1d},new[]{2d,1d},new[]{2d,2d},new[]{1d,2d}};
var overlap = Map(Section("big"),Section("small",points:smallPoints));
Check(overlap.FindSection(1.5,5,1.5).Id == "small","smallest area wins equal priority");
Check(overlap.FindSection(3,5,3).Id == "big" && overlap.FindSection(5,5,5) == null,"only containing sections compete");
overlap = Map(Section("big",priority:1),Section("small",points:smallPoints));
Check(overlap.FindSection(1.5,5,1.5).Id == "big","higher priority wins despite area");
overlap = Map(Section("zeta"),Section("Alpha"),Section("alpha"));
Check(overlap.FindSection(2,5,2).Id == "Alpha","ordinal id tie break independent of locale/order");
var catalog = MapSectionCatalog.Parse(Catalog(Section("a")));
Check(catalog.Find("town","verified") == catalog.Find("town","verified"),"profile object identity stable");
Check(catalog.Find("Town","verified") == null && catalog.Find("town","other") == null &&
    catalog.Find(null,"verified") == null,"exact scene/build identity");
Check(MapSectionCatalog.Parse("{\"maps\":[]}").Find("town","verified") == null,"empty catalog valid");
Check(Map().Sections.Count == 0,"empty section list valid");

foreach (string json in new[] { null,"", "not JSON", "[]", "null", "{}", "{\"maps\":null}", "{\"maps\":{}}", "{\"maps\":[{}]}" })
    Invalid(json,"malformed catalog " + json);
Invalid(Catalog(Section("same"),Section("same")),"duplicate section ids");
Invalid(Catalog(Section(" ")),"blank id");
Invalid(Catalog(Section("id",key:" ")),"blank text key");
Invalid(Catalog(Section("id",minY:2,maxY:1)),"reversed Y limits");
Invalid(Catalog(Section("id",points:new[] {new[]{0d,0d},new[]{1d,1d}})),"too few points");
Invalid(Catalog(Section("id",points:new[] {new[]{0d,0d},new[]{1d,0d},new[]{1d,0d},new[]{0d,1d}})),"duplicate vertices");
Invalid(Catalog(Section("id",points:new[] {new[]{0d,0d},new[]{1d,1d},new[]{2d,2d}})),"zero area");
Invalid(Catalog(Section("id",points:new[] {new[]{0d,0d},new[]{4d,4d},new[]{0d,4d},new[]{4d,0d}})),"crossing polygon");
Invalid(Catalog(Section("id",points:new[] {new[]{0d,0d},new[]{4d,0d},new[]{2d,0d},new[]{2d,4d},new[]{0d,4d}})),"adjacent overlapping edges");
Invalid(Catalog(Section("id",points:new[] {new[]{0d,0d},new[]{4d,0d},new[]{4d,4d},new[]{2d,0d},new[]{0d,4d}})),"nonadjacent edge touch");
string valid = Catalog(Section("valid"));
foreach (string replacement in new[] {"null","\"NaN\"","1e400"})
    Invalid(valid.Replace("\"minY\":0","\"minY\":"+replacement),"invalid finite number " + replacement);
Invalid(valid.Replace("\"priority\":0","\"priority\":1.5"),"priority must be integer");
Invalid(valid.Replace("\"scene\":\"town\"","\"scene\":\" \""),"blank scene");
Invalid(valid.Replace("\"buildHash\":\"verified\"","\"buildHash\":null"),"missing build identity");
string mapJson = JsonDocument.Parse(valid).RootElement.GetProperty("maps")[0].GetRawText();
Invalid("{\"maps\":["+mapJson+","+mapJson+"]}","duplicate scene/build map");
Check(MapSectionCatalog.Parse("{\"maps\":["+mapJson+","+mapJson.Replace("verified","other")+"]}").Find("town","other") != null,"separate build profiles allowed");
var multipleFiles = MapSectionCatalog.Parse(new[]{valid,valid.Replace("\"scene\":\"town\"","\"scene\":\"otherTown\"")});
Check(multipleFiles.Find("town","verified") != null && multipleFiles.Find("otherTown","verified") != null,
    "multiple resources merged without losing either profile");
bool duplicateResourcesRejected = false;
try { MapSectionCatalog.Parse(new[]{valid,valid}); } catch (FormatException) { duplicateResourcesRejected = true; }
Check(duplicateResourcesRejected,"duplicate profiles rejected across resource boundaries");
bool malformedResourceRejected = false;
try { MapSectionCatalog.Parse(new[]{valid,"malformed"}); } catch (FormatException) { malformedResourceRejected = true; }
Check(malformedResourceRejected,"malformed later resource does not publish partial catalog");
Check(MapSectionCatalog.Parse(Array.Empty<string>()).Find("town","verified") == null,"empty resource list yields empty catalog");

string anchor = "{\"path\":\"Root/Child\",\"x\":1,\"y\":2,\"z\":3,\"tolerance\":0.01}";
string WithAnchors(string value) => valid.Replace("\"sections\":", "\"scenePath\":\"Assets/Town.unity\",\"anchors\":"+value+",\"sections\":");
var anchored = MapSectionCatalog.Parse(WithAnchors("["+anchor+"]")).Find("town","verified");
Check(anchored.Scene == "town" && anchored.ScenePath == "Assets/Town.unity" && anchored.BuildHash == "verified","map provenance retained");
Check(anchored.Anchors.Single() == new MapAnchor("Root/Child",1,2,3,.01),"anchor coordinates/tolerance retained");
Check(MapSectionCatalog.Parse(WithAnchors("null")).Find("town","verified").Anchors.Length == 0,"null optional anchors accepted");
Invalid(WithAnchors("["+anchor+","+anchor+"]"),"duplicate anchor paths");
Invalid(WithAnchors("["+anchor.Replace("0.01","0")+"]"),"zero anchor tolerance");
Invalid(WithAnchors("["+anchor.Replace("0.01","-1")+"]"),"negative anchor tolerance");
Invalid(WithAnchors("["+anchor.Replace("\"x\":1","\"x\":1e400")+"]"),"nonfinite anchor coordinate");
Invalid(WithAnchors("["+anchor.Replace("Root/Child"," ")+"]"),"blank anchor path");

object Component(double left, double bottom, double right, double top, double minY = 0, double maxY = 10) => new
{
    minY, maxY, polygon = new[] {new{x=left,z=bottom},new{x=right,z=bottom},new{x=right,z=top},new{x=left,z=top}}
};
object Multi(string id, object[] components, int priority = 0) => new {id,textKey="section."+id,priority,components};
var joined = Map(Multi("joined",new[] {Component(0,0,4,4),Component(4,0,8,4)}));
var joinedSection = joined.Sections[0];
Check(joined.FindSection(2,5,2) == joined.FindSection(6,5,2) &&
    joined.FindSection(4,5,2) == joinedSection,"multipart returns one logical identity across inclusive seam");
var componentState = new MapSectionState();
var seamMessages = new List<string>();
foreach (double x in new[] {2d,3d,4d,5d,6d,4d,2d})
    for (int sample = 0; sample < 2; sample++)
        if (componentState.Observe(1,joined.FindSection(x,5,2),true,out string seamKey)) seamMessages.Add(seamKey);
Check(seamMessages.SequenceEqual(new[]{"section.joined"}),"crossing every component seam announces once");
var islands = Map(Multi("islands",new[] {Component(0,0,4,4),Component(8,0,12,4)}));
var islandsSection = islands.Sections[0];
Check(islandsSection.Contains(2,5,2) && islandsSection.Contains(10,5,2),"disjoint components inside union");
Check(!islandsSection.Contains(6,5,2) && islands.FindSection(6,5,2) == null,"gap not filled by convex hull");
Check(islandsSection.Contains(4.5,10.5,2,.5) && !islandsSection.Contains(6,5,2,.5),"multipart hysteresis extends edges without filling distant gap");
var ring = Map(Multi("ring",new[] {Component(0,0,4,1),Component(0,3,4,4),Component(0,1,1,3),Component(3,1,4,3)}));
Check(ring.Sections[0].Contains(.5,5,2) && ring.Sections[0].Contains(2,5,.5) &&
    !ring.Sections[0].Contains(2,5,2),"component union preserves central hole");
var floors = Map(Multi("floors",new[] {Component(0,0,4,4,0,1),Component(0,0,4,4,5,6)}));
Check(floors.Sections[0].Contains(2,.5,2) && floors.Sections[0].Contains(2,5.5,2) &&
    !floors.Sections[0].Contains(2,3,2),"per-component Y bounds preserve floor gap");
var localArea = Map(Multi("islands",new[] {Component(0,0,1,1),Component(20,0,120,100)}),
    Section("competitor",points:new[] {new[]{0d,0d},new[]{2d,0d},new[]{2d,2d},new[]{0d,2d}}));
Check(localArea.FindSection(.5,5,.5).Id == "islands","local component area wins despite huge distant component");
localArea = Map(Multi("overlapping",new[] {Component(0,0,10,10),Component(0,0,1,1)}),
    Section("competitor",points:new[] {new[]{0d,0d},new[]{2d,0d},new[]{2d,2d},new[]{0d,2d}}));
Check(localArea.FindSection(.5,5,.5).Id == "overlapping","minimum containing component area resolves internal overlap");
localArea = Map(Multi("small",new[] {Component(0,0,1,1)},priority:-1),Section("higher"));
Check(localArea.FindSection(.5,5,.5).Id == "higher","priority still precedes component area");
localArea = Map(Multi("z",new[] {Component(0,0,4,4)}),Section("a"));
Check(localArea.FindSection(2,5,2).Id == "a","ordinal tie works across legacy and multipart");
Check(Map(Section("legacy")).FindSection(2,5,2).Id == "legacy","legacy single polygon back compatibility");
Invalid(Catalog(Multi("empty",Array.Empty<object>())),"empty components rejected");
Invalid(Catalog(new {id="invalid",textKey="section.invalid",priority=0,components=(object)null}),"null components rejected");
Invalid(Catalog(Multi("bad",new[]{Component(0,0,4,4,10,0)})),"reversed component Y rejected");
Invalid(Catalog(Multi("bad",new object[]{new{minY=0,maxY=10,polygon=new[]{new{x=0,z=0},new{x=1,z=1}}}})),"undersized component rejected");
Invalid(Catalog(Multi("bad",new[]{Component(0,0,4,4)})).Replace("\"minY\":0","\"minY\":1e400"),"nonfinite component Y rejected");
Invalid(Catalog(Multi("bad",new[]{Component(0,0,4,4)})).Replace("\"x\":0","\"x\":1e400"),"nonfinite component point rejected");
Invalid(Catalog(new {id="mixed",textKey="section.mixed",priority=0,minY=0,maxY=10,
    components=new[]{Component(0,0,4,4)}}),"ambiguous legacy fields mixed with components rejected");
Invalid(Catalog(Multi("same",new[]{Component(0,0,4,4)}),Section("same")),"logical ids remain unique across formats");

var a = Map(Section("a")).Sections[0]; var b = Map(Section("b")).Sections[0];
var state = new MapSectionState();
Check(!state.Observe(1,a,true,out _) && state.HasPendingContent,"first entry observation pending");
Check(state.Observe(1,a,true,out string key) && key == "section.a" && !state.HasPendingContent,"matching entry delivers");
Check(!state.Observe(1,a,true,out _),"unchanged section silent");
Check(!state.Observe(1,(MapSection)null,true,out _) && !state.HasPendingContent,"single outside drops pending");
Check(!state.Observe(1,a,true,out _) && !state.Observe(1,a,true,out _),"outside flicker does not rearm");
state.Observe(1,(MapSection)null,true,out _); state.Observe(1,(MapSection)null,true,out _);
Check(!state.Observe(1,a,true,out _) && state.Observe(1,a,true,out _),"confirmed outside rearms return");
Check(!state.Observe(1,b,false,out _) && !state.Observe(1,b,false,out _) && state.HasPendingContent,"blocked speech tracks current section");
Check(!state.Observe(1,a,true,out _),"changed pending needs confirmation");
Check(state.Observe(1,a,true,out key) && key == "section.a","old blocked section never emitted");
Check(!state.Observe(2,a,true,out _) && state.Observe(2,a,true,out _),"session change resets same section");
state.Reset();
Check(!state.HasPendingContent,"reset clears pending");
Check(!state.Observe(0,(MapSection)null,true,out _) && !state.Observe(0,(MapSection)null,true,out _),"empty session accepts outside observations");
Check(!state.Observe(0,a,false,out _) && !state.Observe(0,a,false,out _),"no delivery while speech blocked");
Check(state.Observe(0,a,true,out key) && key == "section.a","confirmed current section delivers when allowed");
state.Reset(); state.Observe(1,a,false,out _); state.Observe(1,a,false,out _);
state.Observe(1,(MapSection)null,false,out _); state.Observe(1,(MapSection)null,false,out _);
Check(!state.Observe(1,(MapSection)null,true,out _) && !state.HasPendingContent,"outside never emits obsolete pending");
state.Reset();
Check(!state.Observe(1,"roomA","Cave",true,out _) && state.Observe(1,"roomA","Cave",true,out key) && key == "Cave",
    "string identity/text overload settles a localized room description");
Check(!state.Observe(1,"roomA","Cave",true,out _),"stable room description silent");
Check(!state.Observe(1,"roomB","Cave",true,out _) && state.HasPendingContent &&
    state.Observe(1,"roomB","Cave",true,out key) && key == "Cave","different ids with same text announce distinct room entry");
Check(!state.Observe(1,"roomB","Updated cave",true,out _) &&
    state.Observe(1,"roomB","Updated cave",true,out key) && key == "Updated cave","same id changed text requires two matching observations");
Check(!state.Observe(1,"roomB","Discarded text",false,out _) && !state.Observe(1,"roomB","Latest text",false,out _) &&
    !state.Observe(1,"roomB","Latest text",false,out _) && state.HasPendingContent,"latest room text tracked while blocked");
Check(state.Observe(1,"roomB","Latest text",true,out key) && key == "Latest text","deferred room text never speaks earlier description");
state.Observe(1,null,"ignored outside description",false,out _); state.Observe(1,null,"ignored outside description",false,out _);
Check(!state.HasPendingContent,"null identity always outside despite text");
Check(!state.Observe(1,"roomB","Latest text",true,out _) && state.Observe(1,"roomB","Latest text",true,out _),
    "confirmed string outside rearms same room");
Check(!state.Observe(2,"roomB","Latest text",true,out _) && state.Observe(2,"roomB","Latest text",true,out _),
    "string room session reset");
state.Reset();
Check(!state.Observe(1,"room","",true,out _) && !state.Observe(1,"room","",true,out _) &&
    !state.HasPendingContent,"empty room description is not emitted");
Check(square.SpeechGroup == square.Id,"legacy speech identity defaults to geometry id");
object Grouped(string id, string group, double left) => new {
    id, speechGroup=group, textKey="section.walkway", priority=0, minY=0, maxY=10,
    polygon=new[]{new{x=left,z=0d},new{x=left+4,z=0d},new{x=left+4,z=4d},new{x=left,z=4d}}
};
var grouped=Map(Grouped("pieceA","walkway",0),Grouped("pieceB","walkway",4),Grouped("pieceC","other",10));
var first=grouped.FindSection(2,5,2); var adjacent=grouped.FindSection(6,5,2);
Check(first.Id != adjacent.Id && first.SpeechGroup == adjacent.SpeechGroup,"group retains distinct geometry identities");
state.Reset();
Check(!state.Observe(1,first,true,out _) && state.Observe(1,adjacent,true,out key),"adjacent pieces settle shared speech group");
Check(!state.Observe(1,first,true,out _) && !state.Observe(1,adjacent,true,out _) && !state.HasPendingContent,
    "crossing shared group pieces does not duplicate speech");
var different=grouped.FindSection(12,5,2);
Check(!state.Observe(1,different,true,out _) && state.Observe(1,different,true,out key),"different group with same text still announces");
Check(grouped.FindSection(9,5,2)==null,"grouping does not fill gap between geometry pieces");
state.Observe(1,(MapSection)null,true,out _); state.Observe(1,(MapSection)null,true,out _);
Check(!state.Observe(1,adjacent,true,out _) && state.Observe(1,first,true,out key),"confirmed gap rearms shared group entry");
state.Reset(); state.Observe(1,first,false,out _); state.Observe(1,adjacent,false,out _);
Check(state.HasPendingContent && state.Observe(1,first,true,out key),"blocked shared group entry delivers once from current piece");
Invalid(Catalog(Grouped("bad"," ",0)),"blank speech group rejected");
Invalid(Catalog(Grouped("bad",null,0)),"explicit null speech group rejected");
double[] identityMatrix = {1,0,0,7, 0,1,0,8, 0,0,1,9, 0,0,0,1};
object Sourced(string path, double[] matrix, double tolerance = .01) => new {
    id="sourced", textKey="section.bridge", priority=2, minY=0, maxY=10,
    polygon=new[]{new{x=0d,z=0d},new{x=4d,z=0d},new{x=4d,z=4d},new{x=0d,z=4d}},
    source=new{path,matrix,tolerance}
};
var sourceMap=Map(Sourced("Root/Bridge",identityMatrix),Section("fallback"));
var sourceSection=sourceMap.FindSection(2,5,2);
Check(sourceSection.Source.Path=="Root/Bridge" && sourceSection.Source.Matrix.SequenceEqual(identityMatrix) &&
    sourceSection.Source.Tolerance==.01,"source matrix preserves row-major world translation and path");
Check(square.Source==null,"legacy profile has no section source");
int sourceCalls=0;
Check(sourceMap.FindSection(2,5,2,s=>{sourceCalls++; return s.Source==null;}).Id=="fallback" && sourceCalls==2,
    "unavailable winning source falls back to overlapping available region");
sourceCalls=0;
Check(sourceMap.FindSection(20,5,20,s=>{sourceCalls++; return false;})==null && sourceCalls==0,
    "availability is checked only for geometrically containing sections");
Check(sourceMap.FindSection(2,5,2,s=>false)==null,"all unavailable sources produce outside");
foreach(var path in new[]{""," ","/Root","Root/","Root//Bridge","Root/ /Bridge"})
    Invalid(Catalog(Sourced(path,identityMatrix)),"invalid source path segments "+path);
foreach(var length in new[]{0,15,17})
    Invalid(Catalog(Sourced("Root/Bridge",new double[length])),"source matrix must have exactly sixteen values");
Invalid(Catalog(Sourced("Root/Bridge",identityMatrix,0)),"zero source tolerance rejected");
Invalid(Catalog(Sourced("Root/Bridge",identityMatrix,-1)),"negative source tolerance rejected");
bool finiteRejected=false;
try { new MapSectionSource("Root/Bridge",identityMatrix.Select((v,i)=>i==3?double.NaN:v).ToArray(),.01); }
catch(FormatException) { finiteRejected=true; }
Check(finiteRejected,"nonfinite source matrix rejected directly");
finiteRejected=false;
try { new MapSectionSource("Root/Bridge",identityMatrix,double.PositiveInfinity); }
catch(FormatException) { finiteRejected=true; }
Check(finiteRejected,"nonfinite source tolerance rejected directly");
var copyInput=(double[])identityMatrix.Clone(); var sourceCopy=new MapSectionSource("Root/Bridge",copyInput,.01);
copyInput[3]=999;
Check(sourceCopy.Matrix[3]==7,"source constructor retains its own matrix snapshot");
string ColliderCatalog(double[] center, double[] size, string path="Root/Bridge/Floor", bool nested=false)
{
    var node=System.Text.Json.Nodes.JsonNode.Parse(Catalog(Sourced("Root/Bridge",identityMatrix)));
    var collider=System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(new{path,matrix=identityMatrix,tolerance=.01,center,size}));
    if(nested) collider["colliders"]=new System.Text.Json.Nodes.JsonArray();
    node["maps"][0]["sections"][0]["source"]["colliders"]=new System.Text.Json.Nodes.JsonArray(collider);
    return node.ToJsonString();
}
var colliderSource=MapSectionCatalog.Parse(ColliderCatalog(new[]{1d,2,3},new[]{4d,5,6})).Find("town","verified").Sections[0].Source;
Check(colliderSource.Colliders.Count==1 && colliderSource.Colliders[0].Transform.Path=="Root/Bridge/Floor" &&
    colliderSource.Colliders[0].Center.SequenceEqual(new[]{1d,2,3}) && colliderSource.Colliders[0].Size.SequenceEqual(new[]{4d,5,6}),
    "source preserves exact collider transform/local center/size");
Check(sourceCopy.Colliders.Count==0,"source without collider data remains compatible");
foreach(var length in new[]{0,2,4}) {
    Invalid(ColliderCatalog(new double[length],new[]{1d,2,3}),"collider center requires exactly three coordinates");
    Invalid(ColliderCatalog(new[]{1d,2,3},new double[length]),"collider size requires exactly three coordinates");
}
Invalid(ColliderCatalog(new[]{1d,2,3},new[]{0d,2,3}),"collider zero size rejected");
Check(MapSectionCatalog.Parse(ColliderCatalog(new[]{1d,2,3},new[]{-1d,2,3})).Find("town","verified")
    .Sections[0].Source.Colliders[0].Size[0]==-1,"collider signed authored size preserved exactly");
Invalid(ColliderCatalog(new[]{1d,2,3},new[]{1d,2,3},"Root//Floor"),"collider path segments validated");
Invalid(ColliderCatalog(new[]{1d,2,3},new[]{1d,2,3},nested:true),"collider nesting rejected even when empty");
bool malformedCollider=false;
try { new MapSectionCollider(sourceCopy,new[]{double.NaN,0,0},new[]{1d,2,3}); }
catch(FormatException) { malformedCollider=true; }
Check(malformedCollider,"nonfinite collider center rejected");
var localCenter=new[]{1d,2,3};var localSize=new[]{4d,5,6};
var retainedCollider=new MapSectionCollider(sourceCopy,localCenter,localSize);
var inputColliders=new[]{retainedCollider};var retainedSource=new MapSectionSource("Root",identityMatrix,.01,inputColliders);
localCenter[0]=99;localSize[0]=99;inputColliders[0]=null;
Check(retainedSource.Colliders[0].Center[0]==1 && retainedSource.Colliders[0].Size[0]==4,"source/collider data retain immutable snapshots");
bool ambiguousColliders=false;
try { new MapSectionSource("Root",identityMatrix,.01,new[]{retainedCollider,retainedCollider}); }
catch(FormatException) { ambiguousColliders=true; }
Check(ambiguousColliders,"duplicate contributing collider paths rejected");
ambiguousColliders=false;
try { new MapSectionSource("Root",identityMatrix,.01,new MapSectionCollider[]{null}); }
catch(FormatException) { ambiguousColliders=true; }
Check(ambiguousColliders,"null contributing collider rejected");
var scenery = new SceneryPresenceState();
bool ObserveScenery(string id, bool canSpeak = true, bool valid = true, bool tent = true) =>
    scenery.Observe(1, id, id == null ? null : "Tent", tent, canSpeak, "Left the tent", out _, valid);
Check(ObserveScenery("tent-a"), "exact tent entry speaks once");
Check(!ObserveScenery("tent-a"), "unchanged tent stays silent");
Check(!ObserveScenery(null), "single outside sample cannot claim departure");
Check(!ObserveScenery("tent-a"), "boundary jitter does not repeat tent");
Check(!ObserveScenery(null, valid:false) && !ObserveScenery(null, valid:false), "unavailable geometry cannot claim departure");
Check(!ObserveScenery("tent-a"), "recovery in same tent remains silent");
Check(!ObserveScenery(null), "first proven outside observation settles");
Check(ObserveScenery(null), "second proven outside observation announces tent departure");
Check(!ObserveScenery(null), "unchanged outside does not repeat departure");
Check(ObserveScenery("tent-b"), "a second actual tent has its own entry identity");
Check(ObserveScenery("tent-c"), "adjacent distinct tent is not collapsed by category");
Check(!ObserveScenery(null,canSpeak:false) && !ObserveScenery(null,canSpeak:false) && scenery.HasPendingContent,
    "proven departure yields to higher-priority speech");
Check(ObserveScenery("tent-c") && !scenery.HasPendingContent, "return cancels deferred exit in favor of current presence");
Check(!ObserveScenery(null,canSpeak:false) && !ObserveScenery(null,canSpeak:false), "another exit can defer");
Check(!ObserveScenery(null,valid:false) && !scenery.HasPendingContent, "loss of authoritative position cancels stale exit");
Check(!ObserveScenery(null) && ObserveScenery(null), "recovered departure requires fresh confirmation");
scenery.Reset();
Check(!ObserveScenery("tent-a",canSpeak:false) && scenery.HasPendingContent, "unsaid entry may defer");
Check(!ObserveScenery(null) && !ObserveScenery(null) && !scenery.HasPendingContent, "unspoken brief pass never produces departure speech");
Check(ObserveScenery("tent-a"), "arrival after unsaid pass speaks");
Check(scenery.Observe(2,null,null,false,true,"Left the tent",out _)==false, "new session cannot inherit departure");
Check(ObserveScenery("tent-a"), "session change rearms current tent");
Check(ObserveScenery("wagon",tent:false), "another known place replaces exact tent");
Check(!ObserveScenery(null) && !ObserveScenery(null), "leaving unrelated scenery does not say left tent");
Console.WriteLine($"Passed {checks} map section checks.");
