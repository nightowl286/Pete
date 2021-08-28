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
using Pete.Other;

namespace Pete.ViewModels
{
    [RegionMemberLifetime(KeepAlive = false)]
    public class EntryListViewModel : BindableBase, INavigationAware
    {
        #region Private
        private ObservableCollection<CategoryViewModel> _Categories;
        private DelegateCommand _OpenDashboardCommand;
        private DelegateCommand _EditCategoryCommand;
        private DelegateCommand _DeleteCategoryCommand;
        private DelegateCommand _AddNewCommand;
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
        private Dispatcher _Dispatcher;
        private DelegateCommand<uint?> _ShowCategoryCommand;
        private ThreadedQueue _DisplayThread;
        #endregion

        #region Properties
        public ReadOnlyObservableCollection<CategoryViewModel> Categories => new ReadOnlyObservableCollection<CategoryViewModel>(_Categories);
        public int FilterCategoryIndex { get => _FilterCategoryIndex; set => SetProperty(ref _FilterCategoryIndex, value, FilterCategoryChanged); }
        public DelegateCommand AddNewCommand { get => _AddNewCommand; private set => SetProperty(ref _AddNewCommand, value); }
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

            AddNewCommand = new DelegateCommand(AddNewCommandPayload);
            OpenDashboardCommand = new DelegateCommand(() => _RegionManager.RequestNavigate(RegionNames.MainRegion, nameof(Dashboard), App.DebugNavigationCallback));

            EditCategoryCommand = new DelegateCommand(EditCategoryMethod, () => FilterCategoryIndex > 0).ObservesProperty(() => FilterCategoryIndex);
            DeleteCategoryCommand = new DelegateCommand(() => _DialogService.ConfirmRemove(DeleteSelectedCategory, "are you sure you want to remove this category? this action cannot be undone."),
                () => FilterCategoryIndex > 0).ObservesProperty(() => FilterCategoryIndex);

            _DisplayThread = new ThreadedQueue();

            FilterCategoryChanged();

            ShowEntryCommand = new DelegateCommand<uint?>(ShowEntryMethod);

            ShowCategoryCommand = new DelegateCommand<uint?>(index => { if (index.HasValue && index < _Categories.Count) 
                    FilterCategoryIndex = (int)index.Value; });

        }

        #region Events
        private void FilterCategoryChanged()
        {
            if (!_ReactToSelectionChanged || _FilterCategoryIndex == -1) return;


            _DisplayThread.CancelAllThenSchedule(PerformFilter);
        }
        #endregion

        #region Methods
        private void AddNewCommandPayload()
        {
            _RegionManager.RequestNavigate(RegionNames.MainRegion, nameof(EntryEditor), App.DebugNavigationCallback, new NavigationParameters()
            {

                {"token",_EntryStore.GetNewID() }
            });
        }
        private async Task PerformFilter(CancellationToken cancelToken)
        {
            if (cancelToken.IsCancellationRequested) return;

            _SelectedCategory = _Categories[FilterCategoryIndex];
            uint? filterId = _SelectedCategory.ID;
            IsFiltering = filterId.HasValue;

            int totalCount = _EntryStore.Count;
            _Dispatcher.Invoke(_Entries.Clear);
            var temp = _EntryStore.GetAll(filterId, out int filteredCount);
            int i = 0;
            double interval = App.SLIDE_ANIMATION_INTERVAL;
            foreach(var t in temp)
            {

                if (cancelToken.IsCancellationRequested) return;

                _Dispatcher.Invoke(() => _Entries.Add(t));
                FilterText = $"collecting entries... {++i:n0}";

                await Task.Delay((int)Math.Ceiling(interval));
                if (interval > App.SLIDE_ANIMATION_INTERVAL_MINIMUM)
                    interval -= App.SLIDE_ANIMATION_INTERVAL_DECREMENT;
            }


            string entry = totalCount == 1 ? "entry" : "entries";

            if (filterId.HasValue)
                FilterText = $"showing {filteredCount:n0} / {totalCount:n0} {entry}";
            else if (totalCount == 1)
                FilterText = "showing the only entry";
            else if (totalCount == 0)
                FilterText = "you have no entries";
            else
                FilterText = $"showing all {totalCount:n0} entries";

        }
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
        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            _DisplayThread.CancelAll();
            _DisplayThread = null;
            _Categories.Clear();
            _Categories = null;
            _Entries.Clear();
            _Entries = null;
        }
        public void OnNavigatedTo(NavigationContext navigationContext) { }
        #endregion
    }
}
