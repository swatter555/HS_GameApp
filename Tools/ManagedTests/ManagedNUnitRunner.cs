using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework.Api;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

// Supplemental managed checks against existing Unity/NUnit references. No application bootstrap.
internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length != 5)
        {
            Console.Error.WriteLine("Usage: runner refs.txt Main.dll EditorTests.dll selection.txt output-directory");
            return 2;
        }
        try
        {
            ResolveAssemblies(args[0], args[1], args[2]);
            return Execution.Run(args);
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 2; }
    }

    internal static void ResolveAssemblies(string references, string main, string tests)
    {
        var paths = File.ReadAllLines(references).Where(File.Exists)
            .GroupBy(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        paths["Main"] = Path.GetFullPath(main);
        paths["EditorTests"] = Path.GetFullPath(tests);
        AppDomain.CurrentDomain.AssemblyResolve += (sender, request) =>
            paths.TryGetValue(new AssemblyName(request.Name).Name, out var path) ? Assembly.LoadFrom(path) : null;
    }
}

internal static class Execution
{
    internal static int Run(string[] args)
    {
        var main = Assembly.LoadFrom(args[1]);
        var tests = Assembly.LoadFrom(args[2]);
        var catalog = main.GetType("HammerAndSickle.Models.WeaponProfileDB");
        var app = main.GetType("HammerAndSickle.Services.AppService");
        var originalHandler = app.GetMethod("GetTestHandler").Invoke(null, null);
        Console.WriteLine("MANAGED NUnit lifecycle checks; no Unity native execution, Play or build.");
        bool initialized = (bool)catalog.GetProperty("IsInitialized").GetValue(null);
        Console.WriteLine("Catalog initialized at process entry: " + initialized);
        if (initialized) throw new InvalidOperationException("Expected a fresh process with an uninitialized catalog.");
        Directory.CreateDirectory(args[4]);
        using var report = new NUnitFileListener(Path.Combine(args[4], "events.txt"));
        int passed = 0, failed = 0, skipped = 0, index = 0;
        foreach (var selection in File.ReadAllLines(args[3]).Select(s => s.Trim())
            .Where(s => s.Length > 0 && !s.StartsWith("#", StringComparison.Ordinal)))
        {
            // Each selection is a whole fixture or an exact NUnit case name. NUnit owns lifecycle.
            var runner = new NUnitTestAssemblyRunner(new DefaultTestAssemblyBuilder());
            runner.Load(tests, new Dictionary<string, object> { ["NumberOfTestWorkers"] = 0 });
            bool isCase = selection.StartsWith("case:", StringComparison.Ordinal);
            string name = isCase ? selection.Substring(5) : "HammerAndSickle.Tests." + selection;
            var filter = TestFilter.FromXml(TNode.FromXml("<filter><" + (isCase ? "test" : "class") + ">" +
                System.Security.SecurityElement.Escape(name) + "</" + (isCase ? "test" : "class") + "></filter>"));
            int count = runner.CountTestCases(filter);
            if (count == 0) throw new InvalidOperationException("Selection discovered zero cases: " + selection);
            bool initializedBeforeRun = (bool)catalog.GetProperty("IsInitialized").GetValue(null);
            Console.WriteLine("Catalog initialized before selection: " + initializedBeforeRun);
            if (index == 0 && initializedBeforeRun)
                throw new InvalidOperationException("Test discovery initialized the catalog before fixture setup.");
            Console.WriteLine("RUN " + selection + " (" + count + " cases)");
            var handlerBefore = app.GetMethod("GetTestHandler").Invoke(null, null);
            var result = runner.Run(report, filter);
            File.WriteAllText(Path.Combine(args[4], (++index).ToString("D3") + ".xml"), result.ToXml(true).OuterXml);
            passed += result.PassCount; failed += result.FailCount;
            skipped += result.SkipCount + result.InconclusiveCount;
            Console.WriteLine("RESULT " + selection + ": " + result.PassCount + " passed, " + result.FailCount +
                " failed, " + (result.SkipCount + result.InconclusiveCount) + " skipped/inconclusive");
            if (result.ResultState.Status == TestStatus.Failed && result.FailCount == 0) failed++;
            if (!ReferenceEquals(handlerBefore, app.GetMethod("GetTestHandler").Invoke(null, null)))
            {
                Console.Error.WriteLine("Application error handler leaked from " + selection);
                failed++;
            }
        }
        if (!ReferenceEquals(originalHandler, app.GetMethod("GetTestHandler").Invoke(null, null)))
        {
            Console.Error.WriteLine("Application error handler leaked across the selected fixtures.");
            failed++;
        }
        Console.WriteLine("TOTAL " + passed + " passed, " + failed + " failed, " + skipped + " skipped/inconclusive");
        if (index == 0) throw new InvalidOperationException("Selection file contains no tests.");
        if (report.Truncated) throw new InvalidOperationException("Event report exceeded its output limit.");
        return failed > 0 ? 1 : skipped > 0 ? 2 : 0;
    }
}
