using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace Pete.ViewModels.Dialogs
{
    public class BugReportDialogViewModel : BindableBase, IDialogAware
    {
        #region Private
        private readonly string[] _Emojis = new string[]
        {
            "(。_。)", "(⊙_⊙)？", "＼（〇_ｏ）／",
            "(￣m￣）", "o_o", "(•ˋ _ ˊ•)", "(●__●)",
            "U_U", "X_X", "T_T", "O_O", "~_~", "-_-", "=_=",
        };
        private string _Emoji;
        private string _FullPath;
        private string _FileName;
        private string _FolderPath;
        private string _FolderName;
        private string _GithubLink = @"https://github.com/nightowl286";
        private DelegateCommand _OpenGithubCommand;
        private DelegateCommand _RestartCommand;
        private DelegateCommand<string> _OpenCommand;
        Random _rnd = new Random();
        public event Action<IDialogResult> RequestClose;
        #endregion

        #region Properties
        public string Emoji { get => _Emoji; private set => SetProperty(ref _Emoji, value); }
        public string FullPath { get => _FullPath; private set => SetProperty(ref _FullPath, value); }
        public string FileName { get => _FileName; private set => SetProperty(ref _FileName, value); }
        public string FolderPath { get => _FolderPath; private set => SetProperty(ref _FolderPath, value); }
        public string FolderName { get => _FolderName; private set => SetProperty(ref _FolderName, value); }
        public string GithubLink { get => _GithubLink; private set => SetProperty(ref _GithubLink, value); }
        public DelegateCommand OpenGithubCommand { get => _OpenGithubCommand; private set => SetProperty(ref _OpenGithubCommand, value); }
        public DelegateCommand RestartCommand { get => _RestartCommand; private set => SetProperty(ref _RestartCommand, value); }
        public DelegateCommand<string> OpenCommand { get => _OpenCommand; private set => SetProperty(ref _OpenCommand, value); }
        public string Title => "Pete | Bug encountered";
        #endregion
        public BugReportDialogViewModel()
        {
            RestartCommand = new DelegateCommand(App.RestartAsAdmin);
            OpenGithubCommand = new DelegateCommand(() => App.OpenUrl(GithubLink));
            OpenCommand = new DelegateCommand<string>(OpenCallback);
        }

        #region Methods
        private void OpenCallback(string path)
        {
            if (Directory.Exists(path))
            {
                if (!Path.EndsInDirectorySeparator(path))
                    path += Path.DirectorySeparatorChar;
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\""));
            }
            else if (File.Exists(path))
                ShowSelectedInExplorer.FileOrFolder(path);
        }
        private void PickRandomEmoji() => Emoji = _Emojis[_rnd.Next(0, _Emojis.Length)];
        public bool CanCloseDialog() => false;
        public void OnDialogClosed() { }
        public void OnDialogOpened(IDialogParameters parameters)
        {
            App.Current.Resources.MergedDictionaries[0]["Dialog.ShowInTaskBar"] = true;

            PickRandomEmoji();
            if (parameters.TryGetValue("path", out string path))
            {
                FullPath = path;
                FileName = Path.GetFileName(path);
                FolderPath = Path.GetDirectoryName(path);
                FolderName = Path.GetFileName(FolderPath);
            }
        }
        #endregion
    }
}
