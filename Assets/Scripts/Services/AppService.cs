/****************************************************************************************
 * File: AppService.cs
 * 
 * Purpose: Provides centralized error handling, logging, and lifecycle management for Unity applications.
 * This service handles both synchronous and editor-time operations safely, ensuring proper cleanup
 * and resource management throughout the application lifecycle.
 * 
 * Key Features:
 * - Thread-safe logging with ReaderWriterLockSlim
 * - Proper Unity lifecycle integration
 * - Implements IDisposable for deterministic cleanup
 * - Fallback mechanisms for log persistence
 * - Safe shutdown handling for both editor and runtime
 * 
 * Usage:
 * AppService.Instance.HandleException("ClassName", "MethodName", exception);
 * 
 * Note: This service automatically hooks into Unity's logging system and handles
 * unhandled exceptions across the application domain.
 ****************************************************************************************/

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
    /// Used to propagate error information to the UI and other interested systems.
    /// </summary>
    public class ErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Short user-facing message or summary.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Detailed stack trace or additional info.
        /// </summary>
        public string StackTrace { get; set; }

        /// <summary>
        /// When the error occurred (UTC time).
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// System info snapshot at the time of error, if applicable.
        /// </summary>
        public string SystemInfo { get; set; }

        /// <summary>
        /// True if the error is considered fatal for the application.
        /// </summary>
        public bool IsFatal { get; set; }
    }

    /// <summary>
    /// Configuration settings for the AppService that can be serialized and modified in Unity Inspector.
    /// </summary>
    [Serializable]
    public class ErrorServiceConfig
    {
        // Whether to automatically save logs when the application quits
        public bool SaveLogsOnQuit = true;

        // Maximum number of debug logs to store
        public int MaxDebugLogs = 50;

        // Maximum number of warning logs to store
        public int MaxWarningLogs = 20;

        // Maximum number of error logs to store
        public int MaxErrorLogs = 20;

        // Prefix for log file names (e.g., "UnityLogs_Error.txt")
        public string LogFilePrefix = "UnityLogs";

        // Whether to include system information with each log entry
        public bool IncludeSystemInfo = true;
    }

    /// <summary>
    /// Represents a single log entry with critical metadata.
    /// </summary>
    public struct LogEntry
    {
        // When the log entry was created
        public DateTime Timestamp;

        // The actual log message
        public string Message;

        // Stack trace, if relevant
        public string StackTrace;

        // Corresponds to Unity's built-in LogType (e.g., Log, Warning, Error, etc.)
        public LogType UnityLogType;

        // System info captured if needed
        public string SystemInfo;
    }

    /// <summary>
    /// AppService (ErrorAndDirectoryService) provides centralized error and directory creation handling, logging, and 
    /// lifecycle management for Unity applications.
    /// </summary>
    public partial class AppService : MonoBehaviour, IDisposable
    {
        #region Constants

        private const string CLASS_NAME = "EADService";

        // Folder name constants
        public const string MyGamesFolderName = "My Games";
        public const string MainAppFolderName = "HS_MapEditor";
        public const string DebugDataFolderName = "DebugData";
        public const string ScenarioStorageFolderName = "Scenarios";
        public const string AtlasStoragePath = "Assets/Graphics/Atlases";

        #endregion // Constants


        #region Configuration and Events

        /// <summary>
        /// Singleton instance of the AppService.
        /// Initialized in Awake and destroyed with the application.
        /// </summary>
        public static AppService Instance { get; private set; }

        /// <summary>
        /// Configuration settings for the error service.
        /// Modifiable through Unity Inspector.
        /// </summary>
        [SerializeField]
        [Tooltip("Configure log limits and behavior")]
        private ErrorServiceConfig config = new();

        /// <summary>
        /// Event raised when an error occurs. Subscribers (typically UI elements)
        /// can handle this to display error messages to the user.
        /// </summary>
        public event EventHandler<ErrorEventArgs> OnErrorOccurred;

        #endregion // Configuration and Events


        #region Properties

        // Path Properties
        public string MyGamesPath { get; private set; }
        public string MainAppFolderPath { get; private set; }
        public string DebugDataFolderPath { get; private set; }
        public string ScenarioStorageFolderPath { get; private set; }

        #endregion // Properties


        #region Private Fields

        // Thread synchronization for log access
        private readonly ReaderWriterLockSlim _logLock = new(LockRecursionPolicy.SupportsRecursion);

        // Circular buffers for different log types
        private readonly Queue<LogEntry> debugLogs = new();
        private readonly Queue<LogEntry> warningLogs = new();
        private readonly Queue<LogEntry> errorLogs = new();

        // Lifecycle management
        private bool isDisposed;
        private CancellationTokenSource shutdownToken;

        #endregion // Private Fields


        #region Unity Lifecycle

        /// <summary>
        /// Initializes the singleton instance and core service components.
        /// Called when the GameObject is instantiated.
        /// </summary>
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                shutdownToken = new CancellationTokenSource();
                InitializeService();
            }
            else
            {
                // Properly dispose of this instance before destroying
                Dispose(true);
                GC.SuppressFinalize(this);  // Prevent finalizer from running
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Registers event handlers when the component becomes active.
        /// Called after Awake and when the GameObject is enabled.
        /// </summary>
        private void OnEnable()
        {
            RegisterEventHandlers();
        }

        /// <summary>
        /// Performs cleanup when the component is disabled.
        /// Ensures logs are flushed and handlers are unregistered.
        /// </summary>
        private void OnDisable()
        {
            UnregisterEventHandlers();
            FlushLogsSync();
        }

        /// <summary>
        /// Performs final cleanup when the GameObject is destroyed.
        /// Ensures all resources are properly disposed.
        /// </summary>
        private void OnDestroy()
        {
            Dispose(true);
        }

        /// <summary>
        /// Handles application shutdown.
        /// Ensures logs are saved if configured to do so.
        /// </summary>
        private void OnApplicationQuit()
        {
            if (config.SaveLogsOnQuit)
            {
                FlushLogsSync();
            }
        }

        #endregion // Unity Lifecycle


        #region Initialization and Cleanup

        /// <summary>
        /// Performs one-time initialization of the service.
        /// Sets up configuration and creates necessary directories.
        /// </summary>
        private void InitializeService()
        {
            // Setup folder paths.
            SetupFolderPaths();

            ValidateConfiguration();
            CreateLogDirectories();

            // Seed the random number generator.
            int seed = System.DateTime.Now.Millisecond;
            UnityEngine.Random.InitState(seed);

            Debug.Log($"{GetType().Name}: Service initialized successfully.");
        }

        /// <summary>
        /// Sets up the necessary folder paths for the application.
        /// </summary>
        private void SetupFolderPaths()
        {
            try
            {
                MyGamesPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    MyGamesFolderName);

                MainAppFolderPath = Path.Combine(MyGamesPath, MainAppFolderName);
                DebugDataFolderPath = Path.Combine(MainAppFolderPath, DebugDataFolderName);
                ScenarioStorageFolderPath = Path.Combine(MainAppFolderPath, ScenarioStorageFolderName);

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

        /// <summary>
        /// Registers global exception and log handlers.
        /// Should be called only once during initialization.
        /// </summary>
        private void RegisterEventHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
            Application.logMessageReceived += CaptureUnityLogs;
        }

        /// <summary>
        /// Safely unregisters all event handlers.
        /// Called during cleanup to prevent memory leaks.
        /// </summary>
        private void UnregisterEventHandlers()
        {
            if (isDisposed) return;

            AppDomain.CurrentDomain.UnhandledException -= HandleUnhandledException;
            Application.logMessageReceived -= CaptureUnityLogs;
        }

        /// <summary>
        /// Validates and adjusts configuration settings to ensure valid values.
        /// Prevents invalid states that could cause issues during operation.
        /// </summary>
        private void ValidateConfiguration()
        {
            config.MaxDebugLogs = Mathf.Max(1, config.MaxDebugLogs);
            config.MaxWarningLogs = Mathf.Max(1, config.MaxWarningLogs);
            config.MaxErrorLogs = Mathf.Max(1, config.MaxErrorLogs);
        }

        /// <summary>
        /// Creates necessary directories for log storage.
        /// Ensures write permissions and handles potential IO errors.
        /// </summary>
        private void CreateLogDirectories()
        {
            try
            {
                string logPath = DebugDataFolderPath;
                if (!Directory.Exists(logPath))
                {
                    Directory.CreateDirectory(logPath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"ErrorService.CreateLogDirectories: Error creating log directories: {ex.Message}");
            }
        }

        #endregion // Initialization and Cleanup


        #region Log Management

        /// <summary>
        /// Captures Unity log messages and stores them in appropriate queues.
        /// Implements circular buffer behavior to limit memory usage.
        /// </summary>
        /// <param name="message">The log message content</param>
        /// <param name="stackTrace">Stack trace if available</param>
        /// <param name="type">Type of log message (Log, Warning, Error, Exception)</param>
        private void CaptureUnityLogs(string message, string stackTrace, LogType type)
        {
            if (isDisposed) return;

            var entry = new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Message = message,
                StackTrace = stackTrace,
                UnityLogType = type,
                SystemInfo = config.IncludeSystemInfo ? GetSystemInfo() : string.Empty
            };

            try
            {
                _logLock.EnterWriteLock();

                switch (type)
                {
                    case LogType.Log:
                        EnqueueLimited(debugLogs, entry, config.MaxDebugLogs);
                        break;
                    case LogType.Warning:
                        EnqueueLimited(warningLogs, entry, config.MaxWarningLogs);
                        break;
                    case LogType.Error:
                    case LogType.Exception:
                        EnqueueLimited(errorLogs, entry, config.MaxErrorLogs);
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

        /// <summary>
        /// Implements circular buffer behavior for log queues.
        /// Ensures queues never exceed their configured maximum size.
        /// </summary>
        /// <param name="queue">Target log queue</param>
        /// <param name="entry">New log entry</param>
        /// <param name="maxCount">Maximum allowed entries</param>
        private void EnqueueLimited(Queue<LogEntry> queue, LogEntry entry, int maxCount)
        {
            while (queue.Count >= maxCount)
            {
                queue.Dequeue();
            }
            queue.Enqueue(entry);
        }

        #endregion

        #region Exception Handling

        /// <summary>
        /// Handles unhandled exceptions at the AppDomain level.
        /// These are always treated as fatal errors.
        /// </summary>
        /// <param name="sender">Source of the exception</param>
        /// <param name="e">Exception event args</param>
        private void HandleUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (isDisposed) return;

            if (e.ExceptionObject is Exception exception)
            {
                HandleException("UnhandledException", "Global", exception);
            }
            else
            {
                string message = $"Non-Exception unhandled error: {e.ExceptionObject}";
                Debug.LogError(message);
                RaiseErrorEvent(new ErrorEventArgs
                {
                    Message = message,
                    Timestamp = DateTime.UtcNow,
                    SystemInfo = config.IncludeSystemInfo ? GetSystemInfo() : string.Empty,
                    IsFatal = true
                });
            }
        }

        #endregion // Exception Handling


        #region Saving and Cleanup

        /// <summary>
        /// Saves game state and logs, then quits the application.
        /// Handles both editor and runtime environments appropriately.
        /// </summary>
        private void SaveAndQuit()
        {
            if (isDisposed) return;

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

        /// <summary>
        /// Synchronously writes all logs to disk.
        /// Used during shutdown to ensure no logs are lost.
        /// </summary>
        private void FlushLogsSync()
        {
            if (isDisposed) return;

            try
            {
                _logLock.EnterReadLock();

                WriteLogsImmediate(
                    Path.Combine(DebugDataFolderPath, $"{config.LogFilePrefix}_Debug.txt"),
                    debugLogs);

                WriteLogsImmediate(
                    Path.Combine(DebugDataFolderPath, $"{config.LogFilePrefix}_Warning.txt"),
                    warningLogs);

                WriteLogsImmediate(
                    Path.Combine(DebugDataFolderPath, $"{config.LogFilePrefix}_Error.txt"),
                    errorLogs);
            }
            catch (Exception ex)
            {
                Debug.LogError($"ErrorService.FlushLogsSync: Failed to save logs: {ex.Message}");
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

        /// <summary>
        /// Performs immediate synchronous write of logs to file.
        /// No async operations to ensure reliability during shutdown.
        /// </summary>
        /// <param name="filePath">Target file path for logs</param>
        /// <param name="logs">Collection of logs to write</param>
        private void WriteLogsImmediate(string filePath, IEnumerable<LogEntry> logs)
        {
            using var writer = new StreamWriter(filePath, false);
            foreach (var entry in logs)
            {
                writer.WriteLine(
                    $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] {entry.UnityLogType}: {entry.Message}\n{entry.StackTrace}");
            }
        }

        /// <summary>
        /// Placeholder for game state saving functionality.
        /// To be implemented based on game-specific requirements.
        /// </summary>
        public void SaveGameState()
        {
            // TODO: Implement game state saving logic here.
        }

        /// <summary>
        /// Retrieves formatted system information for logging.
        /// Helps with debugging by providing context about the runtime environment.
        /// </summary>
        /// <returns>Formatted string containing system information</returns>
        private string GetSystemInfo()
        {
            return $"OS: {UnityEngine.SystemInfo.operatingSystem}, " +
                   $"GPU: {UnityEngine.SystemInfo.graphicsDeviceName}, " +
                   $"RAM: {UnityEngine.SystemInfo.systemMemorySize}MB, " +
                   $"Unity Version: {Application.unityVersion}";
        }

        /// <summary>
        /// Fallback mechanism to save critical error logs when file I/O fails.
        /// Uses PlayerPrefs as a last resort storage option.
        /// </summary>
        private void FallbackSaveErrorDataToPlayerPrefs()
        {
            if (isDisposed) return;

            const int maxErrorCount = 10;
            const string errorKey = "CriticalLogs";

            try
            {
                _logLock.EnterReadLock();

                var recentErrors = errorLogs
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

        #endregion // Saving and Cleanup


        #region IDisposable Implementation

        /// <summary>
        /// Public implementation of Dispose pattern.
        /// Ensures proper cleanup of managed resources.
        /// Always use in conjunction with 'using' statements when possible.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// Handles both explicit disposal and finalization.
        /// </summary>
        /// <param name="disposing">
        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    // Free managed resources
                    try
                    {
                        // Cancel any pending operations
                        shutdownToken?.Cancel();
                        shutdownToken?.Dispose();

                        // Dispose the thread synchronization lock
                        if (_logLock != null)
                        {
                            // Ensure no locks are held before disposing
                            while (_logLock.IsReadLockHeld || _logLock.IsWriteLockHeld || _logLock.IsUpgradeableReadLockHeld)
                            {
                                Thread.Sleep(1);
                            }
                            _logLock.Dispose();
                        }

                        // Clear log queues
                        debugLogs?.Clear();
                        warningLogs?.Clear();
                        errorLogs?.Clear();
                    }
                    catch (Exception ex)
                    {
                        // Log but don't throw from Dispose
                        Debug.LogError($"ErrorService.Dispose: Error during cleanup: {ex.Message}");
                    }
                }

                // Set disposed flag
                isDisposed = true;
            }
        }

        #endregion // IDisposable Implementation


        #region Event Handling

        /// <summary>
        /// Safely raises the OnErrorOccurred event if the service hasn't been disposed.
        /// Implements thread-safe event invocation pattern.
        /// </summary>
        /// <param name="e">Error event arguments to pass to subscribers</param>
        private void RaiseErrorEvent(ErrorEventArgs e)
        {
            if (isDisposed) return;

            // Cache event to prevent race conditions
            var handler = OnErrorOccurred;
            if (handler != null)
            {
                try
                {
                    handler(this, e);
                }
                catch (Exception ex)
                {
                    // Log but don't throw - event handler exceptions shouldn't crash the service
                    Debug.LogError($"ErrorService.RaiseErrorEvent: Event handler threw exception: {ex.Message}");
                }
            }
        }

        #endregion // Event Handling
    }
}