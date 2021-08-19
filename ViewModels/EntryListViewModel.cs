using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Pete.Services.Interfaces;
using Pete.Views;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;
using Pete.Models;
using System.Threading;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace Pete.ViewModels
{
    public class EntryListViewModel : BindableBase, INavigationAware
    {
        #region Private
        private ObservableCollection<CategoryViewModel> _Categories;
        private DelegateCommand _OpenDashboardCommand;
        private DelegateCommand _EditCategoryCommand;
        private DelegateCommand _DeleteCategoryCommand;
        private DelegateCommand<uint?> _ShowEntryCommand;
        private readonly ICategoryStore _CategoryStore;
        private readonly IRegionManager _RegionManager;
        private readonly IEntryStore _EntryStore;
        private readonly IDialogService _DialogService;
        private int _FilterCategoryIndex = 0;
        private ObservableCollection<EntryPreviewViewModel> _Entries = new ObservableCollection<EntryPreviewViewModel>();
        private string _FilterText;
        private bool _IsFiltering;
        private CategoryViewModel _SelectedCategory;
        private bool _ReactToSelectionChanged = true;
        private CancellationTokenSource _FilterTokenSource;
        private ManualResetEvent _FilterResetEvent;
        private Dispatcher _Dispatcher;
        private DelegateCommand<uint?> _ShowCategoryCommand;
        #endregion

        #region Properties
        public ReadOnlyObservableCollection<CategoryViewModel> Categories => new ReadOnlyObservableCollection<CategoryViewModel>(_Categories);
        public int FilterCategoryIndex { get => _FilterCategoryIndex; set => SetProperty(ref _FilterCategoryIndex, value, FilterCategoryChanged); }
        public DelegateCommand OpenDashboardCommand { get => _OpenDashboardCommand; private set => SetProperty(ref _OpenDashboardCommand, value); }
        public DelegateCommand EditCategoryCommand { get => _EditCategoryCommand; private set => SetProperty(ref _EditCategoryCommand, value); }
        public DelegateCommand DeleteCategoryCommand { get => _DeleteCategoryCommand; private set => SetProperty(ref _DeleteCategoryCommand, value); }
        public DelegateCommand<uint?> ShowEntryCommand { get => _ShowEntryCommand; private set => SetProperty(ref _ShowEntryCommand, value); }
        public DelegateCommand<uint?> ShowCategoryCommand { get => _ShowCategoryCommand; private set => SetProperty(ref _ShowCategoryCommand, value); }
        public ReadOnlyObservableCollection<EntryPreviewViewModel> Entries => new ReadOnlyObservableCollection<EntryPreviewViewModel>(_Entries);
        public string FilterText { get => _FilterText; private set => SetProperty(ref _FilterText, value); }
        public bool IsFiltering { get => _IsFiltering; private set => SetProperty(ref _IsFiltering, value); }
        #endregion
        public EntryListViewModel(ICategoryStore categoryStore, IRegionManager regionManager, IEntryStore entryStore, IDialogService dialogService)
        {
            _CategoryStore = categoryStore;
            _RegionManager = regionManager;
            _EntryStore = entryStore;
            _DialogService = dialogService;
            _Dispatcher = Dispatcher.CurrentDispatcher;

            _Categories = new ObservableCollection<CategoryViewModel>(categoryStore.Categories);
            _Categories.Insert(0, new CategoryViewModel(null, null, "<show all>"));

            OpenDashboardCommand = new DelegateCommand(() => _RegionManager.RequestNavigate(RegionNames.MainRegion, nameof(Dashboard), App.DebugNavigationCallback));

            EditCategoryCommand = new DelegateCommand(EditCategoryMethod, () => FilterCategoryIndex > 0).ObservesProperty(() => FilterCategoryIndex);
            DeleteCategoryCommand = new DelegateCommand(() => _DialogService.ConfirmRemove(DeleteSelectedCategory, "are you sure you want to remove this category? this action cannot be undone."),
                () => FilterCategoryIndex > 0).ObservesProperty(() => FilterCategoryIndex);

            _FilterResetEvent = new ManualResetEvent(true);

            FilterCategoryChanged();

            ShowEntryCommand = new DelegateCommand<uint?>(ShowEntryMethod);

            ShowCategoryCommand = new DelegateCommand<uint?>(index => { if (index.HasValue && index < _Categories.Count) 
                    FilterCategoryIndex = (int)index.Value; });

        }

        #region Events
        private void FilterCategoryChanged()
        {
            if (!_ReactToSelectionChanged) return;

            if (_FilterTokenSource != null)
                _FilterTokenSource.Cancel();

            Task.Run(PerformFilter);
        }
        private void PerformFilter()
        {
            if (_FilterResetEvent.WaitOne())
            {
                _FilterResetEvent.Reset();
                _FilterTokenSource = new CancellationTokenSource();
            }

            _SelectedCategory = _Categories[FilterCategoryIndex];
            uint? filterId = _SelectedCategory.ID;
            IsFiltering = filterId.HasValue;

            int totalCount = _EntryStore.Count;
            _Dispatcher.Invoke(_Entries.Clear);
            var temp = _EntryStore.GetAll(filterId, out int filteredCount);
            int i = 0;
            foreach(var t in temp)
            {

                if (_FilterTokenSource.Token.IsCancellationRequested)
                {
                    _FilterResetEvent.Set();
                    return;
                }

                _Dispatcher.Invoke(() => _Entries.Add(t));
                FilterText = $"collecting entries... {++i:n0}";

                Task.Delay(App.SLIDE_ANIMATION_INTERVAL).Wait();
            }


            string entry = totalCount == 1 ? "entry" : "entries";

            if (filterId.HasValue)
                FilterText = $"showing {filteredCount:n0} / {totalCount:n0} {entry}";
            else
                FilterText = $"showing all {totalCount:n0} {entry}";

            _FilterResetEvent.Set();
        }
        #endregion

        #region Methods
        private void EditCategoryMethod()
        {
            _DialogService.Input(EditCategoryCallback, "edit category", "please enter the new name you wish to use for this category.", "new category name",
                _SelectedCategory.Name, ValidateCategoryRename, ButtonResult.None, ButtonInfo.Cancel, new ButtonInfo(ButtonType.Primary, "rename", ButtonResult.Yes));
        }
        private void DeleteSelectedCategory()
        {
            _CategoryStore.RemoveCategory(_SelectedCategory.ID.Value);
            _ReactToSelectionChanged = false;
            _Categories.RemoveAt(_FilterCategoryIndex);
            _ReactToSelectionChanged = true;
            FilterCategoryIndex = 0;
        }
        private void EditCategoryCallback(ButtonResult result, string name)
        {
            if (result == ButtonResult.Yes)
            {
                Debug.Assert(_SelectedCategory.ID.HasValue);
                _CategoryStore.UpdateCategory(_SelectedCategory.ID.Value, name);
            }
        }
        private string ValidateCategoryRename(string newName)
        {
            newName = newName?.Trim();
            if (newName?.Length > 0)
            {
                if (newName == _SelectedCategory.Name)
                    return null;
                else if (_CategoryStore.Categories.Any(cat => cat.Name == newName))
                    return "this category already exists";
                return null;
            }
            return "category name must contain at least one character";
        }
        private void ShowEntryMethod(uint? id)
        {
            if (id.HasValue)
                _RegionManager.RequestNavigate(RegionNames.MainRegion, nameof(EntryEditor), App.DebugNavigationCallback, new NavigationParameters()
                {
                    {"id", id.Value }
                });
        }
        public bool IsNavigationTarget(NavigationContext navigationContext) => false;
        public void OnNavigatedFrom(NavigationContext navigationContext) { }
        public void OnNavigatedTo(NavigationContext navigationContext) { }
        #endregion
    }
}
