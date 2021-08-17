using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Pete.Services.Interfaces;
using Pete.Views;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

namespace Pete.ViewModels
{
    public class ActivityLogViewModel : BindableBase, INavigationAware
    {
        #region Private
        private readonly IActivityLog _ActivityLog;
        private readonly IRegionManager _RegionManager;
        private bool _FromDashboard;
        private bool _IsShowingFromEntry;
        private DelegateCommand _GoBackCommand;
        private NavigationParameters _GoBackParams;
        private DelegateCommand _ShowDashboardCommand;
        #endregion
        #region Properties
        public bool FromDashboard { get => _FromDashboard; private set => SetProperty(ref _FromDashboard, value); }
        public bool IsShowingFromEntry { get => _IsShowingFromEntry; private set => SetProperty(ref _IsShowingFromEntry, value); }
        public DelegateCommand GoBackCommand { get => _GoBackCommand; private set => SetProperty(ref _GoBackCommand, value); }
        public DelegateCommand ShowDashboardCommand { get => _ShowDashboardCommand; private set => SetProperty(ref _ShowDashboardCommand, value); }
        #endregion
        public ActivityLogViewModel(IActivityLog activityLog, IRegionManager regionManager)
        {
            _ActivityLog = activityLog;
            _RegionManager = regionManager;
            ShowDashboardCommand = new DelegateCommand(() => _RegionManager.RequestNavigate(RegionNames.MainRegion, nameof(Dashboard), App.DebugNavigationCallback));
        }

        #region Methods
        public bool IsNavigationTarget(NavigationContext navigationContext) => false;
        public void OnNavigatedFrom(NavigationContext navigationContext) { }
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            GoBackCommand = new DelegateCommand(navigationContext.NavigationService.Journal.GoBack);

            /*  Improve this with a better prism approach later, for now it is fine to use reflection.
                Speed is not important in this case however for any future proofing it should be improved.
            */
            if (navigationContext.NavigationService.Journal is RegionNavigationJournal journal && journal.CanGoBack)
            {
                object value = typeof(RegionNavigationJournal).GetField("backStack", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(journal);
                FromDashboard = (value as Stack<IRegionNavigationJournalEntry>).Peek().Uri.OriginalString == nameof(Dashboard);
            }
            else
                FromDashboard = false;

            if (navigationContext.Parameters.TryGetValue("go-back-parameters", out NavigationParameters goBackParams))
                _GoBackParams = goBackParams;
        }
        #endregion
    }
}
