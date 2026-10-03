using System.Numerics;
using LeAccess.Accessibility.Combat;

int checks = 0;
void Equal<T>(T expected, T actual, string name)
{
    checks++;
    if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"{name}: expected {expected}, got {actual}");
}

var english = new Dictionary<string, string>
{
    ["enemy_scan.none"] = "No enemies nearby", ["enemy_scan.count_one"] = "1 enemy nearby",
    ["enemy_scan.count_many"] = "{0} enemies nearby", ["enemy_scan.entry"] = "{0}, {1}, about {2} units",
    ["enemy_scan.entry_boss"] = "{0}, boss, {1}, about {2} units", ["enemy_scan.unnamed"] = "Unknown enemy",
    ["enemy_scan.direction_here"] = "right on you",
    ["guidance.direction.up"] = "up", ["guidance.direction.right"] = "right", ["guidance.direction.down"] = "down",
    ["guidance.direction.left"] = "left", ["guidance.direction.up_right"] = "up and right",
};
var spanish = new Dictionary<string, string>(english)
{
    ["enemy_scan.none"] = "Sin enemigos cerca", ["enemy_scan.count_many"] = "{0} enemigos cerca",
    ["enemy_scan.entry"] = "{0}, {1}, unas {2} unidades", ["enemy_scan.entry_boss"] = "{0}, jefe, {1}, unas {2} unidades",
    ["guidance.direction.up"] = "arriba", ["guidance.direction.right"] = "derecha", ["guidance.direction.up_right"] = "arriba a la derecha",
};
EnemyScanText Words(Dictionary<string, string> table) => (key, args) => string.Format(table[key], args);

var player = new Vector3(10, 5, 10);
var forward = new Vector3(0, 0, 1);
var right = new Vector3(1, 0, 0);
string Scan(IEnumerable<EnemyContact> contacts, Dictionary<string, string> table = null) =>
    EnemyScanReport.Build(contacts, player, forward, right, Words(table ?? english));
EnemyContact At(int id, string name, float dx, float dz, bool boss = false, float dy = 0) =>
    new(id, name, boss, player + new Vector3(dx, dy, dz));

Equal("No enemies nearby", Scan(Array.Empty<EnemyContact>()), "none nearby");
Equal("Sin enemigos cerca", Scan(Array.Empty<EnemyContact>(), spanish), "none nearby in Spanish");
Equal("1 enemy nearby. Wolf, up, about 4 units", Scan(new[] { At(1, "Wolf", 0, 4) }), "single enemy ahead");

var many = new[] { At(1, "Far", 0, 30), At(2, "Near", 5, 0), At(3, "Mid", 0, -8), At(4, "Mid2", -12, 0), At(5, "Farthest", 40, 40) };
Equal("5 enemies nearby. Near, right, about 5 units. Mid, down, about 8 units. Mid2, left, about 12 units",
    Scan(many), "ordered by distance, limited to three, true total");
Equal(EnemyScanReport.MaxListed, 3, "limit constant");

Equal("2 enemies nearby. Lagon, boss, up, about 6 units. Wolf, right, about 9 units",
    Scan(new[] { At(1, "Wolf", 9, 0), At(2, "Lagon", 0, 6, boss: true) }), "boss tag and order");
Equal("2 enemigos cerca. Lagon, jefe, arriba, unas 6 unidades. Wolf, derecha, unas 9 unidades",
    Scan(new[] { At(1, "Wolf", 9, 0), At(2, "Lagon", 0, 6, boss: true) }, spanish), "Spanish text");

Equal("1 enemy nearby. A, up and right, about 7 units", Scan(new[] { At(1, "A", 5, 5) }), "diagonal sector");
var turned = EnemyScanReport.Build(new[] { At(1, "A", 0, 4) }, player, right, new Vector3(0, 0, -1), Words(english));
Equal("1 enemy nearby. A, left, about 4 units", turned, "direction follows camera");

Equal("1 enemy nearby. A, up, about 4 units", Scan(new[] { At(1, "A", 0, 3.5f) }), "half rounds away from zero");
Equal("1 enemy nearby. A, up, about 3 units", Scan(new[] { At(1, "A", 0, 3.49f) }), "rounds down below half");
Equal("1 enemy nearby. A, up, about 3 units", Scan(new[] { At(1, "A", 0, 3, dy: 50) }), "distance is horizontal");

Equal("1 enemy nearby. Unknown enemy, up, about 2 units", Scan(new[] { At(1, "  ", 0, 2) }), "unnamed fallback");
Equal("1 enemy nearby. A, right on you, about 0 units", Scan(new[] { At(1, "A", 0, 0, dy: 1) }), "directly on the player");

var bad = new[] { At(1, "Nan", float.NaN, 1), At(2, "Inf", float.PositiveInfinity, 0), At(3, "Ok", 0, 2) };
Equal("1 enemy nearby. Ok, up, about 2 units", Scan(bad), "non-finite positions skipped and not counted");
Equal("No enemies nearby", Scan(bad.Take(2)), "only invalid positions");

Equal("2 enemies nearby. A, up, about 4 units. B, up, about 4 units",
    Scan(new[] { At(9, "B", 0, 4), At(2, "A", 0, 4) }), "equal distance ordered by id");

Equal(true, NearestEnemySelection.Contains(100, 100), "range boundary included");
Equal(false, NearestEnemySelection.Contains(100, 100.1f), "beyond range");
Equal(false, NearestEnemySelection.Contains(float.NaN, 1), "invalid range");

Console.WriteLine($"Enemy scan tests passed: {checks} checks");
