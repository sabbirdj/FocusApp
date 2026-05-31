// Copyright (c) FocusMode. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text.Json;
using FocusMode.Models;

namespace FocusMode.Services
{
    /// <summary>
    /// Provides persistence for <see cref="FocusSession"/> objects, saving and loading
    /// them to/from <c>%AppData%\FocusMode\suspended_session.json</c>.
    /// This enables crash recovery — if the app crashes while focus mode is active,
    /// the session file survives and suspended processes can be resumed on next launch.
    /// </summary>
    public sealed class SessionPersistenceService
    {
        private static readonly string SessionDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FocusMode");

        private static readonly string SessionFilePath =
            Path.Combine(SessionDirectory, "suspended_session.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionPersistenceService"/> class.
        /// Ensures the session directory exists.
        /// </summary>
        public SessionPersistenceService()
        {
            Directory.CreateDirectory(SessionDirectory);
        }

        /// <summary>
        /// Serializes the given <see cref="FocusSession"/> to JSON and writes it to disk.
        /// This should be called immediately after activating focus mode so that
        /// crash recovery is always possible.
        /// </summary>
        /// <param name="session">The focus session to persist.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="session"/> is <c>null</c>.</exception>
        public void SaveSession(FocusSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            try
            {
                string json = JsonSerializer.Serialize(session, JsonOptions);
                File.WriteAllText(SessionFilePath, json);
                System.Diagnostics.Debug.WriteLine(
                    $"[SessionPersistence] Session saved with {session.SuspendedProcesses?.Count ?? 0} killed processes.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[SessionPersistence] Failed to save session: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Reads the session file from disk and deserializes it into a <see cref="FocusSession"/>.
        /// Returns <c>null</c> if the file does not exist or cannot be deserialized.
        /// </summary>
        /// <returns>
        /// The deserialized <see cref="FocusSession"/>, or <c>null</c> if no session file is found.
        /// </returns>
        public FocusSession? LoadSession()
        {
            try
            {
                if (!File.Exists(SessionFilePath))
                    return null;

                string json = File.ReadAllText(SessionFilePath);
                if (string.IsNullOrWhiteSpace(json))
                    return null;

                var session = JsonSerializer.Deserialize<FocusSession>(json, JsonOptions);
                System.Diagnostics.Debug.WriteLine(
                    $"[SessionPersistence] Session loaded with {session?.SuspendedProcesses?.Count ?? 0} killed processes.");
                return session;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[SessionPersistence] Failed to load session: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deletes the session file from disk. Should be called after a clean
        /// deactivation of focus mode.
        /// </summary>
        public void ClearSession()
        {
            try
            {
                if (File.Exists(SessionFilePath))
                {
                    File.Delete(SessionFilePath);
                    System.Diagnostics.Debug.WriteLine("[SessionPersistence] Session file cleared.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[SessionPersistence] Failed to clear session: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks whether a session file exists on disk, indicating that a previous
        /// focus session was not properly ended (i.e., the app crashed).
        /// </summary>
        /// <returns>
        /// <c>true</c> if a crashed session file exists; otherwise, <c>false</c>.
        /// </returns>
        public bool HasCrashedSession()
        {
            return File.Exists(SessionFilePath);
        }
    }
}

