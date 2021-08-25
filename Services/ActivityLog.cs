using System;
using System.Collections.Generic;
using Pete.Services.Interfaces;
using Prism.Mvvm;
using Pete.Models.Logs;
using System.IO;
using TNO.BitUtilities;
using System.Text;
using System.Linq;
using Pete.Models;
using Microsoft.Win32;
using System.Diagnostics;
using System.Collections.ObjectModel;
using Pete.Models.Warnings;
using System.Windows;

namespace Pete.Services
{
    public class ActivityLog : BindableBase, IActivityLog
    {
        #region Consts
        private const string PATH_DATA = "data";
        private const string PATH_LOG = PATH_DATA + "\\log.data";
        private const int MAXIMUM_LOG_TIME_DIFFERENCE_MS = 150;
        #endregion

        #region Private
        private byte[] _TempData;
        private bool _GotDecrypted;
        private bool _HasUnseenWarning;
        private LogBase _RegistrationLog;
        private DateTime? _WarningSeenAt;
        private DateTime? _LastCleanup;
        private Dictionary<uint, List<EntryLog>> _EntryLogs;
        private List<LogBase> _AllLogs;
        private readonly IEncryptionModule _EncryptionModule;
        private DateTime? _LastLogWriteReg;
        private DateTime? _LastLogWriteFile;
        private DateTime? _LastLogCreateTime;
        private DateTime? _LastRegWrite;
        private bool _WasRegTypeChanged;
        private bool _WasLogTampered;
        private LogBase _LastLog;
        private ObservableCollection<WarningBase> _Warnings = new ObservableCollection<WarningBase>();
        #endregion

        #region Properties
        public bool GotDecrypted { get => _GotDecrypted; private set => SetProperty(ref _GotDecrypted, value); }
        public bool HasUnseenWarning { get => _HasUnseenWarning; private set => SetProperty(ref _HasUnseenWarning, value); }
        public ReadOnlyObservableCollection<WarningBase> Warnings => new ReadOnlyObservableCollection<WarningBase>(_Warnings);
        #endregion
        public ActivityLog(IEncryptionModule encryptionModule)
        {
            _EntryLogs = new Dictionary<uint, List<EntryLog>>();
            _AllLogs = new List<LogBase>();
            _EncryptionModule = encryptionModule;

            Load();

        }

        #region Methods
        public void RunCleanup()
        {

        }
        private void LoadLastLogDate()
        {
            if (File.Exists(PATH_LOG))
            {
                FileInfo file = new FileInfo(PATH_LOG);
                _LastLogWriteFile = file.LastWriteTimeUtc;
                _LastLogCreateTime = file.CreationTimeUtc;
            }

#if DEBUG
            if (!App.REQUIRE_ADMIN) return;
#endif
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\TNO\Pete", false))
            {
                if (key != null)
                {
                    object got = key.GetValue("value", null);
                    if (got is long gotLong)
                        _LastLogWriteReg = new DateTime(gotLong);
                    else if (got != null)
                        _WasRegTypeChanged = true;

                    _LastRegWrite = key.GetLastWrite();
                }
            }
        }
        public void GenerateWarnings()
        {
            _Warnings.Clear();

            #region Failed logins
            List<WarningFailedLogin> failedLogins = new List<WarningFailedLogin>();
            for (int i = _AllLogs.Count - 1; i >= 0; i--)
            {
                LogBase log = _AllLogs[i];
                if (log.Type == LogType.WarningsSeen)
                    break;

                if (log.Type == LogType.FailedLogin)
                    failedLogins.Add(new WarningFailedLogin(log.Date));
            }
            if (failedLogins.Count == 1)
                _Warnings.Add(failedLogins[0]);
            else if (failedLogins.Count > 1)
                _Warnings.Add(new WarningFailedLoginGroup(failedLogins.ToArray()));
            #endregion

            if (_RegistrationLog == null)
                _Warnings.Add(new WarningLogWiped());

            bool registerDateInvalid = _RegistrationLog != null && _RegistrationLog.Date != _LastLogCreateTime;
            bool writeInvalid = _LastLogWriteFile != _LastLogWriteReg || _LastRegWrite != _LastLogWriteFile || _LastLogWriteFile == null || _WasRegTypeChanged;

            if (registerDateInvalid || writeInvalid)
                _Warnings.Add(new WarningLogRestored());

            foreach (WarningBase warning in _Warnings)
                Debug.WriteLine("  " + warning.GetType().Name);
            Debug.WriteLine("");



            HasUnseenWarning = _Warnings.Count > 0;
        }
        public void LogDeletion(uint id, string name, string category)
        {
            EntryDeletedLog log = new EntryDeletedLog(DateTime.UtcNow, name, category);
            InvalidateEntryLogs(id);
            AddLog(log);
        }
        public void InvalidateEntryLogs(uint entryId)
        {
            if (_EntryLogs.ContainsKey(entryId))
                _EntryLogs.Remove(entryId);

            foreach(LogBase log in _AllLogs)
            {
                if (log is EntryLog entryLog)
                {
                    if (entryLog.EntryId == entryId)
                        entryLog.EntryId = null;
                }
            }
        }
        private LogBase ReadLog(IAdvancedBitReader r)
        {
            LogType type = r.ReadLogType();
            DateTime date = r.Read<DateTime>();

            if (type == LogType.Entry)
            {
                EntryLogType entryType = r.ReadEntryLogType();
                if (entryType == EntryLogType.Delete)
                {
                    string name = r.ReadString(Encoding.UTF8);
                    string category = r.ReadBool() ? r.ReadString(Encoding.UTF8) : null;
                    return new EntryDeletedLog(date, name, category);
                }
                uint? entryId = null;
                if (r.ReadBool()) entryId = r.Read<uint>();


                return new EntryLog(entryType, date, entryId);
            }
            else if (type == LogType.TamperAttempt)
            {
                TamperType tamper = r.ReadTamperType();
                return new TamperLog(tamper, date);
            }

            return new LogBase(type, date);
        }
        private void CheckLogType(LogBase log)
        {
            if (log.Type == LogType.WarningsSeen && _WarningSeenAt < log.Date)
                _WarningSeenAt = log.Date;
            else if (log.Type == LogType.Cleanup && _LastCleanup < log.Date)
                _LastCleanup = log.Date;
            else if (log.Type == LogType.Register)
                _RegistrationLog = log;
            else if (log is EntryLog entryLog && entryLog.EntryId.HasValue)
                AddEntryLog(entryLog);
        }
        private void Load()
        {
            if (File.Exists(PATH_LOG))
            {
                byte[] data = File.ReadAllBytes(PATH_LOG);
                AdvancedBitReader r = new AdvancedBitReader();
                r.FromArray(data);

                int unencryptedLogs = r.Read<int>();
                for (int i = 0; i < unencryptedLogs; i++)
                {
                    LogBase log = ReadLog(r);
                    _AllLogs.Add(log);

                    CheckLogType(log);

                }

                int encryptedBytes = r.Read<int>();
                _TempData = r.ReadBytes((ulong)encryptedBytes * 8UL);
            }
            LoadLastLogDate();
        }
        public void LoadEncrypted()
        {
            if (!File.Exists(PATH_LOG)) GotDecrypted = true;
            else if (!_GotDecrypted)
            {
                byte[] decrypted = _EncryptionModule.Decrypt(_TempData);
                _TempData = null;
                GotDecrypted = true;

                List<LogBase> logs = new List<LogBase>();
                AdvancedBitReader r = new AdvancedBitReader();
                r.FromArray(decrypted);
                int count = r.Read<int>();
                for (int i = 0; i < count; i++)
                {
                    LogBase log = ReadLog(r);
                    CheckLogType(log);

                    logs.Add(log);
                }

                _AllLogs.InsertRange(0, logs);

                if (logs.Count > 0)
                    _LastLog = logs[^1];

            }

            GenerateWarnings();
        }
        public void SeenWarnings()
        {
            DateTime now = DateTime.UtcNow;

            foreach(WarningBase warning in _Warnings)
            {
                if (warning is WarningLogRestored) _AllLogs.Add(new TamperLog(TamperType.LogRestored, now));
                else if (warning is WarningLogWiped) _AllLogs.Add(new TamperLog(TamperType.LogWiped, now));
            }


            _WarningSeenAt = now;
            _Warnings.Clear();

            if (_RegistrationLog == null)
            {
                _AllLogs.Add(new LogBase(LogType.WarningsSeen, now));
                LogBase reg = new LogBase(LogType.Register, now);
                CheckLogType(reg);
                AddLog(reg);

                FileInfo file = new FileInfo(PATH_LOG);
                file.CreationTimeUtc = now;
            }
            else
                AddLog(new LogBase(LogType.WarningsSeen, now));

            HasUnseenWarning = false;
        }
        private void Save()
        {
            byte[] final;
            if (_GotDecrypted)
            {
                AdvancedBitWriter w = new AdvancedBitWriter();
                w.Write(_AllLogs.Count);
                foreach(LogBase log in _AllLogs)
                    log.Save(w);

                w.Flush();
                byte[] encrypted = w.ToArray();
                encrypted = _EncryptionModule.Encrypt(encrypted);
                w.Dispose();

                w = new AdvancedBitWriter();
                w.Write(0);
                w.Write(encrypted.Length);
                w.WriteBytes(encrypted);

                w.Flush();
                final = w.ToArray();
                w.Dispose();
            }
            else
            {
                AdvancedBitWriter w = new AdvancedBitWriter();
                w.Write(_AllLogs.Count);
                foreach (LogBase log in _AllLogs)
                    log.Save(w);

                w.Write(_TempData.Length);
                w.WriteBytes(_TempData);

                w.Flush();
                final = w.ToArray();
                w.Dispose();
            }

            if (!Directory.Exists(PATH_DATA)) Directory.CreateDirectory(PATH_DATA);

            File.WriteAllBytes(PATH_LOG, final);
        }
        public void LogFailedLogin() => AddLog(new LogBase(LogType.FailedLogin, DateTime.UtcNow));
        public IEnumerable<LogBase> GetAll() => _AllLogs;
        public IEnumerable<EntryLog> GetAll(uint entryId)
        {
            if (_EntryLogs.TryGetValue(entryId, out List<EntryLog> list))
                return new List<EntryLog>(list);
            return new List<EntryLog>();
        }
        public IEnumerable<EntryLog> GetLast(uint entryId, int count)
        {
            if (_EntryLogs.TryGetValue(entryId, out List<EntryLog> list))
                return new List<EntryLog>(list.TakeLast(count));
            return new List<EntryLog>();
        }
        private void AddEntryLog(EntryLog log)
        {
            if (_EntryLogs.TryGetValue(log.EntryId.Value, out List<EntryLog> list))
                list.Add(log);
            else
            {
                List<EntryLog> logs = new List<EntryLog> { log };

                _EntryLogs.Add(log.EntryId.Value, logs);
            }
        }
        private void AddLog(LogBase log)
        {
            DateTime date = log.Date.ToUniversalTime();

            _AllLogs.Add(log);

            Save();

            FileInfo info = new FileInfo(PATH_LOG);
            info.LastWriteTimeUtc = date;
            if (log.Type == LogType.Register)
                info.CreationTimeUtc = date;

#if DEBUG
            if (!App.REQUIRE_ADMIN) return;
#endif
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\TNO\Pete", true))
            {
                key.SetValue("value", date.Ticks, RegistryValueKind.QWord);
                key.SetLastWrite(date);
            }
        }
        public DateTime Log(uint entryId, EntryLogType type)
        {
            DateTime now = DateTime.UtcNow;
            EntryLog log = new EntryLog(type, now, entryId);

            AddEntryLog(log);
            AddLog(log);

            return now;
        }
        public DateTime? GetLast(uint entryId, EntryLogType type)
        {
            if (!_EntryLogs.TryGetValue(entryId, out List<EntryLog> list))
                return null;

            for(int i = list.Count-1; i>=0; i--)
            {
                if (list[i].EntryType == type)
                    return list[i].Date;
            }

            return null;
        }
        public void GetLastAll(uint entryId, out DateTime? view, out DateTime? edit, out DateTime? create)
        {
            view = edit = create = null;
            if (!_EntryLogs.TryGetValue(entryId, out List<EntryLog> list))
                return;

            for(int i = list.Count-1;i>=0; i--)
            {
                if (view.HasValue & edit.HasValue & create.HasValue) return;

                DateTime time = list[i].Date;
                EntryLogType type = list[i].EntryType;

                if (type == EntryLogType.Create & !create.HasValue) create = time;
                if (type == EntryLogType.Edit & !edit.HasValue) edit = time;
                if (type == EntryLogType.View & !view.HasValue) view = time;
            }
        }
        public void GetLastAll(uint entryId, out DateTime? view, out DateTime? edit)
        {
            view = edit = null;
            if (!_EntryLogs.TryGetValue(entryId, out List<EntryLog> list))
                return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (view.HasValue & edit.HasValue) return;

                DateTime time = list[i].Date;
                EntryLogType type = list[i].EntryType;

                if (type == EntryLogType.Edit & !edit.HasValue) edit = time;
                if (type == EntryLogType.View & !view.HasValue) view = time;
            }
        }
        public void Log(LogType type)
        {
            DateTime now = DateTime.UtcNow;
            LogBase log = new LogBase(type, now);
            CheckLogType(log);
            AddLog(log);
        }
        #endregion
    }
}
