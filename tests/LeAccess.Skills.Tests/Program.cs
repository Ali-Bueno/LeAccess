int checks = 0;
var failures = new List<string>();
void Check(bool condition, string message)
{
    checks++;
    if (!condition) failures.Add(message);
}

BindingTests.Run(Check);
LearningTests.Run(Check);
SelectionTests.Run(Check);
TooltipDetailsTests.Run(Check);
TreeNavigationTests.Run(Check);
if (failures.Count != 0) throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
Console.WriteLine($"Skill tests passed: {checks} checks.");
