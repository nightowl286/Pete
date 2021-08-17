using System;
using System.Collections.Generic;
using System.Linq;
using Prism.Commands;
using Prism.Mvvm;
using Pete.Services.Interfaces;
using TNO.Pete.E2fa.UsbHasher;
using Prism.Regions;
using Pete.Models;
using Pete.Views;
using Prism.Services.Dialogs;
using System.ComponentModel;

namespace Pete.ViewModels
{
    public class DashboardViewModel : BindableBase, INavigationAware
    {
        #region Private
        private bool _ActivityWarning;
        private readonly IRegionManager _RegionManager;
        private readonly IEntryStore _EntryStore;
        private readonly IDialogService _DialogService;
        private readonly ICategoryStore _CategoryStore;
        private readonly IActivityLog _ActivityLog;
        private DelegateCommand _AddNewCommand;
        private DelegateCommand _ShowAllCommand;
        private DelegateCommand _LoadedCommand;
        private DelegateCommand _ActivityLogCommand;
        #endregion

        #region Properties
        public bool ActivityWarning { get => _ActivityWarning; private set => SetProperty(ref _ActivityWarning, value); }
        public DelegateCommand AddNewCommand { get => _AddNewCommand; private set => SetProperty(ref _AddNewCommand, value); }
        public DelegateCommand ShowAllCommand { get => _ShowAllCommand; private set => SetProperty(ref _ShowAllCommand, value); }
        public DelegateCommand LoadedCommand { get => _LoadedCommand; private set => SetProperty(ref _LoadedCommand, value); }
        public DelegateCommand ActivityLogCommand { get => _ActivityLogCommand; private set => SetProperty(ref _ActivityLogCommand, value); }
        #endregion
        public DashboardViewModel(IRegionManager regionManager, IEntryStore entryStore, IDialogService dialogService, ICategoryStore categoryStore, IActivityLog activityLog)
        {
            ActivityWarning = false;

            _ActivityLog = activityLog;
            _EntryStore = entryStore;
            _RegionManager = regionManager;
            _DialogService = dialogService;
            _CategoryStore = categoryStore;

            AddNewCommand = new DelegateCommand(AddNewCallback);
            ShowAllCommand = new DelegateCommand(() => NavigateTo(nameof(EntryList)));
            ActivityLogCommand = new DelegateCommand(() => NavigateTo(nameof(ActivityLog)));

            LoadedCommand = new DelegateCommand(CheckMissingCategories);

            activityLog.PropertyChanged += ActivityLog_PropertyChanged;
        }
        ~DashboardViewModel() => _ActivityLog.PropertyChanged -= ActivityLog_PropertyChanged;

        #region Events
        private void ActivityLog_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IActivityLog))
                ActivityWarning = _ActivityLog.HasUnseenWarning;
        }
        #endregion

        #region Methods
        private void CheckMissingCategories()
        {
            int missing = _EntryStore.CountMissingCategories(out int entriesAffected);
            if (missing > 0)
            {
                bool c = missing == 1;
                bool e = entriesAffected == 1;

                string message = c ? "a missing category was" : $"{missing:n0} missing categories were detected, you can restore the 'categories.bin' ";
                message += $" file from a backup and press retry to try and reload {(c ? "it" : "them")} now, or exit the app to try again later.\n\n"
                    + $" you can also purge the {(c ? "category" : $"{missing:n0} categories")} which will affect {entriesAffected:n0}"
                    + $" {(e ? "entry" : "entries")} and cause {(e ? "it" : "them")} to be uncategorised.";

                _DialogService.Message(MissingCategoriesCallback, $"purge {(missing == 1 ? "category" : "categories")}?", message, ButtonResult.Abort,
                    new ButtonInfo(ButtonType.Normal, "purge", ButtonResult.Yes),
                    new ButtonInfo(ButtonType.Cancel, "exit", ButtonResult.Abort),
                    new ButtonInfo(ButtonType.Primary, "retry", ButtonResult.Retry));
            }
        }
        private void MissingCategoriesCallback(ButtonResult result)
        {
            if (result == ButtonResult.Retry)
            {
                _CategoryStore.LoadCategories();
                CheckMissingCategories();
            }
            else if (result == ButtonResult.Yes)
                _EntryStore.PurgeMissingCategories();
            else
                App.Current.Shutdown();
        }
        private void NavigateTo(string name) => _RegionManager.RequestNavigate(RegionNames.MainRegion, name, App.DebugNavigationCallback);
        private void AddNewCallback()
        {
            _RegionManager.RequestNavigate(RegionNames.MainRegion, nameof(EntryEditor), App.DebugNavigationCallback, new NavigationParameters()
            {
                { "token", _EntryStore.GetNewID() }
            });
        }
        public void OnNavigatedTo(NavigationContext navigationContext) { }
        public bool IsNavigationTarget(NavigationContext navigationContext) => false;
        public void OnNavigatedFrom(NavigationContext navigationContext) { }
        #endregion
    }
}
