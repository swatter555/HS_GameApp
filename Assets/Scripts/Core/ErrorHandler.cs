using HammerAndSickle.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security;
using UnityEngine;

namespace HammerAndSickle.Core
{
    /// <summary>
    /// Interface for handling exceptions throughout the application.
    /// Allows for easy mocking and testing without depending on specific implementations.
    /// </summary>
    public interface IErrorHandler
    {
        /// <summary>
        /// Handles an exception that occurred in application code.
        /// </summary>
        /// <param name="className">Name of the class where the exception occurred</param>
        /// <param name="methodName">Name of the method where the exception occurred</param>
        /// <param name="exception">The exception that was caught</param>
        void HandleException(string className, string methodName, Exception exception);

        /// <summary>
        /// Handles an exception with specified severity level.
        /// </summary>
        /// <param name="className">Name of the class where the exception occurred</param>
        /// <param name="methodName">Name of the method where the exception occurred</param>
        /// <param name="exception">The exception that was caught</param>
        /// <param name="severity">Severity level of the exception</param>
        void HandleException(string className, string methodName, Exception exception, ExceptionSeverity severity);
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
    /// Static provider for error handling throughout the application.
    /// Provides a clean interface that can be easily mocked for unit testing.
    /// Integrates seamlessly with AppService while allowing test substitution.
    /// </summary>
    public static class ErrorHandler
    {
        private static IErrorHandler _instance;

        /// <summary>
        /// Gets the current error handler instance.
        /// Defaults to AppService if available, otherwise uses fallback handler.
        /// </summary>
        public static IErrorHandler Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                // Try to use AppService if available
                if (Services.AppService.Instance != null)
                    return Services.AppService.Instance;

                // Fallback for testing or when AppService isn't available
                return new FallbackErrorHandler();
            }
            private set => _instance = value;
        }

        /// <summary>
        /// Sets a custom error handler. Useful for unit testing.
        /// </summary>
        /// <param name="handler">The error handler to use, or null to reset to AppService</param>
        public static void SetHandler(IErrorHandler handler)
        {
            Instance = handler;
        }

        /// <summary>
        /// Resets the error handler to use AppService (production default).
        /// </summary>
        public static void ResetToDefault()
        {
            _instance = null; // Will fall back to AppService
        }

        /// <summary>
        /// Handles an exception using the current error handler with Minor severity.
        /// </summary>
        /// <param name="className">Name of the class where the exception occurred</param>
        /// <param name="methodName">Name of the method where the exception occurred</param>
        /// <param name="exception">The exception that was caught</param>
        public static void HandleException(string className, string methodName, Exception exception)
        {
            Instance?.HandleException(className, methodName, exception, ExceptionSeverity.Minor);
        }

        /// <summary>
        /// Handles an exception using the current error handler with specified severity.
        /// </summary>
        /// <param name="className">Name of the class where the exception occurred</param>
        /// <param name="methodName">Name of the method where the exception occurred</param>
        /// <param name="exception">The exception that was caught</param>
        /// <param name="severity">Severity level of the exception</param>
        public static void HandleException(string className, string methodName, Exception exception, ExceptionSeverity severity)
        {
            Instance?.HandleException(className, methodName, exception, severity);
        }
    }

    /// <summary>
    /// Fallback error handler used when AppService is not available (typically during testing).
    /// </summary>
    public class FallbackErrorHandler : IErrorHandler
    {
        public void HandleException(string className, string methodName, Exception exception)
        {
            HandleException(className, methodName, exception, ExceptionSeverity.Minor);
        }

        public void HandleException(string className, string methodName, Exception exception, ExceptionSeverity severity)
        {
            string severityTag = severity == ExceptionSeverity.Fatal || severity == ExceptionSeverity.Critical ? "[CRITICAL]" : "[INFO]";
            Debug.LogError($"{severityTag} [{className}.{methodName}] {exception.Message}\n{exception.StackTrace}");
        }
    }

    /// <summary>
    /// Test error handler that collects exceptions for verification in unit tests.
    /// </summary>
    public class TestErrorHandler : IErrorHandler
    {
        private readonly List<(string ClassName, string MethodName, Exception Exception, ExceptionSeverity Severity)> _handledException;

        public TestErrorHandler()
        {
            _handledException = new List<(string, string, Exception, ExceptionSeverity)>();
        }

        /// <summary>
        /// Gets a read-only list of all handled exceptions.
        /// </summary>
        public IReadOnlyList<(string ClassName, string MethodName, Exception Exception, ExceptionSeverity Severity)> HandledException => _handledException.AsReadOnly();

        /// <summary>
        /// Gets the count of handled exceptions.
        /// </summary>
        public int ExceptionCount => _handledException.Count;

        /// <summary>
        /// Clears all recorded exceptions.
        /// </summary>
        public void Clear()
        {
            _handledException.Clear();
        }

        /// <summary>
        /// Checks if a specific exception type was handled.
        /// </summary>
        /// <typeparam name="T">The exception type to check for</typeparam>
        /// <returns>True if an exception of this type was handled</returns>
        public bool HasExceptionOfType<T>() where T : Exception
        {
            return _handledException.Any(e => e.Exception is T);
        }

        /// <summary>
        /// Checks if an exception of specific severity was handled.
        /// </summary>
        /// <param name="severity">The severity level to check for</param>
        /// <returns>True if an exception of this severity was handled</returns>
        public bool HasExceptionOfSeverity(ExceptionSeverity severity)
        {
            return _handledException.Any(e => e.Severity == severity);
        }

        /// <summary>
        /// Gets the most recent exception of a specific type.
        /// </summary>
        /// <typeparam name="T">The exception type to find</typeparam>
        /// <returns>The most recent exception of this type, or null if none found</returns>
        public T GetMostRecentExceptionOfType<T>() where T : Exception
        {
            return _handledException
                .Where(e => e.Exception is T)
                .LastOrDefault().Exception as T;
        }

        public void HandleException(string className, string methodName, Exception exception)
        {
            HandleException(className, methodName, exception, ExceptionSeverity.Minor);
        }

        public void HandleException(string className, string methodName, Exception exception, ExceptionSeverity severity)
        {
            _handledException.Add((className, methodName, exception, severity));
        }
    }

    /// <summary>
    /// Silent error handler that does nothing - useful for performance testing.
    /// </summary>
    public class NullErrorHandler : IErrorHandler
    {
        public void HandleException(string className, string methodName, Exception exception)
        {
            // Silent
        }

        public void HandleException(string className, string methodName, Exception exception, ExceptionSeverity severity)
        {
            // Silent
        }
    }
}

// Extension to the existing AppService class
namespace HammerAndSickle.Services
{
    public partial class AppService : IErrorHandler
    {
        #region Configuration Extensions

        /// <summary>
        /// Configuration for exception severity handling.
        /// </summary>
        [Serializable]
        public class ExceptionSeverityConfig
        {
            [Tooltip("Whether to save game state on Critical exceptions")]
            public bool SaveOnCritical = true;

            [Tooltip("Whether to quit application on Critical exceptions")]
            public bool QuitOnCritical = true;

            [Tooltip("Whether to save game state on Fatal exceptions")]
            public bool SaveOnFatal = true;

            [Tooltip("Whether to quit application on Fatal exceptions")]
            public bool QuitOnFatal = true;

            [Tooltip("Whether to show error dialogs for Moderate+ exceptions")]
            public bool ShowErrorDialogs = true;
        }

        /// <summary>
        /// Severity-based exception handling configuration.
        /// </summary>
        [SerializeField]
        [Tooltip("Configure how different exception severities are handled")]
        private ExceptionSeverityConfig severityConfig = new();

        #endregion

        #region IErrorHandler Implementation

        /// <summary>
        /// Handles an exception with Minor severity (backward compatibility).
        /// </summary>
        /// <param name="className">Name of the class where the exception occurred</param>
        /// <param name="methodName">Name of the method where the exception occurred</param>
        /// <param name="exception">The exception that was caught</param>
        public void HandleException(string className, string methodName, Exception exception)
        {
            // Determine severity based on exception type for backward compatibility
            var severity = DetermineExceptionSeverity(exception);
            HandleException(className, methodName, exception, severity);
        }

        /// <summary>
        /// Handles an exception with specified severity level.
        /// </summary>
        /// <param name="className">Name of the class where the exception occurred</param>
        /// <param name="methodName">Name of the method where the exception occurred</param>
        /// <param name="exception">The exception that was caught</param>
        /// <param name="severity">Severity level of the exception</param>
        public void HandleException(string className, string methodName, Exception exception, Core.ExceptionSeverity severity)
        {
            if (isDisposed) return;

            string message = $"Exception in {className}.{methodName}: {exception.Message}";

            // Log with appropriate level based on severity
            switch (severity)
            {
                case Core.ExceptionSeverity.Minor:
                    Debug.LogWarning($"[{severity}] {message}");
                    break;
                case Core.ExceptionSeverity.Moderate:
                    Debug.LogWarning($"[{severity}] {message}\n{exception.StackTrace}");
                    break;
                case Core.ExceptionSeverity.Critical:
                case Core.ExceptionSeverity.Fatal:
                    Debug.LogError($"[{severity}] {message}\n{exception.StackTrace}");
                    break;
            }

            // Raise error event for UI notification (if configured)
            if (severity >= Core.ExceptionSeverity.Moderate && severityConfig.ShowErrorDialogs)
            {
                RaiseErrorEvent(new ErrorEventArgs
                {
                    Message = message,
                    StackTrace = exception.StackTrace,
                    Timestamp = DateTime.UtcNow,
                    SystemInfo = config.IncludeSystemInfo ? GetSystemInfo() : string.Empty,
                    IsFatal = severity == Core.ExceptionSeverity.Fatal
                });
            }

            // Handle critical/fatal exceptions
            switch (severity)
            {
                case Core.ExceptionSeverity.Critical:
                    if (severityConfig.SaveOnCritical && severityConfig.QuitOnCritical)
                    {
                        SaveAndQuit();
                    }
                    else if (severityConfig.SaveOnCritical)
                    {
                        SaveGameState();
                    }
                    break;

                case Core.ExceptionSeverity.Fatal:
                    if (severityConfig.SaveOnFatal && severityConfig.QuitOnFatal)
                    {
                        SaveAndQuit();
                    }
                    else if (severityConfig.SaveOnFatal)
                    {
                        SaveGameState();
                    }
                    break;
            }
        }

        private Core.ExceptionSeverity DetermineExceptionSeverity(Exception exception)
        {
            return exception switch
            {
                // Specific argument exceptions first (most specific to least specific)
                ArgumentNullException => Core.ExceptionSeverity.Minor,
                ArgumentOutOfRangeException => Core.ExceptionSeverity.Minor,
                ArgumentException => Core.ExceptionSeverity.Minor,

                // Logic and state errors
                InvalidOperationException => Core.ExceptionSeverity.Moderate,
                NotSupportedException => Core.ExceptionSeverity.Moderate,
                NullReferenceException => Core.ExceptionSeverity.Moderate,
                IndexOutOfRangeException => Core.ExceptionSeverity.Minor,

                // I/O and serialization errors
                IOException => Core.ExceptionSeverity.Moderate,
                FileNotFoundException => Core.ExceptionSeverity.Moderate,
                DirectoryNotFoundException => Core.ExceptionSeverity.Moderate,
                SerializationException => Core.ExceptionSeverity.Moderate,
                TimeoutException => Core.ExceptionSeverity.Moderate,

                // Security and access issues
                UnauthorizedAccessException => Core.ExceptionSeverity.Critical,
                SecurityException => Core.ExceptionSeverity.Critical,

                // System resource exhaustion (fatal)
                OutOfMemoryException => Core.ExceptionSeverity.Fatal,
                InsufficientMemoryException => Core.ExceptionSeverity.Fatal,
                StackOverflowException => Core.ExceptionSeverity.Fatal,
                AccessViolationException => Core.ExceptionSeverity.Fatal,

                // Default for unknown exceptions
                _ => Core.ExceptionSeverity.Moderate
            };
        }

        #endregion
    }
}