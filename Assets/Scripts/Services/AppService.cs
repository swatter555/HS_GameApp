using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace HammerAndSickle.Services
{
    /// <summary>
    /// Represents a single exception occurrence with context and timing information.
    /// </summary>
    public struct ExceptionEntry
    {
        public DateTime Timestamp;
        public string ClassName;
        public string MethodName;
        public string ExceptionType;
        public string Message;
        public string StackTrace;
    }

    /// <summary>
    /// Simple test handler for capturing exceptions and UI messages during unit tests.
    /// </summary>
    public class TestHandler
    {
        private readonly List<ExceptionEntry> _exceptions = new();
        private readonly List<string> _uiMessages = new();

        public IReadOnlyList<ExceptionEntry> Exceptions => _exceptions.AsReadOnly();
        public IReadOnlyList<string> UiMessages => _uiMessages.AsReadOnly();

        public int ExceptionCount => _exceptions.Count;
        public int UiMessageCount => _uiMessages.Count;
        public string LatestUiMessage => _uiMessages.Count > 0 ? _uiMessages[^1] : null;

        public void HandleException(string className, string methodName, Exception exception)
        {
            var entry = new ExceptionEntry
            {
                Timestamp = DateTime.Now,
                ClassName = className,
                MethodName = methodName,
                ExceptionType = exception.GetType().Name,
                Message = exception.Message,
                StackTrace = exception.StackTrace
            };
            _exceptions.Add(entry);
        }

        public void CaptureUiMessage(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                _uiMessages.Add(message);
        }

        public bool HasExceptionOfType<T>() where T : Exception
        {
            return _exceptions.Exists(e => e.ExceptionType == typeof(T).Name);
        }

        public void Clear()
        {
            _exceptions.Clear();
            _uiMessages.Clear();
        }
    }

    /// <summary>
    /// Static service providing centralized exception handling, UI messaging, and game directory management.
    /// Handles immediate exception logging to files and maintains comprehensive session data.
    /// </summary>
    public static class AppService
    {
        #region Constants

        private const string CLASS_NAME = "AppService";
        private const string MyGamesFolderName = "My Games";
        private const string MainAppFolderName = "Hammer and Sickle";
        private const int MaxUiMessages = 100;

        #endregion // Constants

        #region Private Fields

        // Cached paths
        private static string _mainAppPath;
        private static string _scenariosPath;
        private static string _mapPath;
        private static string _oobPath;
        private static string _aiiPath;
        private static string _brfPath;
        private static string _cmpPath;
        private static string _logsPath;
        private static string _exportsPath;
        private static string _backupsPath;

        // Session data
        private static readonly List<ExceptionEntry> _exceptions = new();
        private static readonly List<string> _uiMessages = new(MaxUiMessages);

        // Test support
        private static TestHandler _testHandler;

        // File writing lock for thread safety
        private static readonly object _fileLock = new();

        #endregion // Private Fields

        #region Properties

        /// <summary>
        /// Path to the main application folder: Documents/My Games/Hammer and Sickle/
        /// </summary>
        public static string MainAppPath => GetOrCreatePath(ref _mainAppPath,
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        MyGamesFolderName, MainAppFolderName));

        /// <summary>
        /// Path to scenario storage: Documents/My Games/Hammer and Sickle/scenarios/
        /// </summary>
        public static string ScenariosPath => GetOrCreatePath(ref _scenariosPath,
            Path.Combine(MainAppPath, "scenario"));

        /// <summary>
        /// Gets the file system path to the directory where map files are stored.
        /// </summary>
        public static string MapPath => GetOrCreatePath(ref _mapPath,
            Path.Combine(MainAppPath, "map"));

        /// <summary>
        /// Gets the file system path to the "Order of Battle" (OOB) directory, creating it if it does not already exist.
        /// </summary>
        public static string OobPath => GetOrCreatePath(ref _oobPath,
            Path.Combine(MainAppPath, "oob"));

        /// <summary>
        /// Gets the file system path to the "aii" directory within the main application path.
        /// </summary>
        public static string AiiPath => GetOrCreatePath(ref _aiiPath,
            Path.Combine(MainAppPath, "aii"));

        /// <summary>
        /// Gets the file system path to the "brf" directory within the main application path.
        /// </summary>
        public static string BrfPath => GetOrCreatePath(ref _brfPath,
            Path.Combine(MainAppPath, "brf"));

        /// <summary>
        /// Gets the path to the "cmp" directory, creating it if it does not already exist.
        /// </summary>
        public static string CmpPath => GetOrCreatePath(ref _cmpPath,
            Path.Combine(MainAppPath, "cmp"));

        /// <summary>
        /// Path to log files: Documents/My Games/Hammer and Sickle/logs/
        /// </summary>
        public static string LogsPath => GetOrCreatePath(ref _logsPath,
            Path.Combine(MainAppPath, "logs"));

        /// <summary>
        /// Path to exported content: Documents/My Games/Hammer and Sickle/exports/
        /// </summary>
        public static string ExportsPath => GetOrCreatePath(ref _exportsPath,
            Path.Combine(MainAppPath, "exports"));

        /// <summary>
        /// Path to backup files: Documents/My Games/Hammer and Sickle/backups/
        /// </summary>
        public static string BackupsPath => GetOrCreatePath(ref _backupsPath,
            Path.Combine(MainAppPath, "backups"));

        /// <summary>
        /// Most recent UI message captured. Returns null if no message exists.
        /// </summary>
        public static string LatestUiMessage
        {
            get
            {
                if (_testHandler != null)
                    return _testHandler.LatestUiMessage;

                return _uiMessages.Count > 0 ? _uiMessages[^1] : null;
            }
        }

        /// <summary>
        /// Total number of exceptions captured this session.
        /// </summary>
        public static int ExceptionCount => _testHandler?.ExceptionCount ?? _exceptions.Count;

        #endregion // Properties

        #region Exception Handling

        /// <summary>
        /// Handles an exception by logging it immediately to file and storing for session reporting.
        /// </summary>
        /// <param name="className">Name of the class where exception occurred</param>
        /// <param name="methodName">Name of the method where exception occurred</param>
        /// <param name="exception">The exception that occurred</param>
        public static void HandleException(string className, string methodName, Exception exception)
        {
            if (exception == null) return;

            var entry = new ExceptionEntry
            {
                Timestamp = DateTime.Now,
                ClassName = className ?? "Unknown",
                MethodName = methodName ?? "Unknown",
                ExceptionType = exception.GetType().Name,
                Message = exception.Message ?? "No message",
                StackTrace = exception.StackTrace ?? "No stack trace"
            };

            // Use test handler if set
            if (_testHandler != null)
            {
                _testHandler.HandleException(className, methodName, exception);
                return;
            }

            // Add to session list
            _exceptions.Add(entry);

            // Write immediately to file
            WriteExceptionToFile(entry);

            // Also log to Unity console
            Debug.LogError($"[{className}.{methodName}] {exception.GetType().Name}: {exception.Message}");
        }

        /// <summary>
        /// Writes complete session exception log to a timestamped file.
        /// </summary>
        public static void WriteSessionLog()
        {
            if (_testHandler != null) return;
            if (_exceptions.Count == 0) return;

            try
            {
                string sessionFileName = $"session_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log";
                string sessionFilePath = Path.Combine(LogsPath, sessionFileName);

                var sb = new StringBuilder();
                sb.AppendLine($"=== Hammer & Sickle Session Log ===");
                sb.AppendLine($"Session Start: {_exceptions[0].Timestamp:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Total Exceptions: {_exceptions.Count}");
                sb.AppendLine($"Unity Version: {Application.unityVersion}");
                sb.AppendLine($"Platform: {Application.platform}");
                sb.AppendLine();

                foreach (var entry in _exceptions)
                {
                    sb.AppendLine($"[{entry.Timestamp:HH:mm:ss}] {entry.ClassName}.{entry.MethodName}");
                    sb.AppendLine($"  Type: {entry.ExceptionType}");
                    sb.AppendLine($"  Message: {entry.Message}");
                    if (!string.IsNullOrEmpty(entry.StackTrace))
                    {
                        sb.AppendLine($"  Stack Trace:");
                        sb.AppendLine($"    {entry.StackTrace.Replace("\n", "\n    ")}");
                    }
                    sb.AppendLine();
                }

                lock (_fileLock)
                {
                    File.WriteAllText(sessionFilePath, sb.ToString());
                }

                Debug.Log($"Session log written: {sessionFilePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"{CLASS_NAME}.WriteSessionLog: Failed to write session log: {ex.Message}");
            }
        }

        /// <summary>
        /// Writes a single exception entry to the error log file.
        /// </summary>
        /// <param name="entry"></param>
        private static void WriteExceptionToFile(ExceptionEntry entry)
        {
            try
            {
                string errorFilePath = Path.Combine(LogsPath, "errors.log");
                string logLine = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] {entry.ClassName}.{entry.MethodName} - {entry.ExceptionType}: {entry.Message}\n";

                lock (_fileLock)
                {
                    File.AppendAllText(errorFilePath, logLine);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{CLASS_NAME}.WriteExceptionToFile: Failed to write to error log: {ex.Message}");
            }
        }

        #endregion // Exception Handling

        #region UI Message Handling

        /// <summary>
        /// Captures a message intended for the in-game UI.
        /// </summary>
        /// <param name="message">Message to display to the player</param>
        public static void CaptureUiMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            // Use test handler if set
            if (_testHandler != null)
            {
                _testHandler.CaptureUiMessage(message);
                return;
            }

            // Respect size cap – drop oldest when full
            if (_uiMessages.Count >= MaxUiMessages)
                _uiMessages.RemoveAt(0);

            _uiMessages.Add(message);
        }

        /// <summary>
        /// Returns a snapshot of all buffered UI messages (oldest to newest).
        /// </summary>
        public static IReadOnlyList<string> GetUiMessageLog()
        {
            if (_testHandler != null)
                return _testHandler.UiMessages;

            return _uiMessages.AsReadOnly();
        }

        #endregion // UI Message Handling

        #region Directory Management

        private static string GetOrCreatePath(ref string cachedPath, string fullPath)
        {
            if (!string.IsNullOrEmpty(cachedPath))
                return cachedPath;

            try
            {
                if (!Directory.Exists(fullPath))
                    Directory.CreateDirectory(fullPath);

                cachedPath = fullPath;
                return cachedPath;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{CLASS_NAME}.GetOrCreatePath: Failed to create directory {fullPath}: {ex.Message}");
                return fullPath; // Return path anyway, let caller handle missing directory
            }
        }

        #endregion // Directory Management

        #region Test Support

        /// <summary>
        /// Sets a test handler for unit testing. Set to null to restore default behavior.
        /// </summary>
        public static void SetTestHandler(TestHandler testHandler)
        {
            _testHandler = testHandler;
        }

        /// <summary>
        /// Resets test handler to default behavior.
        /// </summary>
        public static void ResetTestHandler()
        {
            _testHandler = null;
        }

        /// <summary>
        /// Gets the current test handler (for test verification).
        /// </summary>
        public static TestHandler GetTestHandler()
        {
            return _testHandler;
        }

        #endregion // Test Support

        #region Cleanup

        /// <summary>
        /// Shuts down Unity regardless of game state data safety. Performs necessary cleanup.
        /// Use with caution - may lead to data loss if game state is unsaved.
        /// </summary>
        public static void UnityQuit_DataUnsafe()
        {
            // Perform cleanup tasks.
            Shutdown();

            // Quit the application.
            Application.Quit();
        }

        /// <summary>
        /// Clears all session data and writes final session log.
        /// </summary>
        private static void Shutdown()
        {
            try
            {
                WriteSessionLog();
                _exceptions.Clear();
                _uiMessages.Clear();
                Debug.Log($"{CLASS_NAME}: Shutdown completed.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"{CLASS_NAME}.Shutdown: Error during shutdown: {ex.Message}");
            }
        }

        #endregion // Cleanup
    }
}