﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace Pete.ViewModels.Dialogs
{
    [RegionMemberLifetime(KeepAlive = false)]
    public class BugReportDialogViewModel : BindableBase, IDialogAware
    {
        #region Private
        private readonly string[] _Emojis = new string[]
        {
            "(。_。)", "＼（〇_ｏ）／","￣へ￣","┻━┻ ︵ ＼( °□° )／ ︵ ┻━┻","ಠ_ಠ","(ノ｀Д)ノ","(╯▔皿▔)╯",
            "(￣m￣）", "o_o", "(•ˋ _ ˊ•)", "(●__●)","(⓿_⓿)","(►__◄)","┗|｀O′|┛",@"¯\(°_o)/¯","(x_x)",
            "U_U", "X_X", "T_T", "O_O", "~_~", "-_-", "=_=","(╯°□°）╯︵ ┻━┻",
        };
        private string _Emoji;
        private string _FullPath;
        private string _FileName;
        private string _FolderPath;
        private string _FolderName;
        private DelegateCommand _RestartCommand;
        private DelegateCommand<string> _OpenCommand;
        private readonly Random _rnd = new Random();
#pragma warning disable CS0067
        public event Action<IDialogResult> RequestClose;
#pragma warning restore CS0067
        #endregion

        #region Properties
        public string Emoji { get => _Emoji; private set => SetProperty(ref _Emoji, value); }
        public string FullPath { get => _FullPath; private set => SetProperty(ref _FullPath, value); }
        public string FileName { get => _FileName; private set => SetProperty(ref _FileName, value); }
        public string FolderPath { get => _FolderPath; private set => SetProperty(ref _FolderPath, value); }
        public string FolderName { get => _FolderName; private set => SetProperty(ref _FolderName, value); }
        public DelegateCommand RestartCommand { get => _RestartCommand; private set => SetProperty(ref _RestartCommand, value); }
        public DelegateCommand<string> OpenCommand { get => _OpenCommand; private set => SetProperty(ref _OpenCommand, value); }
        public string Title => "Pete | Bug encountered";
        #endregion
        public BugReportDialogViewModel()
        {
            RestartCommand = new DelegateCommand(App.RestartAsAdmin);
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
