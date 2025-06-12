// -----------------------------------------------------------------------------
//  Hammer & Sickle – AppService UI‑Message Extension
// -----------------------------------------------------------------------------
//  This partial adds a thread‑safe buffer for player‑facing UI messages. The
//  buffer keeps the most recent 100 lines (FIFO) so the gameplay/UI layer can
//  query both the *latest* message and a *rolling log* when it chooses.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;

namespace HammerAndSickle.Services
{
    public partial class AppService
    {
        #region UI Message Capture =================================================

        /// <summary>
        /// Maximum number of UI messages to keep in memory.
        /// </summary>
        private const int MaxUiMessages = 100;

        // Thread‑safe queue to store UI messages chronologically (oldest → newest).
        private readonly Queue<string> _uiMessages = new Queue<string>(MaxUiMessages);
        private readonly ReaderWriterLockSlim _uiMsgLock = new ReaderWriterLockSlim();

        /// <summary>
        /// Most recent UI message captured. Returns <c>null</c> if no message exists.
        /// </summary>
        public string LatestUiMessage
        {
            get
            {
                _uiMsgLock.EnterReadLock();
                try
                {
                    return _uiMessages.Count > 0 ? _uiMessages.Peek() : null;
                }
                finally { _uiMsgLock.ExitReadLock(); }
            }
        }

        /// <summary>
        /// Captures a message intended for the in‑game UI. Maintains a fixed‑size
        /// circular buffer so memory usage stays bounded.
        /// </summary>
        /// <param name="message">Human‑readable string to show players.</param>
        public void CaptureUiMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            _uiMsgLock.EnterWriteLock();
            try
            {
                // Respect size cap – drop oldest when full.
                if (_uiMessages.Count == MaxUiMessages)
                    _uiMessages.Dequeue();

                _uiMessages.Enqueue(message);
            }
            finally { _uiMsgLock.ExitWriteLock(); }
        }

        /// <summary>
        /// Returns a snapshot of all buffered UI messages (oldest → newest).
        /// </summary>
        public IReadOnlyList<string> GetUiMessageLog()
        {
            _uiMsgLock.EnterReadLock();
            try { return _uiMessages.ToArray(); }
            finally { _uiMsgLock.ExitReadLock(); }
        }

        /// <summary>
        /// Must be called from <c>Dispose(bool)</c> to clean up lock resources.
        /// </summary>
        private void DisposeUiMessageLock()
        {
            if (_uiMsgLock != null)
            {
                try { _uiMsgLock.Dispose(); }
                catch { /* Suppress – disposing should never throw. */ }
            }
        }

        #endregion // UI Message Capture
    }
}