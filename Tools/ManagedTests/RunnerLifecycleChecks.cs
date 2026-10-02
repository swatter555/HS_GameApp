using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Api;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

// Compile separately from the game tests; negative fixtures are intentional runner checks.
internal static class LifecycleChecks
{
    internal static readonly List<string> Calls = new List<string>();
    private static int Main()
    {
        try { return Run(); }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static int Run()
    {
        CheckOutput();
        var runner = new NUnitTestAssemblyRunner(new DefaultTestAssemblyBuilder());
        runner.Load(typeof(LifecycleChecks).Assembly, new Dictionary<string, object> { ["NumberOfTestWorkers"] = 0 });
        var result = runner.Run(NUnit.Framework.Internal.TestListener.NULL, TestFilter.FromXml(
            TNode.FromXml("<filter><class>LifecycleSuccess</class></filter>")));
        var expected = new[] { "base once", "derived once", "base setup", "derived setup", "case 1",
            "derived teardown", "base teardown", "base setup", "derived setup", "case 2",
            "derived teardown", "base teardown", "derived end", "base end" };
        if (result.PassCount != 2 || !Calls.SequenceEqual(expected)) throw new Exception("Inherited NUnit lifecycle/order was not honored.");
        foreach (string fixture in new[] { "LifecycleSetupFailure", "LifecycleTeardownFailure" })
        {
            runner = new NUnitTestAssemblyRunner(new DefaultTestAssemblyBuilder());
            runner.Load(typeof(LifecycleChecks).Assembly, new Dictionary<string, object> { ["NumberOfTestWorkers"] = 0 });
            result = runner.Run(NUnit.Framework.Internal.TestListener.NULL, TestFilter.FromXml(
                TNode.FromXml("<filter><class>" + fixture + "</class></filter>")));
            if (result.FailCount != 1) throw new Exception("NUnit failed to propagate " + fixture);
        }
        if (LifecycleSetupFailure.BodyRan) throw new Exception("NUnit executed the body after failed setup.");
        Console.WriteLine("Runner lifecycle checks passed: inherited once/per-case ordering, parameter cases, setup failure, teardown failure.");
        return 0;
    }

    private static void CheckOutput()
    {
        string directory = Path.Combine(Path.GetTempPath(), "hs-nunit-output-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "events.txt");
        using (var listener = new NUnitFileListener(path, 16384))
        {
            var runner = new NUnitTestAssemblyRunner(new DefaultTestAssemblyBuilder());
            runner.Load(typeof(LifecycleChecks).Assembly, new Dictionary<string, object> { ["NumberOfTestWorkers"] = 0 });
            var result = runner.Run(listener, TestFilter.FromXml(
                TNode.FromXml("<filter><class>OutputChecks</class></filter>")));
            if (result.PassCount != 1 || result.FailCount != 1 || listener.Truncated || listener.OutputEvents < 1 || listener.OutputEvents > 16)
                throw new Exception("NUnit output callbacks were lost, recursive, or excessive.");
        }
        string report = File.ReadAllText(path);
        foreach (string marker in new[] { "output-once", "output-setup", "output-body", "output-error",
            "output-teardown", "output-end", "intentional-output-failure" })
            if (!report.Contains(marker)) throw new Exception("Missing output marker: " + marker);
        if (report.Split(new[] { "output-body" }, StringSplitOptions.None).Length != 2)
            throw new Exception("Test output was duplicated by the listener.");
        if (report.Split(new[] { "CASE OutputChecks." }, StringSplitOptions.None).Length != 3)
            throw new Exception("TestStarted callbacks did not use the file sink.");
        string capped = Path.Combine(directory, "bounded.txt");
        using (var listener = new NUnitFileListener(capped, 16))
        {
            listener.TestOutput(new TestOutput(new string('x', 128), "Progress", "bounded"));
            listener.TestOutput(new TestOutput("ignored-after-limit", "Progress", "bounded"));
            if (!listener.Truncated)
                throw new Exception("Listener output limit was not enforced.");
        }
        if (File.ReadAllText(capped).Length != 16) throw new Exception("Listener wrote past its output limit.");
        Console.WriteLine("Runner output checks passed: real NUnit lifecycle/body/error/failure callbacks, no recursion, bounded file sink.");
    }
}

[TestFixture]
public class OutputChecks
{
    [OneTimeSetUp] public void Once() => TestContext.Progress.WriteLine("output-once");
    [SetUp] public void Setup() => TestContext.Progress.WriteLine("output-setup");
    [TearDown] public void Teardown() => TestContext.Progress.WriteLine("output-teardown");
    [OneTimeTearDown] public void End() => TestContext.Progress.WriteLine("output-end");
    [Test] public void WritesOutput()
    {
        Console.WriteLine("output-body");
        Console.Error.WriteLine("output-error");
    }
    [Test] public void ReportsFailure() => Assert.Fail("intentional-output-failure");
}

public abstract class LifecycleBase
{
    [OneTimeSetUp] public void BaseOnce() => LifecycleChecks.Calls.Add("base once");
    [SetUp] public void BaseSetup() => LifecycleChecks.Calls.Add("base setup");
    [TearDown] public void BaseTeardown() => LifecycleChecks.Calls.Add("base teardown");
    [OneTimeTearDown] public void BaseEnd() => LifecycleChecks.Calls.Add("base end");
}

[TestFixture]
public class LifecycleSuccess : LifecycleBase
{
    [OneTimeSetUp] public void Once() => LifecycleChecks.Calls.Add("derived once");
    [SetUp] public void Setup() => LifecycleChecks.Calls.Add("derived setup");
    [TearDown] public void Teardown() => LifecycleChecks.Calls.Add("derived teardown");
    [OneTimeTearDown] public void End() => LifecycleChecks.Calls.Add("derived end");
    [TestCase(1), TestCase(2)] public void Case(int value) => LifecycleChecks.Calls.Add("case " + value);
}

[TestFixture]
public class LifecycleSetupFailure
{
    internal static bool BodyRan;
    [SetUp] public void Setup() => throw new InvalidOperationException("intentional setup failure");
    [Test] public void Body() { BodyRan = true; Assert.Fail("Body must not run after failed setup"); }
}

[TestFixture]
public class LifecycleTeardownFailure
{
    [Test] public void Body() { }
    [TearDown] public void Teardown() => throw new InvalidOperationException("intentional teardown failure");
}
