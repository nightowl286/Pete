using System;
using System.Collections.Generic;
using System.Linq;
using Pete.Services.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

namespace Pete.ViewModels
{
    public class ActivityLogViewModel : BindableBase, INavigationAware
    {
        #region Private
        private readonly IActivityLog _ActivityLog;
        private bool _FromDashboard;
        private DelegateCommand _GoBackCommand;
        private NavigationParameters _GoBackParams;
        private DelegateCommand _ShowDashboardCommand;
        private EntryPreviewViewModel _ShowingFromEntry;
        #endregion
        #region Properties
        public bool FromDashboard { get => _FromDashboard; private set => SetProperty(ref _FromDashboard, value); }
        public DelegateCommand GoBackCommand { get => _GoBackCommand; private set => SetProperty(ref _GoBackCommand, value); }
        public DelegateCommand ShowDashboardCommand { get => _ShowDashboardCommand; private set => SetProperty(ref _ShowDashboardCommand, value); }
        #endregion
        public ActivityLogViewModel(IActivityLog activityLog)
        {
            _ActivityLog = activityLog;
        }

        #region Methods
        public bool IsNavigationTarget(NavigationContext navigationContext) => false;
        public void OnNavigatedFrom(NavigationContext navigationContext) { }
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            GoBackCommand = new DelegateCommand(navigationContext.NavigationService.Journal.GoBack);
            if (navigationContext.Parameters.TryGetValue("from-dashboard", out bool fromDashboard))
                FromDashboard = fromDashboard;
            if (navigationContext.Parameters.TryGetValue("go-back-parameters", out NavigationParameters goBackParams))
                _GoBackParams = goBackParams;
        }
        #endregion
    }
}
