using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using Pete.Models;
using Pete.Models.Logs;
using Pete.Models.Warnings;

namespace Pete.Services.Interfaces
{

    public interface IActivityLog : INotifyPropertyChanged
    {
        #region Properties
        bool GotDecrypted { get; }
        bool HasUnseenWarning { get; }
        ReadOnlyObservableCollection<WarningBase> Warnings { get; }
        #endregion

        #region Methods
        void AddFirstRegistration();
        void RunCleanup();
        void GenerateWarnings();
        IEnumerable<LogBase> GetAll();
        void LogDeletion(uint id, string name, string category);
        void InvalidateEntryLogs(uint entryId);
        void LoadEncrypted();
        void LogFailedLogin();
        void SeenWarnings();
        IEnumerable<EntryLog> GetAll(uint entryId);
        IEnumerable<EntryLog> GetLast(uint entryId, int count);
        DateTime? GetLast(uint entryId, EntryLogType type);
        void GetLastAll(uint entryId, out DateTime? view, out DateTime? edit, out DateTime? create);
        void GetLastAll(uint entryId, out DateTime? view, out DateTime? edit);
        DateTime Log(uint entryId, EntryLogType type);
        void Log(LogType type);
        #endregion
    }
}
