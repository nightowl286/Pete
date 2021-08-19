using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Pete.Models;
using Pete.Models.Logs;
using Pete.Services.Interfaces;
using Pete.ViewModels.Logs;
using Pete.Views.Dialogs;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace Pete.ViewModels
{
    public class EntryEditorViewModel : BindableBase, INavigationAware
    {
        #region Consts
        private const int LAST_LOG_AMOUNT = 5;
        #endregion

        #region Private
        private uint _EntryId;
        private ReservationToken<uint> _ReservedToken;
        private string _Title = "";
        private string _TitleError;
        private string _Data = "";
        private bool _IsInEditMode;
        private bool _CanDelete = true;
        private ObservableCollection<CategoryViewModel> _Categories;
        private ObservableCollection<EditorEntryLogViewModel> _LastLogs = new ObservableCollection<EditorEntryLogViewModel>();
        private bool _ShowLastLogs;
        private int _SelectedCategoryIndex;
        private int _LastSelectedCategoryIndex;
        #region Dates
        private DateTime? _CreateDate;
        private DateTime? _ViewDate;
        private DateTime? _EditDate;
        private DateTime _OpenDate;
        #endregion
        #region Commands
        private DelegateCommand _StartEditCommand;
        private DelegateCommand _SaveEditCommand;
        private DelegateCommand _CancelEditCommand;
        private DelegateCommand _GoBackCommand;
        private DelegateCommand _DeleteEntryCommand;
        private DelegateCommand _ComboBoxSelection;
        #endregion
        private readonly IDialogService _DialogService;
        private readonly IEntryStore _EntryStore;
        private readonly ICategoryStore _CategoryStore;
        private readonly IActivityLog _ActivityLog;
        private readonly Dispatcher _Dispatcher;
        private bool _ReactToSelectionChanged = true;
        #endregion

        #region Properties
        public string Title { get => _Title; set => SetProperty(ref _Title, value, TitleEdited); }
        public string TitleError { get => _TitleError; private set => SetProperty(ref _TitleError, value); }
        public string Data { get => _Data; set => SetProperty(ref _Data, value); }
        public bool IsInEditMode { get => _IsInEditMode; private set => SetProperty(ref _IsInEditMode, value); }
        public bool CanDelete { get => _CanDelete; private set => SetProperty(ref _CanDelete, value); }
        public ReadOnlyObservableCollection<CategoryViewModel> Categories => new ReadOnlyObservableCollection<CategoryViewModel>(_Categories);
        public ReadOnlyObservableCollection<EditorEntryLogViewModel> LastLogs => new ReadOnlyObservableCollection<EditorEntryLogViewModel>(_LastLogs);
        public int SelectedCategoryIndex { get => _SelectedCategoryIndex; set => SetProperty(ref _SelectedCategoryIndex, value); }
        public bool ShowLastLogs { get => _ShowLastLogs; set => SetProperty(ref _ShowLastLogs, value); }
        #region Dates
        public DateTime? CreateDate { get => _CreateDate; private set => SetProperty(ref _CreateDate, value); }
        public DateTime? ViewDate { get => _ViewDate; private set => SetProperty(ref _ViewDate, value); }
        public DateTime? EditDate { get => _EditDate; private set => SetProperty(ref _EditDate, value); }
        #endregion
        #region Commands
        public DelegateCommand StartEditCommand { get => _StartEditCommand; private set => SetProperty(ref _StartEditCommand, value); }
        public DelegateCommand SaveEditCommand { get => _SaveEditCommand; private set => SetProperty(ref _SaveEditCommand, value); }
        public DelegateCommand CancelEditCommand { get => _CancelEditCommand; private set => SetProperty(ref _CancelEditCommand, value); }
        public DelegateCommand GoBackCommand { get => _GoBackCommand; private set => SetProperty(ref _GoBackCommand, value); }
        public DelegateCommand DeleteEntryCommand { get => _DeleteEntryCommand; private set => SetProperty(ref _DeleteEntryCommand, value); }
        public DelegateCommand ComboBoxSelection { get => _ComboBoxSelection; private set => SetProperty(ref _ComboBoxSelection, value); }
        #endregion
        #endregion
        public EntryEditorViewModel(IDialogService dialogService, IEntryStore entryStore, ICategoryStore categoryStore, IActivityLog activityLog)
        {
            _Dispatcher = Dispatcher.CurrentDispatcher;

            _DialogService = dialogService;
            _EntryStore = entryStore;
            _CategoryStore = categoryStore;
            _ActivityLog = activityLog;

            _Categories = new ObservableCollection<CategoryViewModel>(categoryStore.Categories);
            _Categories.Insert(0, new CategoryViewModel(null, null, "<uncategorised>"));
            _Categories.Add(new CategoryViewModel(null, null, "add new..."));

            StartEditCommand = new DelegateCommand(() => IsInEditMode = true);
            SaveEditCommand = new DelegateCommand(SaveCommandCallback, () => TitleError == null).ObservesProperty(() => TitleError);

            CancelEditCommand = new DelegateCommand(CancelEditCallback).ObservesCanExecute(() => IsInEditMode);

            DeleteEntryCommand = new DelegateCommand(() => _DialogService.ConfirmRemove(DeleteEntryConfirmation, "are you sure you want to remove this entry? this action cannot be undone."), () => CanDelete && !IsInEditMode).ObservesProperty(() => CanDelete).ObservesProperty(() => IsInEditMode);

            ComboBoxSelection = new DelegateCommand(CategorySelectedChanged).ObservesCanExecute(() => IsInEditMode);
        }

        #region Events
        private void CategorySelectedChanged()
        {
            if (SelectedCategoryIndex == Categories.Count - 1 && _ReactToSelectionChanged)
            {
                _DialogService.Input(NewCategoryCallback, "new category", "please enter the name for the new category. you will be able to change it later on.", "new category name", NewCategoryValidation, ButtonResult.None, ButtonInfo.Cancel, new ButtonInfo(ButtonType.Primary, "create", ButtonResult.Yes));
            }
            else
                _LastSelectedCategoryIndex = SelectedCategoryIndex;
        }
        private string NewCategoryValidation(string name)
        {
            name = name?.Trim();
            if (name?.Length == 0)
                return "a category name must contain at least one characters";
            else if (_CategoryStore.Categories.Any(cat => cat.Name == name))
                return "this category already exists";

            return null;
        }
        private void NewCategoryCallback(ButtonResult result, string name)
        {
            if (result == ButtonResult.Yes)
            {
                CategoryViewModel cat = _CategoryStore.AddCategory(name);
                _ReactToSelectionChanged = false;
                _Categories.Insert(_Categories.Count - 1, cat);
                _ReactToSelectionChanged = true;
                SelectedCategoryIndex = _Categories.Count - 2;
            }
            else
                SelectedCategoryIndex = _LastSelectedCategoryIndex;
        }
        private void CancelEditCallback()
        {
            ButtonInfo keepEditing = new ButtonInfo(ButtonType.Primary, "keep editing", ButtonResult.No);
            if (_ReservedToken == null)
            {
                _DialogService.Message(CancelEditResult, "cancel edit?",
                    "are you sure you want to cancel your edit? any changes will be reverted.", ButtonResult.No,
                    new ButtonInfo(ButtonType.Normal, "cancel edit", ButtonResult.Yes), keepEditing);
            }
            else if (string.IsNullOrWhiteSpace(Title) && string.IsNullOrWhiteSpace(Data))
            {
                _EntryStore.FreeToken(_ReservedToken);
                GoBackCommand?.Execute();
            }
            else
                _DialogService.Message(CancelEditResult, "remove new entry?",
                    "are you sure you want to cancel editing this new entry? this will also remove it.", ButtonResult.No,
                    new ButtonInfo(ButtonType.Normal, "remove entry", ButtonResult.Yes), keepEditing);
        }
        private void CancelEditResult(ButtonResult result)
        {
            if (result == ButtonResult.Yes)
            {
                if (_ReservedToken == null)
                {
                    IsInEditMode = false;
                    SetDataFromEntry();
                }
                else
                {
                    _EntryStore.FreeToken(_ReservedToken);
                    GoBackCommand?.Execute();
                }
            }
        }
        private void TitleEdited()
        {
            if (Title?.Length > 0)
                TitleError = null;
            else
                TitleError = "an entry must contain a title";
        }
        private void SaveCommandCallback()
        {
            byte[] data = Encoding.UTF8.GetBytes(Data ?? string.Empty);

            uint? selectedCategory = _Categories[SelectedCategoryIndex].ID;

            if (_ReservedToken == null)
                _EntryStore.UpdateData(_EntryId, selectedCategory, Title, data);
            else
            {
                _EntryStore.AddEntry(_ReservedToken, selectedCategory, Title, data);

                _EntryId = _ReservedToken.Item;
                _ReservedToken = null;
                CanDelete = true;
                CreateDate = _ActivityLog.Log(_EntryId, EntryLogType.Create);
            }
            IsInEditMode = false;


            EditDate = _OpenDate = _ActivityLog.Log(_EntryId, EntryLogType.Edit);

            UpdateRecentLogs();

        }
        private void DeleteEntryConfirmation()
        {
            _EntryStore.RemoveEntry(_EntryId);


            GoBackCommand.Execute();
        }
        #endregion

        #region Methods
        private void UpdateRecentLogs()
        {
            _LastLogs.Clear();
            foreach (var entry in _ActivityLog.GetLast(_EntryId, LAST_LOG_AMOUNT))
                _LastLogs.Insert(0, new EditorEntryLogViewModel(entry));

            ShowLastLogs = _LastLogs.Count > 0;
        }
        private void SelectCategory(uint? id)
        {
            for(int i = 0; i < _Categories.Count;i++)
            {
                if (_Categories[i].ID == id)
                {
                    SelectedCategoryIndex = i;
                    return;
                }
            }
        }
        private void SetDataFromEntry()
        {
            _EntryStore.GetInfo(_EntryId, out uint? category, out string title);
            SelectCategory(category);
            Title = title;

            Data = Encoding.UTF8.GetString(_EntryStore.GetData(_EntryId));
        }
        public bool IsNavigationTarget(NavigationContext navigationContext) => false;
        public void OnNavigatedFrom(NavigationContext navigationContext) { }
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            GoBackCommand = new DelegateCommand(navigationContext.NavigationService.Journal.GoBack);
            if (navigationContext.Parameters.TryGetValue("token", out ReservationToken<uint> token))
            {
                ShowLastLogs = false;
                _ReservedToken = token;
                if (_EntryStore.Count == 0)
                    Data = "use this area to write down any information you would like to save in this entry";
                CanDelete = false;
                IsInEditMode = true;
                TitleEdited();

                DateTime now = DateTime.UtcNow;

                ViewDate = EditDate = CreateDate = _OpenDate = now;
            }
            else if (navigationContext.Parameters.TryGetValue("id", out uint id))
            {
                _EntryId = id;
                _ActivityLog.GetLastAll(id, out var view, out var edit, out var create);

                UpdateRecentLogs();

                ViewDate = view;
                EditDate = edit;
                CreateDate = create;

                SetDataFromEntry();

                _ActivityLog.Log(id, EntryLogType.View);
            }
        }
        #endregion
    }
}
