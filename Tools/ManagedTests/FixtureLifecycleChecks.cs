using System;
using System.Reflection;
using NUnit.Framework;

// Separate process: exercise the actual fixture's error guard and handler restoration.
// This intentionally runs no game test bodies and cannot initialize another runner's catalog.
internal static class FixtureLifecycleChecks
{
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length < 3 || args.Length > 4)
                throw new ArgumentException("Usage: fixture-check refs.txt Main.dll EditorTests.dll [fixture-name]");
            string fixtureName = args.Length == 4 ? args[3] : "ActionEconomyRegressionTests";
            Program.ResolveAssemblies(args[0], args[1], args[2]);
            var main = Assembly.LoadFrom(args[1]);
            var tests = Assembly.LoadFrom(args[2]);
            var catalog = main.GetType("HammerAndSickle.Models.WeaponProfileDB", true);
            var app = main.GetType("HammerAndSickle.Services.AppService", true);
            var handlerType = main.GetType("HammerAndSickle.Services.TestHandler", true);
            var getHandler = app.GetMethod("GetTestHandler");
            var setHandler = app.GetMethod("SetTestHandler");
            if ((bool)catalog.GetProperty("IsInitialized").GetValue(null))
                throw new Exception("Fixture check must start with an uninitialized catalog.");
            var previous = getHandler.Invoke(null, null);
            var sentinel = Activator.CreateInstance(handlerType);
            try
            {
                setHandler.Invoke(null, new[] { sentinel });
                var type = tests.GetType("HammerAndSickle.Tests." + fixtureName, true);
                var fixture = Activator.CreateInstance(type);
                type.GetMethod("SetUp").Invoke(fixture, null);
                var ownedHandler = getHandler.Invoke(null, null);
                if (ReferenceEquals(ownedHandler, sentinel) || !(bool)catalog.GetProperty("IsInitialized").GetValue(null))
                    throw new Exception("Fixture did not own its catalog setup and error handler.");
                handlerType.GetMethod("HandleException").Invoke(ownedHandler, new object[] {
                    "FixtureLifecycleChecks", "IntentionalError", new InvalidOperationException("intentional captured error") });
                bool rejected = false;
                try { type.GetMethod("TearDown").Invoke(fixture, null); }
                catch (TargetInvocationException error) when (error.InnerException is AssertionException) { rejected = true; }
                if (!rejected || !ReferenceEquals(sentinel, getHandler.Invoke(null, null)))
                    throw new Exception("Unexpected errors must fail teardown and still restore the previous handler.");
                type.GetMethod("SetUp").Invoke(fixture, null);
                type.GetMethod("TearDown").Invoke(fixture, null);
                if (!ReferenceEquals(sentinel, getHandler.Invoke(null, null)))
                    throw new Exception("Clean repeated setup/teardown leaked the handler.");
            }
            finally { setHandler.Invoke(null, new[] { previous }); }
            Console.WriteLine(fixtureName + " lifecycle checks passed: cold catalog, captured-error failure, previous-handler restoration, clean repeat.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
