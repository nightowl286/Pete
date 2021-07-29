using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Pete.Models;
using Pete.Services.Interfaces;
using Pete.ViewModels;
using TNO.BitUtilities;

namespace Pete.Services
{ 
    public class EntryStore : IEntryStore
    {
        #region Consts
        private const string PATH_DATA = "data";
        private const string PATH_ENTRIES = PATH_DATA + "\\data.bin";
        #endregion

        #region Private
        private readonly IIDManager _IDManager;
        private readonly IEncryptionModule _Encryption;
        private readonly ICategoryStore _CategoryStore;
        private readonly IActivityLog _ActivityLog;
        private Dictionary<uint, EntryInfo> _Entries;
        #endregion

        #region Properties
        public int Count => _Entries.Count;
        #endregion
        public EntryStore(IIDManager idManager, IEncryptionModule encryption, ICategoryStore categoryStore, IActivityLog activityLog)
        {
            _IDManager = idManager;
            _Encryption = encryption;
            _CategoryStore = categoryStore;
            _ActivityLog = activityLog;

            _CategoryStore.CategoryRemoved += _CategoryStore_CategoryRemoved;

            _Entries = new Dictionary<uint, EntryInfo>();

            LoadEntries();

        }

        #region Events
        private void _CategoryStore_CategoryRemoved(uint id)
        {
            bool changed = false;
            foreach(var pair in _Entries)
            {
                EntryInfo entry = pair.Value;
                if (entry.Category == id)
                {
                    entry.Category = null;
                    changed = true;
                }
            }
            if (changed)
                SaveEntries();
        }
        #endregion

        #region Methods
        public int CountMissingCategories(out int entriesAffected)
        {
            Dictionary<uint, bool> seen = new Dictionary<uint, bool>();
            int missing = entriesAffected = 0;
            foreach (var pair in _Entries)
            {
                EntryInfo entry = pair.Value;
                if (entry.Category.HasValue)
                {
                    if (!seen.TryGetValue(entry.Category.Value, out bool hasCategory))
                    {
                        hasCategory = _CategoryStore.Categories.Any(cat => cat.ID == entry.Category.Value);
                        seen.Add(entry.Category.Value, hasCategory);

                        if (!hasCategory)
                            missing++;
                    }

                    if (!hasCategory)
                        entriesAffected++;
                }
            }
            return missing;
        }
        public void PurgeMissingCategories()
        {
            Dictionary<uint,bool> seen = new Dictionary<uint, bool>();
            bool saveNeeded = false;
            foreach(var pair in _Entries)
            {
                EntryInfo entry = pair.Value;
                if (entry.Category.HasValue)
                {
                    if (!seen.TryGetValue(entry.Category.Value, out bool hasCategory))
                    {
                        hasCategory = _CategoryStore.Categories.Any(cat => cat.ID == entry.Category.Value);
                        seen.Add(entry.Category.Value, hasCategory);
                    }

                    if (!hasCategory)
                    {
                        entry.Category = null;
                        saveNeeded = true;
                    }
                }
            }
            if (saveNeeded) SaveEntries();
        }
        public IEnumerable<EntryPreviewViewModel> GetAll(uint? categoryId, out int count)
        {
            List<EntryPreviewViewModel> list = new List<EntryPreviewViewModel>();
            foreach(var pair in _Entries)
            {
                EntryInfo entry = pair.Value;
                if (categoryId == null || entry.Category == categoryId)
                {
                    CategoryViewModel category = entry.Category == null ? null : _CategoryStore.GetCategory(entry.Category.Value);

                    _ActivityLog.GetLastAll(pair.Key, out DateTime? view, out DateTime? edit);

                    EntryPreviewViewModel preview = new EntryPreviewViewModel(entry.ID, entry.Title, category, view, edit);
                    list.Add(preview);
                }
            }
            count = list.Count;
            return list;
        }
        public ReservationToken<uint> GetNewID() => _IDManager.ReserveNew();
        public void FreeToken(ReservationToken<uint> token) => _IDManager.Free(token);
        public bool IsNew(uint id) => _IDManager.IsReserved(id);
        private void CheckID(uint id)
        {
            if (!_Entries.ContainsKey(id))
                throw new ArgumentException($"No entry with the id '{id}' exists.", nameof(id));
        }
        public void GetInfo(uint id, out uint? category, out string title)
        {
            CheckID(id);
            EntryInfo entry = _Entries[id];
            category = entry.Category;
            title = entry.Title;
        }
        public byte[] GetData(uint id)
        {
            CheckID(id);
            return _Encryption.Decrypt(_Entries[id].EncryptedData);
        }
        public void AddEntry(ReservationToken<uint> id, uint? category, string title, byte[] data)
        {
            _IDManager.Take(id);
            _Entries.Add(id.Item, new EntryInfo(id.Item, title, category, _Encryption.Encrypt(data)));
            SaveEntries();
        }
        public void UpdateData(uint id, uint? category, string title, byte[] data)
        {
            EntryInfo newData = new EntryInfo(id, title, category, _Encryption.Encrypt(data));
            CheckID(id);
            _Entries[id] = newData;

            SaveEntries();
        }
        public void RemoveEntry(uint id)
        {
            CheckID(id);
            _Entries.Remove(id);
            _IDManager.Free(id);
            SaveEntries();
        }
        private void LoadEntries()
        {
            if (File.Exists(PATH_ENTRIES))
            {
                byte[] encryptedData = File.ReadAllBytes(PATH_ENTRIES);
                byte[] decryptedData = _Encryption.Decrypt(encryptedData);
                AdvancedBitReader r = new AdvancedBitReader();
                r.FromArray(decryptedData);
                int amount = r.Read<int>();
                for(int i = 0; i < amount;i++)
                {
                    uint id = r.Read<uint>();

                    uint? category = r.ReadBool() ? (uint?)r.Read<uint>() : null;

                    string title = r.ReadString(Encoding.UTF8);

                    int dataLength = r.Read<int>();
                    byte[] data = r.ReadBytes((ulong)dataLength * 8UL);
                    _Entries.Add(id, new EntryInfo(id, title, category, data));
                }

                _IDManager.Claim(_Entries.Keys);
            }
        }
        private void SaveEntries()
        {
            if (!Directory.Exists(PATH_DATA)) Directory.CreateDirectory(PATH_DATA);

            using AdvancedBitWriter w = new AdvancedBitWriter();

            w.Write(_Entries.Count);
            foreach (KeyValuePair<uint, EntryInfo> pair in _Entries)
            {
                EntryInfo entry = pair.Value;
                w.Write(entry.ID);
                w.WriteBool(entry.Category.HasValue);
                if (entry.Category.HasValue) w.Write(entry.Category.Value);

                w.WriteString(entry.Title, Encoding.UTF8);

                w.Write(entry.EncryptedData.Length);
                w.WriteBytes(entry.EncryptedData);
            }

            w.Flush();

            byte[] data = w.ToArray();
            byte[] encrypted = _Encryption.Encrypt(data);

            File.WriteAllBytes(PATH_ENTRIES, encrypted);
        }
        #endregion
    }
}
