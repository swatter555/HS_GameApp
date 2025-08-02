using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace HammerAndSickle.Services
{
    /// <summary>
    /// Event arguments for error notifications.
    /// </summary>
    public class ErrorEventArgs : EventArgs
    {
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public DateTime Timestamp { get; set; }
        public string SystemInfo { get; set; }
        public bool IsFatal { get; set; }
    }

    /// <summary>
    /// Defines the severity levels for exceptions to determine appropriate response.
    /// </summary>
    public enum ExceptionSeverity
    {
        /// <summary>
        /// Low-impact exceptions that don't affect core functionality (validation errors, etc.)
        /// </summary>
        Minor,

        /// <summary>
        /// Moderate exceptions that may impact specific features but don't require shutdown
        /// </summary>
        Moderate,

        /// <summary>
        /// Critical exceptions that require immediate attention and may trigger save/quit
        /// </summary>
        Critical,

        /// <summary>
        /// Fatal exceptions that require immediate shutdown
        /// </summary>
        Fatal
    }

    /// <summary>
    /// Configuration settings for the AppService.
    /// </summary>
    [Serializable]
    public class AppServiceConfig
    {
        public bool SaveLogsOnQuit = true;
        public int MaxDebugLogs = 50;
        public int MaxWarningLogs = 20;
        public int MaxErrorLogs = 20;
        public string LogFilePrefix = "UnityLogs";
        public bool IncludeSystemInfo = true;
    }

    /// <summary>
    /// Configuration for exception severity handling.
    /// </summary>
    [Serializable]
    public class ExceptionSeverityConfig
    {
        public bool SaveOnCritical = true;
        public bool QuitOnCritical = true;
        public bool SaveOnFatal = true;
        public bool QuitOnFatal = true;
        public bool ShowErrorDialogs = true;
    }

    /// <summary>
    /// Represents a single log entry with critical metadata.
    /// </summary>
    public struct LogEntry
    {
        public DateTime Timestamp;
        public string Message;
        public string StackTrace;
        public LogType UnityLogType;
        public string SystemInfo;
    }

    /// <summary>
    /// Simple test handler for capturing exceptions and UI messages during unit tests.
    /// </summary>
    public class TestHandler
    {
        private readonly List<(string ClassName, string MethodName, Exception Exception, ExceptionSeverity Severity)> _exceptions = new();
        private readonly List<string> _uiMessages = new();

        public IReadOnlyList<(string ClassName, string MethodName, Exception Exception, ExceptionSeverity Severity)> Exceptions => _exceptions.AsReadOnly();
        public IReadOnlyList<string> UiMessages => _uiMessages.AsReadOnly();

        public int ExceptionCount => _exceptions.Count;
        public int UiMessageCount => _uiMessages.Count;
        public string LatestUiMessage => _uiMessages.LastOrDefault();

        public void HandleException(string className, string methodName, Exception exception, ExceptionSeverity severity = ExceptionSeverity.Minor)
        {
            _exceptions.Add((className, methodName, exception, severity));
        }

        public void CaptureUiMessage(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                _uiMessages.Add(message);
        }

        public bool HasExceptionOfType<T>() where T : Exception
        {
            return _exceptions.Any(e => e.Exception is T);
        }

        public bool HasExceptionOfSeverity(ExceptionSeverity severity)
        {
            return _exceptions.Any(e => e.Severity == severity);
        }

        public T GetMostRecentExceptionOfType<T>() where T : Exception
        {
            return _exceptions.Where(e => e.Exception is T).LastOrDefault().Exception as T;
        }

        public void Clear()
        {
            _exceptions.Clear();
            _uiMessages.Clear();
        }
    }

    /// <summary>
    /// Static AppService providing centralized error handling, logging, UI messaging,
    /// and application lifecycle management for Unity applications.
    /// </summary>
    public static class AppService
    {
        #region Constants

        private const string CLASS_NAME = "AppService";

        // Folder name constants
        public const string MyGamesFolderName = "My Games";
        public const string MainAppFolderName = "Hammer and Sickle";
        public const string DebugDataFolderName = "Debug";
        public const string GameDataFolderName = "Game Data";
        public const string AtlasStoragePath = "Assets/Graphics/Atlases";

        // UI Message constants
        private const int MaxUiMessages = 100;

        #endregion // Constants


        #region Configuration and Events

        /// <summary>
        /// Configuration settings for the service.
        /// </summary>
        public static AppServiceConfig Config { get; private set; } = new();

        /// <summary>
        /// Severity-based exception handling configuration.
        /// </summary>
        public static ExceptionSeverityConfig SeverityConfig { get; private set; } = new();

        /// <summary>
        /// Event raised when an error occurs.
        /// </summary>
        public static event EventHandler<ErrorEventArgs> OnErrorOccurred;

        #endregion // Configuration and Events


        #region Properties

        // Path Properties
        public static string MyGamesPath { get; private set; }
        public static string MainAppFolderPath { get; private set; }
        public static string DebugDataFolderPath { get; private set; }
        public static string ScenarioStorageFolderPath { get; private set; }

        // State Properties
        public static bool IsInitialized { get; private set; }

        /// <summary>
        /// Most recent UI message captured. Returns null if no message exists.
        /// </summary>
        public static string LatestUiMessage
        {
            get
            {
                if (_testHandler != null)
                    return _testHandler.LatestUiMessage;

                _uiMsgLock.EnterReadLock();
                try
                {
                    return _uiMessages.Count > 0 ? _uiMessages.Last() : null;
                }
                finally { _uiMsgLock.ExitReadLock(); }
            }
        }

        #endregion // Properties


        #region Private Fields

        // Thread synchronization for log access
        private static readonly ReaderWriterLockSlim _logLock = new(LockRecursionPolicy.SupportsRecursion);
        private static readonly ReaderWriterLockSlim _uiMsgLock = new(LockRecursionPolicy.SupportsRecursion);

        // Circular buffers for different log types
        private static readonly Queue<LogEntry> _debugLogs = new();
        private static readonly Queue<LogEntry> _warningLogs = new();
        private static readonly Queue<LogEntry> _errorLogs = new();

        // UI message buffer
        private static readonly Queue<string> _uiMessages = new(MaxUiMessages);

        // Lifecycle management
        private static bool _isDisposed = false;

        // Test handler - simple replacement for interface system
        private static TestHandler _testHandler;

        #endregion // Private Fields


        #region Initialization and Lifecycle

        /// <summary>
        /// Automatically initializes AppService when Unity starts up.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes the AppService with optional custom configuration.
        /// </summary>
        public static void Initialize(AppServiceConfig config = null, ExceptionSeverityConfig severityConfig = null)
        {
            if (IsInitialized) return;

            try
            {
                Config = config ?? new AppServiceConfig();
                SeverityConfig = severityConfig ?? new ExceptionSeverityConfig();

                SetupFolderPaths();
                ValidateConfiguration();
                CreateLogDirectories();
                RegisterEventHandlers();

                // Seed the random number generator
                int seed = System.DateTime.Now.Millisecond;
                UnityEngine.Random.InitState(seed);

                IsInitialized = true;
                Debug.Log($"{CLASS_NAME}: Service initialized successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"{CLASS_NAME}.Initialize: Failed to initialize: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Shuts down the AppService and releases all resources.
        /// </summary>
        public static void Shutdown()
        {
            if (_isDisposed) return;

            try
            {
                UnregisterEventHandlers();

                if (Config.SaveLogsOnQuit)
                {
                    FlushLogsSync();
                }

                ClearAllData();
                _isDisposed = true;
                IsInitialized = false;

                Debug.Log($"{CLASS_NAME}: Service shutdown completed.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"{CLASS_NAME}.Shutdown: Error during shutdown: {ex.Message}");
            }
        }

        private static void SetupFolderPaths()
        {
            try
            {
                MyGamesPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    MyGamesFolderName);

                MainAppFolderPath = Path.Combine(MyGamesPath, MainAppFolderName);
                DebugDataFolderPath = Path.Combine(MainAppFolderPath, DebugDataFolderName);
                ScenarioStorageFolderPath = Path.Combine(MainAppFolderPath, GameDataFolderName);

                // Create directories if they don't exist
                Directory.CreateDirectory(MainAppFolderPath);
                Directory.CreateDirectory(DebugDataFolderPath);
                Directory.CreateDirectory(ScenarioStorageFolderPath);
            }
            catch (Exception ex)
            {
                throw new DirectoryNotFoundException(
                    $"{CLASS_NAME}.SetupFolderPaths: Failed to setup application folders: {ex.Message}");
            }
        }

        private static void RegisterEventHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
            Application.logMessageReceived += CaptureUnityLogs;
            Application.quitting += OnApplicationQuitting;
        }

        private static void UnregisterEventHandlers()
        {
            if (_isDisposed) return;

            AppDomain.CurrentDomain.UnhandledException -= HandleUnhandledException;
            Application.logMessageReceived -= CaptureUnityLogs;
            Application.quitting -= OnApplicationQuitting;
        }

        private static void OnApplicationQuitting()
        {
            Shutdown();
        }

        private static void ValidateConfiguration()
        {
            Config.MaxDebugLogs = Mathf.Max(1, Config.MaxDebugLogs);
            Config.MaxWarningLogs = Mathf.Max(1, Config.MaxWarningLogs);
            Config.MaxErrorLogs = Mathf.Max(1, Config.MaxErrorLogs);
        }

        private static void CreateLogDirectories()
        {
            try
            {
                if (!Directory.Exists(DebugDataFolderPath))
                {
                    Directory.CreateDirectory(DebugDataFolderPath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{CLASS_NAME}.CreateLogDirectories: Error creating log directories: {ex.Message}");
            }
        }

        #endregion // Initialization and Lifecycle


        #region Error Handling

        /// <summary>
        /// Handles an exception with Minor severity (backward compatibility).
        /// </summary>
        public static void HandleException(string className, string methodName, Exception exception)
        {
            var severity = DetermineExceptionSeverity(exception);
            HandleException(className, methodName, exception, severity);
        }

        /// <summary>
        /// Handles an exception with specified severity level.
        /// </summary>
        public static void HandleException(string className, string methodName, Exception exception, ExceptionSeverity severity)
        {
            if (_isDisposed) return;

            // Use test handler if set
            if (_testHandler != null)
            {
                _testHandler.HandleException(className, methodName, exception, severity);
                return;
            }

            string message = $"Exception in {className}.{methodName}: {exception.Message}";

            // Log with appropriate level based on severity
            switch (severity)
            {
                case ExceptionSeverity.Minor:
                    Debug.LogWarning($"[{severity}] {message}");
                    break;
                case ExceptionSeverity.Moderate:
                    Debug.LogWarning($"[{severity}] {message}\n{exception.StackTrace}");
                    break;
                case ExceptionSeverity.Critical:
                case ExceptionSeverity.Fatal:
                    Debug.LogError($"[{severity}] {message}\n{exception.StackTrace}");
                    break;
            }

            // Raise error event for UI notification (if configured)
            if (severity >= ExceptionSeverity.Moderate && SeverityConfig.ShowErrorDialogs)
            {
                RaiseErrorEvent(new ErrorEventArgs
                {
                    Message = message,
                    StackTrace = exception.StackTrace,
                    Timestamp = DateTime.UtcNow,
                    SystemInfo = Config.IncludeSystemInfo ? GetSystemInfo() : string.Empty,
                    IsFatal = severity == ExceptionSeverity.Fatal
                });
            }

            // Handle critical/fatal exceptions
            switch (severity)
            {
                case ExceptionSeverity.Critical:
                    if (SeverityConfig.SaveOnCritical && SeverityConfig.QuitOnCritical)
                    {
                        SaveAndQuit();
                    }
                    else if (SeverityConfig.SaveOnCritical)
                    {
                        SaveGameState();
                    }
                    break;

                case ExceptionSeverity.Fatal:
                    if (SeverityConfig.SaveOnFatal && SeverityConfig.QuitOnFatal)
                    {
                        SaveAndQuit();
                    }
                    else if (SeverityConfig.SaveOnFatal)
                    {
                        SaveGameState();
                    }
                    break;
            }
        }

        private static void HandleUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (_isDisposed) return;

            if (e.ExceptionObject is Exception exception)
            {
                HandleException("UnhandledException", "Global", exception, ExceptionSeverity.Fatal);
            }
            else
            {
                string message = $"Non-Exception unhandled error: {e.ExceptionObject}";
                Debug.LogError(message);
                RaiseErrorEvent(new ErrorEventArgs
                {
                    Message = message,
                    Timestamp = DateTime.UtcNow,
                    SystemInfo = Config.IncludeSystemInfo ? GetSystemInfo() : string.Empty,
                    IsFatal = true
                });
            }
        }

        private static ExceptionSeverity DetermineExceptionSeverity(Exception exception)
        {
            return exception switch
            {
                ArgumentNullException => ExceptionSeverity.Minor,
                ArgumentOutOfRangeException => ExceptionSeverity.Minor,
                ArgumentException => ExceptionSeverity.Minor,
                InvalidOperationException => ExceptionSeverity.Moderate,
                NotSupportedException => ExceptionSeverity.Moderate,
                NullReferenceException => ExceptionSeverity.Moderate,
                IndexOutOfRangeException => ExceptionSeverity.Minor,
                IOException => ExceptionSeverity.Moderate,
                System.Runtime.Serialization.SerializationException => ExceptionSeverity.Moderate,
                TimeoutException => ExceptionSeverity.Moderate,
                UnauthorizedAccessException => ExceptionSeverity.Critical,
                System.Security.SecurityException => ExceptionSeverity.Critical,
                OutOfMemoryException => ExceptionSeverity.Fatal,
                StackOverflowException => ExceptionSeverity.Fatal,
                AccessViolationException => ExceptionSeverity.Fatal,
                _ => ExceptionSeverity.Moderate
            };
        }

        #endregion // Error Handling


        #region UI Message Handling

        /// <summary>
        /// Captures a message intended for the in-game UI.
        /// </summary>
        public static void CaptureUiMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            // Use test handler if set
            if (_testHandler != null)
            {
                _testHandler.CaptureUiMessage(message);
                return;
            }

            _uiMsgLock.EnterWriteLock();
            try
            {
                // Respect size cap – drop oldest when full
                if (_uiMessages.Count == MaxUiMessages)
                    _uiMessages.Dequeue();

                _uiMessages.Enqueue(message);
            }
            finally { _uiMsgLock.ExitWriteLock(); }
        }

        /// <summary>
        /// Returns a snapshot of all buffered UI messages (oldest → newest).
        /// </summary>
        public static IReadOnlyList<string> GetUiMessageLog()
        {
            // Use test handler if set
            if (_testHandler != null)
            {
                return _testHandler.UiMessages;
            }

            _uiMsgLock.EnterReadLock();
            try { return _uiMessages.ToArray(); }
            finally { _uiMsgLock.ExitReadLock(); }
        }

        #endregion // UI Message Handling


        #region Log Management

        private static void CaptureUnityLogs(string message, string stackTrace, LogType type)
        {
            if (_isDisposed) return;

            var entry = new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Message = message,
                StackTrace = stackTrace,
                UnityLogType = type,
                SystemInfo = Config.IncludeSystemInfo ? GetSystemInfo() : string.Empty
            };

            try
            {
                _logLock.EnterWriteLock();

                switch (type)
                {
                    case LogType.Log:
                        EnqueueLimited(_debugLogs, entry, Config.MaxDebugLogs);
                        break;
                    case LogType.Warning:
                        EnqueueLimited(_warningLogs, entry, Config.MaxWarningLogs);
                        break;
                    case LogType.Error:
                    case LogType.Exception:
                        EnqueueLimited(_errorLogs, entry, Config.MaxErrorLogs);
                        break;
                }
            }
            finally
            {
                if (_logLock.IsWriteLockHeld)
                {
                    _logLock.ExitWriteLock();
                }
            }
        }

        private static void EnqueueLimited(Queue<LogEntry> queue, LogEntry entry, int maxCount)
        {
            while (queue.Count >= maxCount)
            {
                queue.Dequeue();
            }
            queue.Enqueue(entry);
        }

        #endregion // Log Management


        #region Saving and Cleanup

        private static void SaveAndQuit()
        {
            if (_isDisposed) return;

            try
            {
                SaveGameState();
                FlushLogsSync();
            }
            finally
            {
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                Application.Quit();
                #endif
            }
        }

        private static void FlushLogsSync()
        {
            if (_isDisposed) return;

            try
            {
                _logLock.EnterReadLock();

                WriteLogsImmediate(
                    Path.Combine(DebugDataFolderPath, $"{Config.LogFilePrefix}_Debug.txt"),
                    _debugLogs);

                WriteLogsImmediate(
                    Path.Combine(DebugDataFolderPath, $"{Config.LogFilePrefix}_Warning.txt"),
                    _warningLogs);

                WriteLogsImmediate(
                    Path.Combine(DebugDataFolderPath, $"{Config.LogFilePrefix}_Error.txt"),
                    _errorLogs);
            }
            catch (Exception ex)
            {
                Debug.LogError($"{CLASS_NAME}.FlushLogsSync: Failed to save logs: {ex.Message}");
                FallbackSaveErrorDataToPlayerPrefs();
            }
            finally
            {
                if (_logLock.IsReadLockHeld)
                {
                    _logLock.ExitReadLock();
                }
            }
        }

        private static void WriteLogsImmediate(string filePath, IEnumerable<LogEntry> logs)
        {
            using var writer = new StreamWriter(filePath, false);
            foreach (var entry in logs)
            {
                writer.WriteLine(
                    $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] {entry.UnityLogType}: {entry.Message}\n{entry.StackTrace}");
            }
        }

        public static void SaveGameState()
        {
            // TODO: AppService placeholder for game-specific save logic.
        }

        private static void FallbackSaveErrorDataToPlayerPrefs()
        {
            if (_isDisposed) return;

            const int maxErrorCount = 10;
            const string errorKey = "CriticalLogs";

            try
            {
                _logLock.EnterReadLock();

                var recentErrors = _errorLogs
                    .Reverse()
                    .Take(maxErrorCount)
                    .Reverse()
                    .Select(e => $"[{e.Timestamp:yyyy-MM-dd HH:mm:ss}] {e.UnityLogType}: {e.Message}")
                    .ToList();

                string combinedErrorText = string.Join("\n", recentErrors);
                PlayerPrefs.SetString(errorKey, combinedErrorText);
                PlayerPrefs.Save();
            }
            finally
            {
                if (_logLock.IsReadLockHeld)
                {
                    _logLock.ExitReadLock();
                }
            }
        }

        private static void ClearAllData()
        {
            try
            {
                _logLock.EnterWriteLock();
                _debugLogs.Clear();
                _warningLogs.Clear();
                _errorLogs.Clear();
            }
            finally
            {
                if (_logLock.IsWriteLockHeld)
                {
                    _logLock.ExitWriteLock();
                }
            }

            try
            {
                _uiMsgLock.EnterWriteLock();
                _uiMessages.Clear();
            }
            finally
            {
                if (_uiMsgLock.IsWriteLockHeld)
                {
                    _uiMsgLock.ExitWriteLock();
                }
            }
        }

        #endregion // Saving and Cleanup


        #region Utility Methods

        private static string GetSystemInfo()
        {
            return $"OS: {UnityEngine.SystemInfo.operatingSystem}, " +
                   $"GPU: {UnityEngine.SystemInfo.graphicsDeviceName}, " +
                   $"RAM: {UnityEngine.SystemInfo.systemMemorySize}MB, " +
                   $"Unity Version: {Application.unityVersion}";
        }

        private static void RaiseErrorEvent(ErrorEventArgs e)
        {
            if (_isDisposed) return;

            var handler = OnErrorOccurred;
            if (handler != null)
            {
                try
                {
                    handler(null, e);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"{CLASS_NAME}.RaiseErrorEvent: Event handler threw exception: {ex.Message}");
                }
            }
        }

        #endregion // Utility Methods


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

        /// <summary>
        /// Forces reinitialization (useful for testing scenarios).
        /// </summary>
        public static void ForceReinitialize(AppServiceConfig config = null, ExceptionSeverityConfig severityConfig = null)
        {
            Shutdown();
            _isDisposed = false;
            Initialize(config, severityConfig);
        }

        #endregion // Test Support
    }
}