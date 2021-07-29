using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Pete.Services.Interfaces;
using Prism.Commands;
using Prism.Mvvm;

namespace Pete.ViewModels
{
    [DebuggerDisplay("{Name}, {ID}")]
    public class CategoryViewModel : BindableBase
    {
        #region Private
        private readonly ICategoryStore _CategoryStore;
        private uint? _ID;
        private string _Name;
        #endregion

        #region Properties
        public uint? ID { get => _ID; private set => SetProperty(ref _ID, value); }
        public string Name { get => _Name; private set => SetProperty(ref _Name, value); }
        public ICategoryStore CategoryStore => _CategoryStore;
        #endregion
        public CategoryViewModel(ICategoryStore categoryStore, uint? id, string name)
        {
            _CategoryStore = categoryStore;
            ID = id;
            Name = name;

            if (_CategoryStore != null)
                _CategoryStore.CategoryChanged += CategoryStore_CategoryChanged;
        }
        ~CategoryViewModel()
        {
            if (_CategoryStore != null)
                _CategoryStore.CategoryChanged -= CategoryStore_CategoryChanged;
        }

        #region Events
        private void CategoryStore_CategoryChanged(uint id, string name)
        {
            if (id == ID)
                Name = name;
        }
        #endregion
    }
}
