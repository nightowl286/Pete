using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Pete.Models.Logs;
using Pete.Services.Interfaces;
using Pete.ViewModels.Logs;
using Pete.Views;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

namespace Pete.ViewModels
{
    public class ActivityLogViewModel : BindableBase, INavigationAware
    {

        #region Consts
        private const int FILTER_FIELDS_NEEDED = 12;
        #endregion

        #region Private
        private readonly IActivityLog _ActivityLog;
        private readonly IRegionManager _RegionManager;
        private readonly IEntryStore _EntryStore;
        private readonly ICategoryStore _CategoryStore;
        private bool _FromDashboard;
        private bool _IsShowingFromEntry;
        private DelegateCommand _GoBackCommand;
        private NavigationParameters _GoBackParams;
        private DelegateCommand _ShowDashboardCommand;
        private ObservableCollection<bool> _Filters = new ObservableCollection<bool>();
        private string _FilterText;
        private string _LogCountText;
        private IEnumerable<LogBase> _AllLogs;
        private int _AllLogCount;
        private ObservableCollection<BaseLogViewModel> _DisplayLogs = new ObservableCollection<BaseLogViewModel>();
        private CancellationTokenSource _FilterTokenSource;
        private ManualResetEvent _FilterResetEvent;
        private Dispatcher _Dispatcher;
        #endregion
        #region Properties
        public bool FromDashboard { get => _FromDashboard; private set => SetProperty(ref _FromDashboard, value); }
        public bool IsShowingFromEntry { get => _IsShowingFromEntry; private set => SetProperty(ref _IsShowingFromEntry, value); }
        public DelegateCommand GoBackCommand { get => _GoBackCommand; private set => SetProperty(ref _GoBackCommand, value); }
        public DelegateCommand ShowDashboardCommand { get => _ShowDashboardCommand; private set => SetProperty(ref _ShowDashboardCommand, value); }
        public ObservableCollection<bool> Filters { get => _Filters; private set => SetProperty(ref _Filters, value); }
        public string FilterText { get => _FilterText; private set => SetProperty(ref _FilterText, value); }
        public string LogCountText { get => _LogCountText; private set => SetProperty(ref _LogCountText, value); }
        public ReadOnlyObservableCollection<BaseLogViewModel> DisplayLogs => new ReadOnlyObservableCollection<BaseLogViewModel>(_DisplayLogs);
        #endregion
        public ActivityLogViewModel(IActivityLog activityLog, IEntryStore entryStore, ICategoryStore categoryStore, IRegionManager regionManager)
        {
            _EntryStore = entryStore;
            _ActivityLog = activityLog;
            _RegionManager = regionManager;
            _CategoryStore = categoryStore;
            _Dispatcher = Dispatcher.CurrentDispatcher;

            ShowDashboardCommand = new DelegateCommand(() => _RegionManager.RequestNavigate(RegionNames.MainRegion, nameof(Dashboard), App.DebugNavigationCallback));

            InitFilters();

            _AllLogs = activityLog.GetAll().Reverse();
            _AllLogCount = _AllLogs.Count();
            LogCountText = $"showing all {_AllLogCount:n0} log entries";
            _FilterResetEvent = new ManualResetEvent(true);

            StartFilterLogs();
        }

        #region Methods
        private void InitFilters()
        {
            _Filters = new ObservableCollection<bool>();

            // change later to insert them from settings
            for (int i = 0; i < FILTER_FIELDS_NEEDED; i++)
                _Filters.Add(true);
            _Filters.CollectionChanged += _Filters_CollectionChanged;

            UpdateFilterText();
        }
        private void StartFilterLogs()
        {
            if (_FilterTokenSource != null)
                _FilterTokenSource.Cancel();

            Task.Run(FilterLogs);
        }
        private void FilterLogs()
        {
            if (_FilterResetEvent.WaitOne())
            {
                _FilterResetEvent.Reset();
                _FilterTokenSource = new CancellationTokenSource();
            }


            /* for a more refined app this should be reimplemented in a better more robust way,
               however for a small application like this, which would only receive one or two updates
               after being published, it is alright to keep it simple
            */
            bool login = _Filters[0], register = _Filters[1], cleanup = _Filters[2],
                eMissing = _Filters[3], eView = _Filters[4], eEdit = _Filters[5], eCreate = _Filters[6], eDelete = _Filters[7],
                failedLogin = _Filters[8], warningsSeen = _Filters[9],
                logWipe = _Filters[10], logRestore = _Filters[11];

            _Dispatcher.Invoke(_DisplayLogs.Clear);

            double interval = App.SLIDE_ANIMATION_INTERVAL;
            foreach (LogBase log in _AllLogs)
            {
                BaseLogViewModel toAdd = null;

                if (log is EntryLog entryLog)
                {
                    if (!eMissing & !entryLog.EntryId.HasValue) continue;
                    if (entryLog.EntryType == EntryLogType.View & !eView) continue;
                    if (entryLog.EntryType == EntryLogType.Edit & !eEdit) continue;
                    if (entryLog.EntryType == EntryLogType.Create & !eCreate) continue;

                    string typeSuffix = entryLog.EntryType == EntryLogType.Create ? "d" : "ed";
                    string type = entryLog.EntryType.ToString().ToLower();

                    string text = $"entry {type}{typeSuffix}";

                    toAdd = new EntryLogViewModel(entryLog.EntryId, _EntryStore, _CategoryStore, entryLog.Date, text);
                }
                else if ((log is EntryDeletedLog deletedLog) && eDelete)
                    toAdd = new EntryLogViewModel(deletedLog.EntryName, deletedLog.CategoryName, deletedLog.Date, "entry deleted");
                else if ((log.Type == LogType.Login & login) || (log.Type == LogType.Register & register))
                    toAdd = new BaseLogViewModel(log.Date, log.Type == LogType.Login ? "authorised login" : "first registration");
                else if (log.Type == LogType.FailedLogin & failedLogin)
                    toAdd = new BaseDangerousLogViewModel(log.Date, "failed login");

                if (_FilterTokenSource.Token.IsCancellationRequested)
                {
                    _FilterResetEvent.Set();
                    return;
                }

                if (toAdd != null)
                {
                    _Dispatcher.Invoke(() => _DisplayLogs.Add(toAdd));
                    LogCountText = $"collecting logs... {_DisplayLogs.Count:n0}";

                    Task.Delay((int)Math.Ceiling(interval)).Wait();
                    if (interval > App.SLIDE_ANIMATION_INTERVAL_MINIMUM)
                        interval -= App.SLIDE_ANIMATION_INTERVAL_DECREMENT;

                }
            }

            if (_AllLogCount == _DisplayLogs.Count)
                LogCountText = $"showing all {_AllLogCount:n0} log entries";
            else
                LogCountText = $"showing {_DisplayLogs.Count:n0} / {_AllLogCount:n0} log {(_AllLogCount == 1 ? "entry" : "entries")}";

            _FilterResetEvent.Set();
        }
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
        private void UpdateFilterText()
        {
            int enabled = _Filters.Count(f => f);

            int total = _Filters.Count;

            FilterText = enabled == total ? "all logs" : $"{enabled} / {total} log types";
        }
        #endregion

        #region Events
        private void _Filters_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                UpdateFilterText();
                StartFilterLogs();
            }
        }
        #endregion
    }
}
