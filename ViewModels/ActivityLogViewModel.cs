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
using Pete.ViewModels.Warnings;
using Pete.Models.Warnings;
using Pete.Services;
using System.Collections.Concurrent;
using Pete.Other;

namespace Pete.ViewModels
{
    [RegionMemberLifetime(KeepAlive = false)]
    public class ActivityLogViewModel : BindableBase, INavigationAware
    {

        #region Private
        private readonly IActivityLog _ActivityLog;
        private readonly IRegionManager _RegionManager;
        private readonly IEntryStore _EntryStore;
        private readonly ISettings _Settings;
        private readonly ICategoryStore _CategoryStore;
        private bool _FromDashboard;
        private uint? _ShowFromEntry;
        private DelegateCommand _GoBackCommand;
        private NavigationParameters _GoBackParams;
        private DelegateCommand _ShowDashboardCommand;
        private ObservableCollection<bool> _Filters = new ObservableCollection<bool>();
        private ObservableCollection<bool> _EntryFilters = new ObservableCollection<bool>();
        private string _FilterText;
        private string _LogCountText;
        private IEnumerable<LogBase> _AllLogs;
        private int _AllLogCount;
        private ObservableCollection<BaseLogViewModel> _DisplayLogs = new ObservableCollection<BaseLogViewModel>();
        private Dispatcher _Dispatcher;
        private bool _ShowWarnings;
        private ObservableCollection<WarningBaseViewModel> _Warnings = new ObservableCollection<WarningBaseViewModel>();
        private DelegateCommand _WarningsSeenCommand;
        private int _WarningCount;
        private bool _IsSubscribed = false;
        private ThreadedQueue _FilterThread;
        #endregion

        #region Properties
        public bool FromDashboard { get => _FromDashboard; private set => SetProperty(ref _FromDashboard, value); }
        public uint? ShowFromEntry { get => _ShowFromEntry; private set => SetProperty(ref _ShowFromEntry, value); }
        public DelegateCommand GoBackCommand { get => _GoBackCommand; private set => SetProperty(ref _GoBackCommand, value); }
        public DelegateCommand ShowDashboardCommand { get => _ShowDashboardCommand; private set => SetProperty(ref _ShowDashboardCommand, value); }
        public ObservableCollection<bool> Filters { get => _Filters; private set => SetProperty(ref _Filters, value); }
        public ObservableCollection<bool> EntryFilters { get => _EntryFilters; private set => SetProperty(ref _EntryFilters, value); }
        public string FilterText { get => _FilterText; private set => SetProperty(ref _FilterText, value); }
        public string LogCountText { get => _LogCountText; private set => SetProperty(ref _LogCountText, value); }
        public ReadOnlyObservableCollection<BaseLogViewModel> DisplayLogs => new ReadOnlyObservableCollection<BaseLogViewModel>(_DisplayLogs);
        public bool ShowWarnings { get => _ShowWarnings; private set => SetProperty(ref _ShowWarnings, value); }
        public ReadOnlyObservableCollection<WarningBaseViewModel> Warnings => new ReadOnlyObservableCollection<WarningBaseViewModel>(_Warnings);
        public DelegateCommand WarningsSeenCommand { get => _WarningsSeenCommand; private set => SetProperty(ref _WarningsSeenCommand, value); }
        public int WarningCount { get => _WarningCount; private set => SetProperty(ref _WarningCount, value); }
        #endregion
        public ActivityLogViewModel(IActivityLog activityLog, IEntryStore entryStore, ICategoryStore categoryStore, IRegionManager regionManager, ISettings settings)
        {
            _EntryStore = entryStore;
            _ActivityLog = activityLog;
            _RegionManager = regionManager;
            _CategoryStore = categoryStore;
            _Settings = settings;

            _Dispatcher = Dispatcher.CurrentDispatcher;
            Debug.WriteLine("");

            ShowDashboardCommand = new DelegateCommand(() => _RegionManager.RequestNavigate(RegionNames.MainRegion, nameof(Dashboard), App.DebugNavigationCallback));


            Filters = _Settings.LogFilters;
            EntryFilters = _Settings.LogEntryFilters;

            Subscribe();

            WarningsSeenCommand = new DelegateCommand(WarningsSeen).ObservesCanExecute(() => ShowWarnings);

            foreach (WarningBase warning in _ActivityLog.Warnings)
            {
                if (warning is WarningLogWiped) _Warnings.Add(new WarningBaseViewModel("log has been wiped"));
                else if (warning is WarningLogRestored) _Warnings.Add(new WarningBaseViewModel("log has been tampered with"));
                else if (warning is WarningFailedLogin failed) _Warnings.Add(new WarningFailedLoginViewModel("a failed login attempt", failed.At));
                else if (warning is WarningFailedLoginGroup failedGroup)
                {
                    WarningFailedLoginViewModel[] failedVms = new WarningFailedLoginViewModel[failedGroup.Attempts.Length];
                    for (int i = 0; i < failedVms.Length; i++)
                        failedVms[i] = new WarningFailedLoginViewModel("failed login attempt", failedGroup.Attempts[i].At);
                    _Warnings.Add(new WarningFailedLoginGroupViewModel($"{failedVms.Length} failed login attempt{(failedVms.Length == 1 ? "" : "s")}", failedVms));
                    WarningCount += failedVms.Length -1;
                }
                WarningCount++;
            }

            ShowWarnings = _ActivityLog.HasUnseenWarning;
        }
        ~ActivityLogViewModel() => Unsubscribe();

        #region Methods
        private void Subscribe()
        {
            if (!_IsSubscribed)
            {
                Filters.CollectionChanged += Filters_CollectionChanged;
                EntryFilters.CollectionChanged += Filters_CollectionChanged;

                _FilterThread = new ThreadedQueue();

                _IsSubscribed = true;
            }
        }
        private void Unsubscribe()
        {
            if (_IsSubscribed)
            {
                Filters.CollectionChanged -= Filters_CollectionChanged;
                EntryFilters.CollectionChanged -= Filters_CollectionChanged;
                _IsSubscribed = false;
                _FilterThread.CancelAll();
                _FilterThread = null;
            }
        }
        private void InitalLogSetup()
        {
            _AllLogs = ShowFromEntry.HasValue ? _ActivityLog.GetAll(ShowFromEntry.Value) : _ActivityLog.GetAll();
            _AllLogCount = _AllLogs.Count();

            _AllLogs = _AllLogs.Reverse();

            LogCountText = $"showing all {_AllLogCount:n0} log entries";
            StartFilterLogs();
        }
        private void WarningsSeen()
        {
            _ActivityLog.SeenWarnings();
            InitalLogSetup();
            ShowWarnings = false;
            _Warnings.Clear();
        }
        private void StartFilterLogs()
        {
            _FilterThread.CancelAllThenSchedule(FilterLogs);
        }
        private async Task FilterLogs(CancellationToken cancelToken)
        {
            Debug.WriteLine($"[FilterLogs] started | token {cancelToken.GetHashCode():x2}");
            /* for a more refined app this should be reimplemented in a better more robust way,
               however for a small application like this, which would only receive one or two updates
               after being published, it is alright to keep it simple
            */

            if (cancelToken.IsCancellationRequested)
                return;

            #region Get filters
            bool login = _Filters[0], register = _Filters[1], cleanup = _Filters[2],
                eMissing = _Filters[3], eView = _Filters[4], eEdit = _Filters[5], eCreate = _Filters[6], eDelete = _Filters[7],
                failedLogin = _Filters[8], warningsSeen = _Filters[9],
                logWipe = _Filters[10], logRestore = _Filters[11];

            if (ShowFromEntry.HasValue)
            {
                eView = _EntryFilters[0];
                eEdit = _EntryFilters[1];
                eCreate = _EntryFilters[2];
            }
            #endregion

            _Dispatcher.Invoke(_DisplayLogs.Clear);

            double interval = App.SLIDE_ANIMATION_INTERVAL;
            Task.Delay(App.SLIDE_ANIMATION_INTERVAL).Wait();

            int logNum = 0;
            foreach (LogBase log in _AllLogs)
            {
                logNum++;
                BaseLogViewModel toAdd = null;

                #region Filter log type
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
                else if (log.Type == LogType.Login & login)
                    toAdd = new BaseLogViewModel(log.Date, "authorised login");
                else if (log.Type == LogType.Register & register)
                    toAdd = new BaseLogViewModel(log.Date, logNum == _AllLogCount ? "first registration" : "late registration");
                else if (log.Type == LogType.FailedLogin & failedLogin)
                    toAdd = new BaseDangerousLogViewModel(log.Date, "failed login");
                else if (log.Type == LogType.WarningsSeen & warningsSeen)
                    toAdd = new BaseLogViewModel(log.Date, "warnings marked as seen");
                else if (log is TamperLog tamper)
                {
                    if (tamper.TamperType == TamperType.LogWiped & logWipe)
                        toAdd = new BaseDangerousLogViewModel(tamper.Date, "log has been wiped");
                    else if (tamper.TamperType == TamperType.LogRestored & logRestore)
                        toAdd = new BaseDangerousLogViewModel(tamper.Date, "log has been tampered with");
                }
                #endregion

                if (cancelToken.IsCancellationRequested)
                    return;

                if (toAdd != null)
                {
                    _Dispatcher.Invoke(() => _DisplayLogs.Add(toAdd));
                    LogCountText = $"collecting logs... {_DisplayLogs.Count:n0}";

                    await Task.Delay((int)Math.Ceiling(interval));
                    if (interval > App.SLIDE_ANIMATION_INTERVAL_MINIMUM)
                        interval -= App.SLIDE_ANIMATION_INTERVAL_DECREMENT;

                }
            }

            #region Update text
            if (ShowFromEntry.HasValue)
            {
                string log = "log" + (_DisplayLogs.Count == 1 ? "" : "s");

                if (_AllLogCount == _DisplayLogs.Count)
                    LogCountText = $"showing all {_AllLogCount:n0} entry {log}";
                else
                    LogCountText = $"showing {_DisplayLogs.Count:n0} / {_AllLogCount:n0} entry {log}";
            }
            else
            {
                if (_AllLogCount == _DisplayLogs.Count)
                    LogCountText = $"showing all {_AllLogCount:n0} log entries";
                else
                    LogCountText = $"showing {_DisplayLogs.Count:n0} / {_AllLogCount:n0} log {(_AllLogCount == 1 ? "entry" : "entries")}";
            }
            #endregion

            Debug.WriteLine($"[FilterLogs] ended natural | token {cancelToken.GetHashCode():x2}");
        }
        public bool IsNavigationTarget(NavigationContext navigationContext) => false;
        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            Unsubscribe();
            _DisplayLogs.Clear();
            _DisplayLogs = null;
            _AllLogs = null;

        }
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            Subscribe();

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

            if (navigationContext.Parameters.TryGetValue("show-for-entry", out uint entryId))
                ShowFromEntry = entryId;

            if (!_ActivityLog.HasUnseenWarning)
                InitalLogSetup();
            UpdateFilterText();
        }
        private void UpdateFilterText()
        {
            ObservableCollection<bool> collection = _ShowFromEntry.HasValue ? _EntryFilters : _Filters;

            int enabled = collection.Count(f => f);

            int total = collection.Count;

            FilterText = enabled == total ? "all logs" : $"{enabled} / {total} log types";
        }
        #endregion

        #region Events
        private void Filters_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                Debug.WriteLine($"Filter requirements changed {e.Action} {e.OldStartingIndex} {e.NewStartingIndex} | {DateTime.UtcNow}");
                UpdateFilterText();
                StartFilterLogs();
            }
        }
        #endregion
    }
}
