using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Pete.Services.Interfaces;
using Pete.ViewModels;
using Prism.Mvvm;
using TNO.BitUtilities;

namespace Pete.Services
{
    public class CategoryStore : BindableBase, ICategoryStore
    {
        #region Consts
        private const string PATH_DATA = "data";
        private const string PATH_CATEGORIES = PATH_DATA + "\\categories.bin";
        #endregion

        #region Events
        public event ICategoryStore.CategoryChangedHandler CategoryChanged;
        public event ICategoryStore.CategoryEventHandler CategoryRemoved;
        #endregion

        #region Private
        private ObservableCollection<CategoryViewModel> _Categories;
        private readonly IIDManager _IDManager;
        private readonly IEncryptionModule _Encryption;
        #endregion

        #region Properties
        public ReadOnlyObservableCollection<CategoryViewModel> Categories => new ReadOnlyObservableCollection<CategoryViewModel>(_Categories);
        #endregion
        public CategoryStore(IIDManager idManager, IEncryptionModule encryption)
        {
            _Categories = new ObservableCollection<CategoryViewModel>();

            _IDManager = idManager;
            _Encryption = encryption;

            LoadCategories();
        }

        #region Methods
        public void LoadCategories()
        {
            if (File.Exists(PATH_CATEGORIES))
            {
                byte[] encrypted = File.ReadAllBytes(PATH_CATEGORIES);
                byte[] data = _Encryption.Decrypt(encrypted);

                AdvancedBitReader r = new AdvancedBitReader();
                r.FromArray(data);

                List<uint> ids = new List<uint>();
                int amount = r.Read<int>();
                for (int i = 0; i < amount; i++)
                {
                    uint id = r.Read<uint>();
                    ids.Add(id);

                    string name = r.ReadString(Encoding.UTF8);

                    _Categories.Add(new CategoryViewModel(this, id, name));

                    Debug.WriteLine($"[CategoryStore] Loaded: #{id} - {name}");
                }

                _IDManager.Clear();
                _IDManager.Claim(ids);

            }
        }
        private void SaveCategories()
        {
            using AdvancedBitWriter w = new AdvancedBitWriter();

            w.Write(_Categories.Count);
            foreach(CategoryViewModel category in _Categories)
            {
                Debug.Assert(category.ID.HasValue);
                w.Write(category.ID.Value);
                w.WriteString(category.Name, Encoding.UTF8);
            }

            w.Flush();
            byte[] data = w.ToArray();
            byte[] encrypted = _Encryption.Encrypt(data);

            if (!Directory.Exists(PATH_DATA)) Directory.CreateDirectory(PATH_DATA);
            File.WriteAllBytes(PATH_CATEGORIES, encrypted);

        }
        public CategoryViewModel AddCategory(string name)
        {
            var token = _IDManager.ReserveNew();
            CategoryViewModel cat = new CategoryViewModel(this, token.Item, name);
            _IDManager.Take(token);

            _Categories.Add(cat);

            SaveCategories();

            Debug.WriteLine($"[CategoryStore] Added: #{token.Item} - {name}");

            return cat;
        }
        private void CheckID(uint id)
        {
            if (!_Categories.Any(c => c.ID == id))
                throw new ArgumentException($"A category with the id '{id}' does not exist.", nameof(id));
        }
        public void RemoveCategory(uint id)
        {
            CheckID(id);

            for (int i = _Categories.Count - 1; i >= 0; i--)
            {
                if (_Categories[i].ID == id)
                {

                    _Categories.RemoveAt(i);
                    _IDManager.Free(id);
                    break;
                }
            }

            SaveCategories();

            CategoryRemoved?.Invoke(id);
        }
        public void UpdateCategory(uint id, string name)
        {
            CheckID(id);

            CategoryChanged?.Invoke(id, name);

            SaveCategories();
        }
        public CategoryViewModel GetCategory(uint id)
        {
            CheckID(id);
            foreach(CategoryViewModel cat in _Categories)
            {
                if (cat.ID == id)
                    return cat;
            }

            throw new Exception("This should not be possible");
        }
        #endregion
    }
}
