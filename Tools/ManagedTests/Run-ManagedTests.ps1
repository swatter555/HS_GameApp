param(
    [Parameter(Mandatory)][string]$UnityDataPath,
    [Parameter(Mandatory)][string]$ReferencePathsFile,
    [Parameter(Mandatory)][string]$MainAssembly,
    [Parameter(Mandatory)][string]$TestAssembly,
    [Parameter(Mandatory)][string]$SelectionFile,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [string]$DotNetPath = 'C:\Program Files\dotnet\dotnet.exe',
    [ValidateRange(1, 60)][int]$TimeoutSeconds = 30,
    [switch]$SharedProcess
)

# PowerShell 7. No installs, catalog bootstrap, skipped lifecycle, or Unity editor control.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
foreach ($taskInput in @($DotNetPath, $ReferencePathsFile, $MainAssembly, $TestAssembly, $SelectionFile)) {
    if (-not (Test-Path -LiteralPath $taskInput -PathType Leaf)) { throw "Missing input: $taskInput" }
}
$ReferencePathsFile = (Resolve-Path -LiteralPath $ReferencePathsFile).Path
$MainAssembly = (Resolve-Path -LiteralPath $MainAssembly).Path
$TestAssembly = (Resolve-Path -LiteralPath $TestAssembly).Path
$taskRuntime = @(& $DotNetPath --list-runtimes | ForEach-Object {
    if ($_ -match '^Microsoft.NETCore.App (8\.0\.\d+) ') { [version]$Matches[1] }
} | Sort-Object -Descending)
if ($LASTEXITCODE -ne 0 -or $taskRuntime.Count -eq 0) { throw 'An installed .NET 8 runtime is required. No installation attempted.' }
$taskCompilers = @(Get-ChildItem -LiteralPath (Join-Path $UnityDataPath 'DotNetSdk\sdk') -Filter csc.dll -Recurse)
if ($taskCompilers.Count -ne 1) { throw 'Expected one Unity-bundled Roslyn compiler; supply the exact Editor Data directory.' }
$taskNunit = @(Get-Content -LiteralPath $ReferencePathsFile | Where-Object {
    [IO.Path]::GetFileName($_) -eq 'nunit.framework.dll' -and (Test-Path -LiteralPath $_)
} | Select-Object -Unique)
if ($taskNunit.Count -ne 1) { throw 'Reference file must name exactly one existing NUnit assembly.' }
$taskSelections = @(Get-Content -LiteralPath $SelectionFile | ForEach-Object { $_.Trim() } |
    Where-Object { $_ -and -not $_.StartsWith('#') })
if ($taskSelections.Count -eq 0) { throw 'Selection file is empty.' }
if (Test-Path -LiteralPath $OutputDirectory) { throw 'Use a new output directory to preserve earlier evidence.' }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $OutputDirectory | Out-Null

function Invoke-BoundedDotNet([string[]]$TaskArguments, [string]$TaskLabel) {
    $taskStdout = Join-Path $OutputDirectory ($TaskLabel + '.stdout.txt')
    $taskStderr = Join-Path $OutputDirectory ($TaskLabel + '.stderr.txt')
    # Paths cannot contain double quotes on Windows; quote each argument for Start-Process.
    $taskQuoted = @($TaskArguments | ForEach-Object { '"' + $_ + '"' })
    $taskProcess = Start-Process -FilePath $DotNetPath -ArgumentList $taskQuoted -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput $taskStdout -RedirectStandardError $taskStderr
    $taskTimer = [Diagnostics.Stopwatch]::StartNew()
    try {
        while (-not $taskProcess.WaitForExit(100)) {
            if ($taskTimer.Elapsed.TotalSeconds -gt $TimeoutSeconds -or
                (Get-Item -LiteralPath $taskStdout).Length -gt 2MB -or
                (Get-Item -LiteralPath $taskStderr).Length -gt 2MB) {
                $taskProcess.Kill()
                throw "$TaskLabel stopped at the time/output limit; inspect preserved logs before any retry."
            }
        }
        $taskProcess.Refresh()
        $taskExit = $taskProcess.ExitCode
        "$TaskLabel exit=$taskExit runtime=$($taskRuntime[0])"
        if ($taskExit -ne 0) {
            Get-Content -LiteralPath $taskStdout, $taskStderr -TotalCount 15
            throw "$TaskLabel failed (exit $taskExit); stopped without running subsequent selections."
        }
    }
    finally { $taskProcess.Dispose() }
}

$taskFrameworkRefs = @(Get-ChildItem -LiteralPath (Join-Path $UnityDataPath 'NetStandard\ref\2.1.0') -Filter *.dll)
$taskFrameworkRefs += Get-ChildItem -LiteralPath (Join-Path $UnityDataPath 'NetStandard\compat\2.1.0\shims\netfx') -Filter *.dll
$taskRuntimeConfig = @{ runtimeOptions = @{ tfm = 'net8.0'; framework = @{
    name = 'Microsoft.NETCore.App'; version = $taskRuntime[0].ToString() } } } | ConvertTo-Json -Depth 4
Copy-Item -LiteralPath $taskNunit[0] -Destination (Join-Path $OutputDirectory 'nunit.framework.dll')
foreach ($taskTool in @('RunnerLifecycleChecks', 'ManagedNUnitRunner', 'FixtureLifecycleChecks')) {
    $taskResponse = @('-nologo', '-nostdlib+', '-target:exe', '-langversion:9.0',
        ('-out:"{0}/{1}.dll"' -f $OutputDirectory, $taskTool), ('-r:"{0}"' -f $taskNunit[0]))
    $taskResponse += $taskFrameworkRefs | ForEach-Object { '-r:"' + $_.FullName + '"' }
    $taskResponse += '"' + (Join-Path $PSScriptRoot ($taskTool + '.cs')) + '"'
    $taskResponse += '"' + (Join-Path $PSScriptRoot 'NUnitFileListener.cs') + '"'
    if ($taskTool -eq 'FixtureLifecycleChecks') {
        $taskResponse += '-main:FixtureLifecycleChecks'
        $taskResponse += '"' + (Join-Path $PSScriptRoot 'ManagedNUnitRunner.cs') + '"'
    }
    $taskRspPath = Join-Path $OutputDirectory ($taskTool + '.rsp')
    $taskResponse | Set-Content -LiteralPath $taskRspPath
    Invoke-BoundedDotNet @($taskCompilers[0].FullName, ('@' + $taskRspPath)) ($taskTool + '-compile')
    $taskRuntimeConfig | Set-Content -LiteralPath (Join-Path $OutputDirectory ($taskTool + '.runtimeconfig.json'))
}
Invoke-BoundedDotNet @((Join-Path $OutputDirectory 'RunnerLifecycleChecks.dll')) 'self-check'
foreach ($taskFixture in @('ActionEconomyRegressionTests', 'SigintActionTests')) {
    Invoke-BoundedDotNet @((Join-Path $OutputDirectory 'FixtureLifecycleChecks.dll'), $ReferencePathsFile,
        $MainAssembly, $TestAssembly, $taskFixture) ($taskFixture + '-lifecycle-check')
}
$taskGroups = if ($SharedProcess) { 1 } else { $taskSelections.Count }
for ($taskIndex = 0; $taskIndex -lt $taskGroups; $taskIndex++) {
    $taskLabel = 'selection-{0:D3}' -f ($taskIndex + 1)
    $taskSelected = if ($SharedProcess) { $taskSelections } else { @($taskSelections[$taskIndex]) }
    $taskSelectionPath = Join-Path $OutputDirectory ($taskLabel + '.txt')
    $taskSelected | Set-Content -LiteralPath $taskSelectionPath
    Invoke-BoundedDotNet @((Join-Path $OutputDirectory 'ManagedNUnitRunner.dll'), $ReferencePathsFile,
        $MainAssembly, $TestAssembly, $taskSelectionPath, (Join-Path $OutputDirectory $taskLabel)) $taskLabel
    Get-Content -LiteralPath (Join-Path $OutputDirectory ($taskLabel + '.stdout.txt')) |
        Where-Object { $_ -match '^(Catalog|RESULT|TOTAL)' }
}
