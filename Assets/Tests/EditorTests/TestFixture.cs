using HammerAndSickle.Controllers;
using HammerAndSickle.Models;
using HammerAndSickle.Services;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Universal base class for all Unity tests providing common setup, teardown,
    /// and utility methods for consistent test execution.
    /// </summary>
    [TestFixture]
    public abstract class BaseTestFixture
    {
        #region Constants

        private const string CLASS_NAME = nameof(BaseTestFixture);
        private const int TEST_RANDOM_SEED = 42;

        #endregion //Constants

        #region Properties and Fields

        /// <summary>
        /// Test handler for capturing exceptions and messages during tests.
        /// </summary>
        protected TestHandler TestHandler { get; private set; }

        /// <summary>
        /// Random number generator seeded for reproducible test results.
        /// </summary>
        protected Random TestRandom { get; private set; }

        /// <summary>
        /// Store all messages.
        /// </summary>
        protected List<string> TestLog { get; } = new List<string>();

        /// <summary>
        /// GameDataManager instance used consistently throughout tests.
        /// </summary>
        protected GameDataManager GameManager { get; private set; }

        #endregion //Properties and Fields

        #region Setup and Teardown

        /// <summary>
        /// One-time setup for the entire test fixture.
        /// </summary>
        [OneTimeSetUp]
        public virtual void OneTimeSetUp()
        {
            try
            {
                TestLog.Clear();
                TestLog.Add($"Starting OneTimeSetUp for {GetType().Name}");

                InitializeTestEnvironment();
               
                TestLog.Add($"OneTimeSetUp completed successfully for {GetType().Name}");
            }
            catch (Exception ex)
            {
                TestLog.Add($"OneTimeSetUp failed: {ex.Message}");
                AppService.HandleException(CLASS_NAME, nameof(OneTimeSetUp), ex);
                throw;
            }
        }

        /// <summary>
        /// One-time cleanup for the entire test fixture.
        /// </summary>
        [OneTimeTearDown]
        public virtual void OneTimeTearDown()
        {
            try
            {
                TestLog.Add($"Starting OneTimeTearDown for {GetType().Name}");

                // Clear GameDataManager state to ensure test isolation
                GameManager?.ClearAll();

                CleanupTestEnvironment();
                TestLog.Add($"OneTimeTearDown completed for {GetType().Name}");
            }
            catch (Exception ex)
            {
                TestLog.Add($"OneTimeTearDown failed: {ex.Message}");
                AppService.HandleException(CLASS_NAME, nameof(OneTimeTearDown), ex);
            }
        }

        /// <summary>
        /// Setup before each individual test method.
        /// </summary>
        [SetUp]
        public virtual void SetUp()
        {
            try
            {
                // Clear test handler for each test
                TestHandler?.Clear();
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetUp), ex);
                throw;
            }
        }

        /// <summary>
        /// Cleanup after each individual test method.
        /// </summary>
        [TearDown]
        public virtual void TearDown()
        {
            try
            {
                // Log any captured exceptions or messages for debugging
                if (TestHandler != null && TestHandler.ExceptionCount > 0)
                {
                    TestLog.Add($"Test captured {TestHandler.ExceptionCount} exceptions");
                }
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(TearDown), ex);
            }
        }

        #endregion //Setup and Teardown

        #region Test Environment Management

        /// <summary>
        /// Initializes the test environment with proper AppService configuration.
        /// </summary>
        private void InitializeTestEnvironment()
        {
            try
            {
                TestLog.Add("Initializing test environment");

                // Initialize AppService for testing
                TestHandler = new TestHandler();
                AppService.SetTestHandler(TestHandler);

                // Seed the random number generator for reproducibility
                TestRandom = new Random(TEST_RANDOM_SEED);

                // Get consistent GameDataManager instance
                GameManager = GameDataManager.Instance;

                // Clear any existing state
                GameManager.ClearAll();

                TestLog.Add("Test environment initialization completed");
            }
            catch (Exception ex)
            {
                TestLog.Add($"Failed to initialize test environment: {ex.Message}");
                AppService.HandleException(CLASS_NAME, nameof(InitializeTestEnvironment), ex);
                throw;
            }
        }

        /// <summary>
        /// Cleans up the test environment.
        /// </summary>
        private void CleanupTestEnvironment()
        {
            try
            {
                if (TestHandler != null)
                {
                    TestHandler.Clear();
                }

                AppService.ResetTestHandler();
                TestLog.Add("Test environment cleanup completed");
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(CleanupTestEnvironment), ex);
            }
        }

        #endregion //Test Environment Management

        #region Utility Methods

        /// <summary>
        /// Verifies that exception handling is working correctly in tests.
        /// </summary>
        /// <param name="expectedSource">Expected source class/method</param>
        protected void VerifyExceptionHandled<T>(string expectedSource) where T : Exception
        {
            try
            {
                Assert.IsTrue(TestHandler.HasExceptionOfType<T>(),
                    $"Expected exception of type {typeof(T).Name} was not captured");
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(VerifyExceptionHandled), ex);
                throw;
            }
        }

        /// <summary>
        /// Validates that systems are properly initialized for testing.
        /// </summary>
        protected void AssertSystemsInitialized()
        {
            try
            {
                Assert.IsTrue(WeaponSystemsDatabase.IsInitialized, "WeaponSystemsDatabase should be initialized");
                Assert.IsTrue(IntelProfileDatabase.IsInitialized, "IntelProfile should be initialized");
                Assert.IsTrue(CombatUnitDatabase.IsInitialized, "CombatUnitDatabase should be initialized");
                Assert.IsNotNull(TestHandler, "TestHandler should be initialized");
                Assert.IsNotNull(TestRandom, "TestRandom should be initialized");
                Assert.IsNotNull(GameManager, "GameManager should be initialized");
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(AssertSystemsInitialized), ex);
                throw;
            }
        }

        /// <summary>
        /// Logs test execution details for debugging purposes.
        /// </summary>
        /// <param name="message">Message to log</param>
        protected void LogTestExecution(string message)
        {
            try
            {
                TestLog.Add($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(LogTestExecution), ex);
            }
        }

        /// <summary>
        /// Gets the complete test log for debugging failed tests.
        /// </summary>
        /// <returns>Complete test execution log</returns>
        protected string GetTestLog()
        {
            try
            {
                return string.Join(Environment.NewLine, TestLog);
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetTestLog), ex);
                return $"Error retrieving test log: {ex.Message}";
            }
        }

        #endregion //Utility Methods
    }
}