# Managed fixture verification

Unity Test Runner remains the authority for native EditorTests. The supplemental runner in [Tools/ManagedTests](../Tools/ManagedTests/) executes the existing NUnit lifecycle outside Unity, without starting the application or initializing its catalog. It must not omit setup/teardown to make a Unity-dependent fixture pass.

## Running the supplemental checks

Use PowerShell 7 and an already installed .NET 8 runtime. Supply compiled `Main.dll` and `EditorTests.dll` from the source being verified, a newline-separated list of their existing dependency DLL paths, the exact installed Unity Editor `Data` directory, and a selection file. Compilation of the game assemblies is a separate step: use their Unity compiler responses with output/reference-output paths redirected to isolated temporary directories, and ensure EditorTests references that compiled Main. Do not replace `Library/ScriptAssemblies`.

Each selection line is a complete fixture name (for example `ActionEconomyRegressionTests`) or `case:` followed by an exact NUnit full test-case name, including parameter values. Blank lines and `#` comments are ignored. The default launches each selection in a fresh process. To verify every case independently, list the individual case names from NUnit XML. `-SharedProcess` is an explicit additional order-dependence check; a shared-process pass does not establish cold-start independence.

```powershell
& .\Tools\ManagedTests\Run-ManagedTests.ps1 `
    -UnityDataPath 'C:\Unity\Editors\6000.6.3f1\Editor\Data' `
    -ReferencePathsFile 'C:\path\to\refs.txt' `
    -MainAssembly 'C:\path\to\Main.dll' `
    -TestAssembly 'C:\path\to\EditorTests.dll' `
    -SelectionFile 'C:\path\to\selection.txt' `
    -OutputDirectory 'C:\path\to\new-results' `
    -TimeoutSeconds 15
```

The script compiles only the harness, using the installed Unity compiler/framework references and the referenced NUnit assembly. It verifies pure NUnit lifecycle/output handling first, then checks the actual action-economy fixture's error guard and restoration in a separate process. It never installs a runtime or invokes standalone Mono. Use a new output directory for every run; preserve old results and crash logs.

The runner rejects an initialized catalog at process entry or after first-selection discovery, zero-case/empty selections, handler leaks, test failures, skips/inconclusive results, and truncated reporting. NUnit owns inherited one-time and per-case callbacks and parameterized cases. The fixture itself owns its required initialization. Native services/assets remain unsupported outside Unity; failures are recorded, never skipped or converted to success.

Every NUnit listener callback writes directly to a file, not `Console` or `TestContext`, which NUnit redirects. Output is capped at 1 MiB of characters with a non-recursion guard. The PowerShell watchdog bounds each subprocess to the requested timeout (maximum 60 seconds), checks stdout/stderr against 2 MiB while running, and stops on any nonzero exit or limit. XML and diagnostic files are retained. This is supplemental verification, not an Editor run, Play check, or player build.

## Action-economy fixture repair, 2026-10-02

The native run at 19:55 UTC contained **1,039 tests: 1,021 passed, 18 failed**, all failures in `ActionEconomyRegressionTests`. Its unit factories used catalog weapon identifiers before `WeaponProfileDB.Initialize()`. Missing profiles caused movement initialization errors and invalid transition state. The former managed probe initialized shared catalogs and omitted fixture lifecycle, masking this fixture defect.

`ActionEconomyRegressionTests` now initializes the real catalog in setup, captures unexpected application errors, and restores the previous error handler on successful teardown, failed teardown, or failed setup. Test bodies and gameplay assertions are unchanged. The catalog is treated as the existing idempotent, read-only initialized database; no production reset API or game bootstrap is introduced.

An inherited runner initially crashed under standalone Unity Mono. That path was not retried. Its installed .NET 8 attempt exposed recursive listener output (`TestOutput -> Console.Write -> TestOutput`), producing a stack overflow before any case body. Work stopped and the crash was reported; resumption was explicitly authorized after correcting the output path. The direct file listener and bounded self-checks address the demonstrated recursion. This does not establish the cause of the earlier native memory-read dialog or a Unity Editor crash. No broad crash investigation or runtime installation was performed.

Verified on installed **.NET 8.0.31**, using local **Unity 6000.6.3f1** compiler/references:

| Check | Actual result |
|---|---|
| Main and EditorTests compilation | Passed, no compiler diagnostics |
| Pre-repair assemblies through the corrected NUnit runner, cold | 2 passed / 18 failed; failed names exactly match the native XML |
| Pure NUnit output/lifecycle checks | Passed: inherited one-time/per-case order, parameter cases, setup failure without body execution, teardown failure, output/failure callbacks and output cap |
| Actual fixture lifecycle | Passed: cold catalog initialization, deliberately captured error fails teardown, previous handler restored, clean repeat |
| Complete fixture, cold process | 20 passed / 0 failed |
| Each of its 20 cases in its own cold process | 20 passed / 0 failed; all 18 native failed names accounted for |
| Reverse case order in one cold-start process | 20 passed / 0 failed |
| CombatMath, CombatEngine, AirCombatEngine, CombatOracle, AIPerception, each cold | 8 + 12 + 11 + 15 + 11 = 57 passed / 0 failed |
| Action fixture before and after those five broader suites | 97 passed / 0 failed, including the repeated 20 cases |

These repetitions establish different isolation/order checks, not additional unique tests. There are **77 unique passing game cases** in the successful managed selection.

### Separately exposed dependency

A cold-process `SigintActionTests` run produced **11 passed / 16 failed**. Its weapon-dependent unit factories also rely on an already initialized catalog. The missing profile path reached Unity's native error logger, unavailable under .NET (`ECall methods must be packaged into a system module`). The saved full native run had passed all 27 SIGINT cases after other fixtures initialized the catalog. This is an open fixture-isolation finding, not a new gameplay failure or a passing cold-start check. No SIGINT assertions or source have been changed in this repair. The Chinese/NATO roster fixtures inherit native manager setup and were not rerun by bypassing it; their saved native results are 27/27 and 35/35.

### Scope and remaining acceptance

Only the action-economy fixture, supplemental harness and documentation belong to this repair. Existing icon/prefab-source changes, user scenes/prefabs, metadata, upgrade settings/packages and historical files remain outside it. Before documentation edits, all **4,064 unrelated baseline files** matched their saved hashes.

Committed pins remain **Unity 6000.2.6f2 / URP 17.2.0**; the local reference environment is **6000.6.3f1 / URP 17.6.0**. Compiling exact committed C# sources with local references does not establish a native clean build at committed pins. Native fixture/full EditorTests reruns after recompilation remain pending, as do fresh Khost, Console/Play/UI and build acceptance. Robert owns air-button wiring; no prefab or scene authoring was performed.

Evidence: `C:/Users/coder/AppData/Local/Temp/hs-fixture-repair-20261002`. Original XML and earlier probes are retained at its root; `astra-resume` contains the .NET crash, corrected runs, compiler responses, per-case XML, order runs, native XML capture and baseline hash comparison.
