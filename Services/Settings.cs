using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using Pete.Services.Interfaces;
using Prism.Mvvm;
using TNO.BitUtilities;
using TNO.Json.Parsing;
using TNO.Json.Runtime;

namespace Pete.Services
{
    public class Settings : BindableBase, ISettings
    {
        #region Consts
        private const int DEF_ITER = ISettings.DEF_ITER;
        private const int DEF_SALT = ISettings.DEF_SALT;
        private const string PATH_DATA = "data";
        private const string PATH_SETTINGS = PATH_DATA + "\\settings.bin";
        private const int FILTER_FIELDS= 12;
        private const int FILTER_ENTRY_FIELDS = 3;
        #endregion

        #region Private
        private bool _Loaded = false;
        private bool _ShowEntryListAtStart;
        private int _Iterations = DEF_ITER;
        private int _SaltSize = DEF_SALT;
        private ObservableCollection<bool> _LogFilters = new ObservableCollection<bool>();
        private ObservableCollection<bool> _LogEntryFilters = new ObservableCollection<bool>();
        #endregion

        #region Properties
        public bool ShowEntryListAtStart { get => _ShowEntryListAtStart; set => SetProperty(ref _ShowEntryListAtStart, value); }
        public int Iterations { get => _Iterations; set => SetProperty(ref _Iterations, Math.Max(value, DEF_ITER)); }
        public int SaltSize { get => _SaltSize; set => SetProperty(ref _SaltSize, Math.Max(value, DEF_SALT)); }
        public IEncryptionModule Encryption { get; set; }
        public ObservableCollection<bool> LogFilters { get => _LogFilters; private set => SetProperty(ref _LogFilters, value); }
        public ObservableCollection<bool> LogEntryFilters { get => _LogEntryFilters; private set => SetProperty(ref _LogEntryFilters, value); }
        #endregion

        #region Methods
        public void Load()
        {
            if (_Loaded) return;

            if (File.Exists(PATH_SETTINGS))
            {
                byte[] encrypted = File.ReadAllBytes(PATH_SETTINGS);
                byte[] data = Encryption.Decrypt(encrypted);

                AdvancedBitReader r = new AdvancedBitReader();
                r.FromArray(data);

                byte version = r.Read<byte>();
                if (version == 0) LoadVersion0(r);
                
            }
            else
                SetDefaultFilters();

            LogFilters.CollectionChanged += FilterCollectionChanged;
            LogEntryFilters.CollectionChanged += FilterCollectionChanged;

            _Loaded = true;

        }
        private void FilterCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => Save();
        public void Save()
        {
            byte[] data;
            using (AdvancedBitWriter w = new AdvancedBitWriter())
            {
                w.Write<byte>(0); // settings version

                w.Write(Iterations);
                w.Write(SaltSize);

                Save(w, LogFilters);
                Save(w, LogEntryFilters);

                w.WriteBool(ShowEntryListAtStart);

                w.Flush();
                data = w.ToArray();
            }
            byte[] encrypted = Encryption.Encrypt(data);

            if (!Directory.Exists(PATH_DATA)) Directory.CreateDirectory(PATH_DATA);
            File.WriteAllBytes(PATH_SETTINGS, encrypted);
        }
        public void RestoreDefault()
        {
            Iterations = DEF_ITER;
            SaltSize = DEF_ITER;
            ShowEntryListAtStart = false;
            Save();
        }
        private void SetDefaultFilters()
        {
            LogFilters = new ObservableCollection<bool>(Enumerable.Repeat(true, FILTER_FIELDS));
            LogEntryFilters = new ObservableCollection<bool>(Enumerable.Repeat(true, FILTER_ENTRY_FIELDS));
        }
        #endregion

        #region Versioned loading and saving
        private void LoadVersion0(AdvancedBitReader r)
        {
            Iterations = r.Read<int>();
            SaltSize = r.Read<int>();


            ReadFor(r, LogFilters, FILTER_FIELDS);
            ReadFor(r, LogEntryFilters, FILTER_ENTRY_FIELDS);

            ShowEntryListAtStart = r.ReadBool();
        }
        #endregion

        #region Functions
        private static void ReadFor(IAdvancedBitReader r, ObservableCollection<bool> collection, int needs)
        {
            int amount = r.Read<byte>();
            for (int i = 0; i < amount; i++)
            {
                if (i < needs)
                    collection.Add(r.ReadBool());
                else
                    r.Skip(1); // only 1 bit is needed for the bool
            }

            while (collection.Count < needs)
                collection.Add(true);
        }
        private static void Save(IAdvancedBitWriter w, ObservableCollection<bool> collection)
        {
            w.Write((byte)collection.Count);
            foreach (bool val in collection)
                w.WriteBool(val);
        }
        #endregion
    }
}
