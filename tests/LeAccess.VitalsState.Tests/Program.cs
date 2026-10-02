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
Change(state.Observe(4,4), 4,4,true,"four");
Change(state.Observe(3,3), 3,3,true,"three");
Change(state.Observe(2,2), 2,2,true,"two");
Change(state.Observe(1,1), 1,1,true,"one");
Change(state.Observe(0,0), 0,0,true,"empty");
Change(state.Observe(0,0), -1,-1,false,"empty dedup");
Change(state.Observe(1,1), 1,1,false,"recovery one");
Change(state.Observe(100,100), 100,100,false,"recovery full across bands");
Change(state.Observe(75,75), -1,-1,false,"upper band stays silent");
Change(state.Observe(100,100), -1,-1,false,"full is not special");

foreach (int boundary in new[] {75,50,40,30,20,10,5,4,3,2,1})
{
    state.Reset();
    Change(state.Observe(boundary,-1), -1,-1,false,"boundary baseline " + boundary);
    Change(state.Observe(boundary-1,-1), boundary-1,-1,boundary-1 <= 10,"strict fall " + boundary);
    Change(state.Observe(boundary,-1), boundary,-1,false,"exact recovery " + boundary);
}

state.Reset();
Change(state.Observe(2,2), -1,-1,false,"critical initial baseline");
Change(state.Observe(-1,-1), -1,-1,false,"unknown retains baselines");
Change(state.Observe(1,1), 1,1,true,"critical resumes last baseline");
state.Reset();
Change(state.Observe(100,-1), -1,-1,false,"partially available baseline");
Change(state.Observe(74,9), 74,-1,false,"late mana takes independent baseline");
Change(state.Observe(74,4), -1,4,true,"critical mana alone");
Change(state.Observe(4,20), 4,20,true,"life falls while mana recovers");
Change(state.Observe(9,10), 9,10,true,"mana falls while life recovers");
Change(state.Observe(9,20), -1,20,false,"mana recovery queues");
Change(state.Observe(-5,30), -1,30,false,"negative unavailable preserves health");
Change(state.Observe(4,-1), 4,-1,true,"resumed health uses retained value");
state.Reset();
Change(state.Observe(0,0), -1,-1,false,"empty initial baseline");
Change(state.Observe(100,100), 100,100,false,"empty baseline recovery");
Change(state.Observe(500,200), -1,-1,false,"overcap percentages upper band");
state.Reset();
Change(state.Observe(-1,-1), -1,-1,false,"unknown after reset");
Change(state.Observe(20,5), -1,-1,false,"first available after reset");
Change(state.Observe(19,4), 19,4,true,"combined partial critical fall");
Console.WriteLine($"Passed {checks} vitals state checks.");
