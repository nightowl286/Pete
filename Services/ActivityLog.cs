using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using Pete.Models;
using Pete.Services.Interfaces;
using Prism.Mvvm;
using LogEntry = Pete.Models.LogEntryInfo<Pete.Services.Interfaces.EntryLogType>;

namespace Pete.Services
{
    public class ActivityLog : BindableBase, IActivityLog
    {
        #region Consts
        private const string PATH_DATA = "data";
        #endregion

        #region Private
        private Dictionary<uint, List<LogEntry>> _EntryLogs;
        #endregion
        public ActivityLog()
        {
            _EntryLogs = new Dictionary<uint, List<LogEntry>>();
        }

        #region Methods
        private void LoadEntryLogs()
        {

        }
        private void SaveEntryLogs()
        {

        }

        public IEnumerable<LogEntry> GetAll(uint entryId)
        {
            if (_EntryLogs.TryGetValue(entryId, out List<LogEntry> list))
                return new List<LogEntry>(list);
            return new List<LogEntry>();
        }
        public DateTime Log(uint entryId, EntryLogType type)
        {
            DateTime now = DateTime.UtcNow;
            List<LogEntry> addTo;
            if (_EntryLogs.TryGetValue(entryId, out List<LogEntry> list))
                addTo = list;
            else
            {
                addTo = new List<LogEntry>();
                _EntryLogs.Add(entryId, addTo);
            }

            addTo.Add(new LogEntry(now, type));

            SaveEntryLogs();

            return now;
        }
        public DateTime? GetLast(uint entryId, EntryLogType type)
        {
            if (!_EntryLogs.TryGetValue(entryId, out List<LogEntry> list))
                return null;

            for(int i = list.Count-1; i>=0; i--)
            {
                if (list[i].Type == type)
                    return list[i].UtcDate;
            }

            return null;
        }
        public void GetLastAll(uint entryId, out DateTime? view, out DateTime? edit, out DateTime? create)
        {
            view = edit = create = null;
            if (!_EntryLogs.TryGetValue(entryId, out List<LogEntry> list))
                return;

            for(int i = list.Count-1;i>=0; i--)
            {
                if (view.HasValue & edit.HasValue & create.HasValue) return;

                DateTime time = list[i].UtcDate;
                EntryLogType type = list[i].Type;

                if (type == EntryLogType.Create & !create.HasValue) create = time;
                if (type == EntryLogType.Edit & !edit.HasValue) edit = time;
                if (type == EntryLogType.View & !view.HasValue) view = time;
            }
        }
        public void GetLastAll(uint entryId, out DateTime? view, out DateTime? edit)
        {
            view = edit = null;
            if (!_EntryLogs.TryGetValue(entryId, out List<LogEntry> list))
                return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (view.HasValue & edit.HasValue) return;

                DateTime time = list[i].UtcDate;
                EntryLogType type = list[i].Type;

                if (type == EntryLogType.Edit & !edit.HasValue) edit = time;
                if (type == EntryLogType.View & !view.HasValue) view = time;
            }
        }
        #endregion
    }
}
