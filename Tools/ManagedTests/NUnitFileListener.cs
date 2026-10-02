using System;
using System.IO;
using System.Text;
using NUnit.Framework.Interfaces;

// NUnit redirects Console during execution. Every callback writes directly to this file,
// never to Console or TestContext (both can feed back into TestOutput).
internal sealed class NUnitFileListener : ITestListener, IDisposable
{
    private readonly StreamWriter _writer;
    private readonly int _characterLimit;
    private int _charactersWritten;
    private bool _writing;
    internal bool Truncated { get; private set; }
    internal int OutputEvents { get; private set; }

    internal NUnitFileListener(string path, int characterLimit = 1024 * 1024)
    {
        if (characterLimit < 1) throw new ArgumentOutOfRangeException(nameof(characterLimit));
        _characterLimit = characterLimit;
        _writer = new StreamWriter(new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read),
            new UTF8Encoding(false)) { AutoFlush = true };
    }

    public void TestStarted(ITest test)
    {
        if (!test.IsSuite) Write("CASE " + test.FullName + "\n");
    }

    public void TestOutput(TestOutput output)
    {
        OutputEvents++;
        Write(output.Text);
    }

    public void TestFinished(ITestResult result)
    {
        if (!result.Test.IsSuite && result.ResultState.Status != TestStatus.Passed)
            Write(result.ResultState + " " + result.FullName + "\n" + result.Message + "\n" + result.StackTrace + "\n");
    }

    private void Write(string text)
    {
        lock (_writer)
        {
            // Fail a future accidental redirected-output regression before it can overflow the stack.
            if (_writing) throw new InvalidOperationException("Recursive NUnit reporting detected.");
            _writing = true;
            try
            {
                if (string.IsNullOrEmpty(text)) return;
                int remaining = _characterLimit - _charactersWritten;
                int count = Math.Min(text.Length, remaining);
                if (count > 0) _writer.Write(text.Substring(0, count));
                _charactersWritten += count;
                if (count < text.Length) Truncated = true;
            }
            finally { _writing = false; }
        }
    }

    public void Dispose() => _writer.Dispose();
}
