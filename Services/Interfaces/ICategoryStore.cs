using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using Pete.ViewModels;

namespace Pete.Services.Interfaces
{
    public interface ICategoryStore : INotifyPropertyChanged
    {
        #region Events
        delegate void CategoryChangedHandler(uint id, string name);
        delegate void CategoryEventHandler(uint id);
        event CategoryChangedHandler CategoryChanged;
        event CategoryEventHandler CategoryRemoved;
        #endregion

        #region Properties
        ReadOnlyObservableCollection<CategoryViewModel> Categories { get; }
        #endregion

        #region Methods
        void LoadCategories();
        void UpdateCategory(uint id, string name);
        CategoryViewModel AddCategory(string name);
        CategoryViewModel GetCategory(uint id);
        void RemoveCategory(uint id);
        #endregion
    }
}
