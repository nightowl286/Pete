using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using WorkTask = System.Threading.Tasks.Task;

namespace Pete.ViewModels
{
    public class TaskScreenViewModel : BindableBase, INavigationAware, IJournalAware
    {
        #region Private
        private readonly IRegionManager _RegionManager;
        private string _Target;
        private Action _Task;
        private Action<Action<string>> _TaskUpdate;
        private string _Status;
        private Dispatcher _Dispatcher;
        private NavigationParameters _TargetParameters;
        private string _RegionName;
        #endregion

        #region Properties
        public string Target { get => _Target; private set => SetProperty(ref _Target, value); }
        public string Status { get => _Status; private set => SetProperty(ref _Status, value); }
        public string RegionName { get => _RegionName; private set => SetProperty(ref _RegionName, value); }
        public Action Task { get => _Task; private set => SetProperty(ref _Task, value); }
        public Action<Action<string>> TaskUpdate { get => _TaskUpdate; private set => SetProperty(ref _TaskUpdate, value); }
        public NavigationParameters TargetParameters { get => _TargetParameters; private set => SetProperty(ref _TargetParameters, value); }
        #endregion

        public TaskScreenViewModel(IRegionManager regionManager)
        {
            _RegionManager = regionManager;
            _Dispatcher = Dispatcher.CurrentDispatcher;
        }

        #region Methods
        public bool PersistInHistory() => false;
        public bool IsNavigationTarget(NavigationContext navigationContext) => false;
        public void OnNavigatedFrom(NavigationContext navigationContext) { }
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            if (navigationContext.Parameters.TryGetValue("target", out string target))
                Target = target;
            if (navigationContext.Parameters.TryGetValue("status", out string status))
                Status = status;
            if (navigationContext.Parameters.TryGetValue("region", out string region))
                RegionName = region;

            if (navigationContext.Parameters.TryGetValue("parameters", out NavigationParameters param))
                TargetParameters = param;
            else
                TargetParameters = null;

            if (navigationContext.Parameters.TryGetValue("task", out Action task))
            {
                TaskUpdate = null;
                Task = task;
                WorkTask.Run(WaitForExit);
            }
            else if (navigationContext.Parameters.TryGetValue("task-update", out Action<Action<string>> taskUpdate))
            {
                Task = null;
                TaskUpdate = taskUpdate;
                WorkTask.Run(WaitForExit);
            }

        }
        private void UpdateStatus(string status) => _Dispatcher.Invoke(() => Status = status);
        private void TaskCompleted()
        {
            if (Target != null)
            {
                if (TargetParameters == null)
                    _RegionManager.RequestNavigate(RegionName, Target);
                else
                    _RegionManager.RequestNavigate(RegionName, Target, TargetParameters);
            }
        }
        private void WaitForExit()
        {
            Action taskAction = Task ?? (() => TaskUpdate(UpdateStatus));
            WorkTask.Run(taskAction).Wait();
            _Dispatcher.Invoke(TaskCompleted);
        }
        #endregion
    }
}
