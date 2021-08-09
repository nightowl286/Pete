using System;
using System.Collections.Generic;
using System.Linq;
using Pete.Services.Interfaces;
using Prism.Commands;
using Prism.Mvvm;

namespace Pete.ViewModels
{
    public class EntryPreviewViewModel : BindableBase
    {
        #region Consts
        private const string DATE_FORMAT = "dd/MM/yyyy hh:mm:ss";
        #endregion

        #region Private
        private uint _ID;
        private string _Title;
        private CategoryViewModel _Category;
        private string _CategoryName;
        #region Dates
        private DateTime? _ViewDate;
        private DateTime? _EditDate;
        private DateTime _OpenDate;
        #endregion
        #endregion

        #region Properties
        public uint ID { get => _ID; private set => SetProperty(ref _ID, value); }
        public string Title { get => _Title; private set => SetProperty(ref _Title, value); }
        public CategoryViewModel Category { get => _Category; private set => SetProperty(ref _Category, value); }
        public string CategoryName { get => _CategoryName; private set => SetProperty(ref _CategoryName, value); }
        #region Dates
        public DateTime? ViewDate { get => _ViewDate; private set => SetProperty(ref _ViewDate, value); }
        public DateTime? EditDate { get => _EditDate; private set => SetProperty(ref _EditDate, value); }
        #endregion
        #endregion
        public EntryPreviewViewModel(uint id, string title, CategoryViewModel category, DateTime? viewDate, DateTime? editDate)
        {
            _OpenDate = DateTime.UtcNow;
            ViewDate = viewDate;
            EditDate = editDate;

            ID = id;
            Title = title;
            Category = category;

            if (category != null)
            {
                category.CategoryStore.CategoryChanged += CategoryStore_CategoryChanged;
                category.CategoryStore.CategoryRemoved += CategoryStore_CategoryRemoved;
                CategoryName = category.Name;
            }
        }
        ~EntryPreviewViewModel()
        {
            if (Category != null)
            {
                Category.CategoryStore.CategoryChanged -= CategoryStore_CategoryChanged;
                Category.CategoryStore.CategoryRemoved -= CategoryStore_CategoryRemoved;
            }
        }

        #region Events
        private void CategoryStore_CategoryRemoved(uint id)
        {
            Category = null;
            CategoryName = null;
        }
        private void CategoryStore_CategoryChanged(uint id, string name)
        {
            if (Category?.ID == id)
                CategoryName = name;
        }
        #endregion
    }
}
