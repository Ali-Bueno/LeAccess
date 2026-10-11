using LeAccess.Accessibility.Vitals;

int checks = 0;
void Equal<T>(T expected, T actual, string name)
{
    checks++;
    if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"{name}: expected {expected}, got {actual}");
}
void Change(VitalsChange actual, int health, int mana, bool interrupt, string name)
{
    Equal(new VitalsChange(health, mana, interrupt), actual, name);
    Equal(health >= 0 || mana >= 0, actual.HasContent, name + " content");
}
foreach (var sample in new (double Current, double Maximum, int Expected)[]
{
    (120,120,100), (60,120,50), (119,120,99), (0,120,0), (-3,120,0),
    (50,0,-1), (50,-1,-1), (2000000000,1,100), (.5,2,25), (.999,1,99),
    (double.NaN,100,-1), (10,double.NaN,-1), (double.PositiveInfinity,100,-1),
    (double.NegativeInfinity,100,-1), (10,double.PositiveInfinity,-1), (10,double.NegativeInfinity,-1),
    (double.MaxValue,double.MaxValue,100), (double.MaxValue/2,double.MaxValue,50),
    (double.Epsilon,double.Epsilon,100), (double.Epsilon,100,0),
    (-double.MaxValue,100,0), (0,double.Epsilon,0)
}) Equal(sample.Expected, VitalsAnnouncementState.Percent(sample.Current, sample.Maximum), "percentage " + sample);
for (int value = 0; value <= 100; value++)
    Equal(value, VitalsAnnouncementState.Percent(value, 100), "exact integer percent " + value);
Equal(29, VitalsAnnouncementState.Percent(58, 200), "exact fraction twenty nine");
Equal(28, VitalsAnnouncementState.Percent(28.999999, 100), "subboundary never rounds up");

var state = new VitalsAnnouncementState();
Change(state.Observe(100,100), -1,-1,false,"initial baseline");
Change(state.Observe(99,99), -1,-1,false,"chip damage");
Change(state.Observe(75,75), -1,-1,false,"exact high boundary");
Change(state.Observe(74,74), 74,74,false,"below high boundary");
Change(state.Observe(60,51), -1,-1,false,"same band");
Change(state.Observe(50,50), -1,-1,false,"exact half boundary");
Change(state.Observe(49,49), 49,49,false,"below half boundary");
Change(state.Observe(30,30), 30,30,false,"jump gives current percentage");
Change(state.Observe(10,10), 10,10,true,"critical jump to exact ten");
Change(state.Observe(9,9), 9,9,true,"below ten");
Change(state.Observe(5,5), -1,-1,false,"same five band");
Change(state.Observe(4,4), 4,-1,true,"four; routine mana below ten stays quiet");
Change(state.Observe(3,3), 3,-1,true,"three");
Change(state.Observe(2,2), 2,-1,true,"two");
Change(state.Observe(1,1), 1,-1,true,"one");
Change(state.Observe(0,0), 0,0,true,"empty");
Change(state.Observe(0,0), -1,-1,false,"empty dedup");
Change(state.Observe(1,1), -1,-1,false,"single-band recovery from empty stays quiet");
Change(state.Observe(100,100), 100,100,false,"recovery full across bands");
Change(state.Observe(75,75), -1,-1,false,"upper band stays silent");
Change(state.Observe(100,100), -1,-1,false,"full is not special");

int[] bands = {75,50,40,30,20,10,5,4,3,2,1};
for (int i = 0; i < bands.Length; i++)
{
    int boundary = bands[i];
    state.Reset();
    Change(state.Observe(boundary,-1), -1,-1,false,"boundary baseline " + boundary);
    Change(state.Observe(boundary-1,-1), boundary-1,-1,boundary-1 <= 10,"strict fall " + boundary);
    Change(state.Observe(boundary,-1), -1,-1,false,"single-band rise stays quiet " + boundary);
    Change(state.Observe(boundary-1,-1), -1,-1,false,"falling back into the spoken band stays quiet " + boundary);
    if (i > 0) Change(state.Observe(bands[i-1],-1), bands[i-1],-1,false,"two-band recovery " + boundary);
}

state.Reset();
Change(state.Observe(2,2), -1,-1,false,"critical initial baseline");
Change(state.Observe(-1,-1), -1,-1,false,"unknown retains baselines");
Change(state.Observe(1,1), 1,-1,true,"critical health resumes last baseline; low mana stays quiet");
state.Reset();
Change(state.Observe(100,-1), -1,-1,false,"partially available baseline");
Change(state.Observe(74,9), 74,-1,false,"late mana takes independent baseline");
Change(state.Observe(74,4), -1,-1,false,"routine low mana alone is quiet below ten");
Change(state.Observe(4,20), 4,20,true,"life falls while mana recovers");
Change(state.Observe(9,10), -1,10,false,"mana falls while life rises one band; mana never interrupts");
Change(state.Observe(9,20), -1,-1,false,"one-band mana rise stays quiet");
Change(state.Observe(-5,30), -1,30,false,"two-band mana recovery; negative unavailable preserves health");
Change(state.Observe(4,-1), -1,-1,false,"health back in its spoken band stays quiet");
Change(state.Observe(3,-1), 3,-1,true,"resumed health falls from retained value");
state.Reset();
Change(state.Observe(0,0), -1,-1,false,"empty initial baseline");
Change(state.Observe(100,100), 100,100,false,"empty baseline recovery");
Change(state.Observe(500,200), -1,-1,false,"overcap percentages upper band");
state.Reset();
Change(state.Observe(-1,-1), -1,-1,false,"unknown after reset");
Change(state.Observe(20,5), -1,-1,false,"first available after reset");
Change(state.Observe(19,4), 19,-1,false,"health fall above ten queues; low mana step quiet");
state.Reset();
Change(state.Observe(100,100,acknowledge:false),-1,-1,false,"explicit acceptance still preserves first silent baseline");
Change(state.Observe(9,9,acknowledge:false),9,9,true,"pending critical crossing retains accepted baseline");
Change(state.Observe(8,8,acknowledge:false),8,8,true,"rejected critical attempt keeps priority with fresh values");
state.Acknowledge(8,-1);
Change(state.Observe(8,8,acknowledge:false),-1,8,false,"acceptance commits only the delivered resource");
state.Acknowledge(-1,8);
Change(state.Observe(8,8,acknowledge:false),-1,-1,false,"accepted independent bands deduplicate");
Change(state.Observe(74,74,acknowledge:false),74,74,false,"unaccepted recovery uses current ordinary values");
Change(state.Observe(8,8,acknowledge:false),-1,-1,false,"returning to acknowledged band discards pending recovery");
state.Reset(); state.Observe(100,100,acknowledge:false); state.Observe(9,9,acknowledge:false);
Change(state.Observe(100,100,acknowledge:false),-1,-1,false,"returning to original band discards pending critical history");
state.Reset();
Change(state.Observe(8,8,acknowledge:false),-1,-1,false,"session reset discards pending crossing and establishes baseline");
// Mana sequence from a real session: each cast crossed 75% and regeneration crossed it back.
state.Reset();
int spoken = 0;
foreach (int mana in new[] {100,62,75,62,75,57,75,62,76,73,76,72,76,68,75,62,75,56,45,50,36,40,51,75,62,75,58,76,60,75,44,50,75})
    if (state.Observe(-1, mana).ManaPercent >= 0) spoken++;
Equal(6, spoken, "cast/regeneration cycles speak only real loss and recovery");
state.Reset();
state.Observe(100,-1); state.Observe(74,-1); state.Observe(100,-1);
Change(state.Observe(45,-1), 45,-1,false,"loss below the spoken band speaks after a quiet rise");
// Mana running dry while casting: the first step below ten and empty speak, nothing interrupts.
state.Reset();
state.Observe(100,100);
Change(state.Observe(100,12), -1,12,false,"mana fall to twelve queues");
Change(state.Observe(100,9), -1,9,false,"first mana step below ten speaks without interrupting");
Change(state.Observe(100,5), -1,-1,false,"further low mana steps are quiet");
Change(state.Observe(100,2), -1,-1,false,"low mana is quiet until empty");
Change(state.Observe(100,0), -1,0,false,"empty mana speaks once");
Change(state.Observe(100,3), -1,-1,false,"regeneration below ten from empty is quiet");
Change(state.Observe(100,11), -1,11,false,"recovery above ten speaks");
Change(state.Observe(100,8), -1,8,false,"a new descent speaks its first low step again");
// Independent meters and minimum changes.
var manaOff = new VitalsPolicy(true, false, 0, 0);
var quietMana = new VitalsAnnouncementState();
quietMana.Observe(100, 100, policy: manaOff);
Change(quietMana.Observe(60, 30, policy: manaOff), 60, -1, false, "life speaks while mana announcements are off");
Change(quietMana.Observe(60, 30), -1, -1, false, "re-enabling mana does not replay the crossings it missed");
Change(quietMana.Observe(60, 20), -1, 20, false, "re-enabled mana speaks its next crossing");
var lifeOff = new VitalsPolicy(false, true, 0, 0);
var quietLife = new VitalsAnnouncementState();
quietLife.Observe(100, 100, policy: lifeOff);
Change(quietLife.Observe(5, 60, policy: lifeOff), -1, 60, false, "mana speaks while life announcements are off, even for critical life");
Change(new VitalsAnnouncementState().Observe(100, 100, policy: new VitalsPolicy(false, false, 0, 0)), -1, -1, false, "both off stay silent");

var stepped = new VitalsAnnouncementState();
var manaStep = new VitalsPolicy(true, true, 0, 30);
stepped.Observe(100, 100, policy: manaStep);
Change(stepped.Observe(100, 74, policy: manaStep), -1, -1, false, "a small mana drop across a band stays quiet");
Change(stepped.Observe(100, 60, policy: manaStep), -1, 60, false, "the drop speaks once it is large enough");
Change(new VitalsAnnouncementState().Observe(100, 74, policy: manaStep), -1, -1, false, "the first reading is still a baseline");
var plain = new VitalsAnnouncementState();
plain.Observe(100, 100);
Change(plain.Observe(100, 74), -1, 74, false, "step zero keeps today's behaviour");

var criticalStep = new VitalsAnnouncementState();
var lifeStep = new VitalsPolicy(true, true, 20, 20);
criticalStep.Observe(22, 12, policy: lifeStep);
Change(criticalStep.Observe(18, 11, policy: lifeStep), -1, -1, false, "small changes above critical stay quiet");
Change(criticalStep.Observe(9, 9, policy: lifeStep), 9, -1, true, "critical life ignores the minimum change");
Change(criticalStep.Observe(9, 0, policy: lifeStep), -1, 0, false, "empty mana ignores the minimum change");

Console.WriteLine($"Passed {checks} vitals state checks.");
