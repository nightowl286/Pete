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
        private string _ViewDateString = "???";
        private string _EditDateString = "???";
        private string _ViewDateDisplay = "???";
        private string _EditDateDisplay = "???";
        private DateTime _OpenDate;
        #endregion
        #endregion

        #region Properties
        public uint ID { get => _ID; private set => SetProperty(ref _ID, value); }
        public string Title { get => _Title; private set => SetProperty(ref _Title, value); }
        public CategoryViewModel Category { get => _Category; private set => SetProperty(ref _Category, value); }
        public string CategoryName { get => _CategoryName; private set => SetProperty(ref _CategoryName, value); }
        #region Dates
        public DateTime? ViewDate { get => _ViewDate; private set => SetProperty(ref _ViewDate, value, ViewDateChanged); }
        public DateTime? EditDate { get => _EditDate; private set => SetProperty(ref _EditDate, value, EditDateChanged); }
        public string ViewDateString { get => _ViewDateString; private set => SetProperty(ref _ViewDateString, value); }
        public string EditDateString { get => _EditDateString; private set => SetProperty(ref _EditDateString, value); }
        public string ViewDateDisplay { get => _ViewDateDisplay; private set => SetProperty(ref _ViewDateDisplay, value); }
        public string EditDateDisplay { get => _EditDateDisplay; private set => SetProperty(ref _EditDateDisplay, value); }
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
        private void ViewDateChanged()
        {
            if (ViewDate.HasValue)
            {
                ViewDateDisplay = _OpenDate.Subtract(ViewDate.Value).BiggestUnit();
                ViewDateString = ViewDate.Value.ToLocalTime().ToString(DATE_FORMAT);
            }
            else
            {
                ViewDateDisplay = "???";
                ViewDateString = "???";
            }
        }
        private void EditDateChanged()
        {
            if (EditDate.HasValue)
            {
                EditDateDisplay = _OpenDate.Subtract(EditDate.Value).BiggestUnit();
                EditDateString = EditDate.Value.ToLocalTime().ToString(DATE_FORMAT);
            }
            else
            {
                EditDateDisplay = "???";
                EditDateString = "???";
            }
        }
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
