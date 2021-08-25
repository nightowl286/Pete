using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using Pete.Models;
using Pete.Models.Logs;
using Pete.Views;
using Pete.Views.Dialogs;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;
using TNO.BitUtilities;

namespace Pete
{
    public static class Extensions
    {
        #region Navigate Task Screen
        private static void NavigateTaskScreenBase(IRegionManager regionManager, string region, object task, string target, string status, NavigationParameters targetParameters)
        {
            NavigationParameters p = new NavigationParameters()
            {
                {(task is Action<Action<string>> ?"task-update" : "task"), task },
                {"region",region },
                {"target",target },
            };
            if (status != null)
                p.Add("status", status);

            if (targetParameters != null)
                p.Add("parameters", targetParameters);

            regionManager.RequestNavigate(region, nameof(TaskScreen), App.DebugNavigationCallback, p);
        }
        public static void NavigateTaskScreen(this IRegionManager regionManager, string region, Action task, string target, string status, NavigationParameters targetParameters) =>
            NavigateTaskScreenBase(regionManager, region, task, target, status, targetParameters);
        public static void NavigateTaskScreen(this IRegionManager regionManager, string region, Action<Action<string>> task, string target, NavigationParameters targetParameters)
            => NavigateTaskScreenBase(regionManager, region, task, target, null, targetParameters);
        public static void NavigateTaskScreen(this IRegionManager regionManager, string region, Action task, string target, string status)
            => regionManager.NavigateTaskScreen(region, task, target, status, null);
        public static void NavigateTaskScreen(this IRegionManager regionManager, string region, Action<Action<string>> task, string target)
            => regionManager.NavigateTaskScreen(region, task, target, null);
        public static void NavigateTaskScreen(this IRegionManager regionManager, string region, Action task, string status)
            => regionManager.NavigateTaskScreen(region, task, null, status, null);
        public static void NavigateTaskScreen(this IRegionManager regionManager, string region, Action<Action<string>> task)
            => regionManager.NavigateTaskScreen(region, task, null, null);
        #endregion

        #region Time Formatting
        public static string BiggestUnit(this TimeSpan time) => BiggestUnit(time, 1);
        public static string BiggestUnit(this TimeSpan time, int units)
        {
            List<string> parts = new List<string>();

            for(int i = 0; i < units;i++)
            {
                double value;
                string unit;
                if (time.Days > 0)
                {
                    if (time.Days >= 365)
                    {
                        value = time.Days / 365;
                        unit = "year";
                        time -= TimeSpan.FromDays(value * 365);
                    }
                    else if (time.Days >= 30)
                    {
                        value = time.Days / 30;
                        unit = "month";
                        time -= TimeSpan.FromDays(value * 30);
                    }
                    else
                    {
                        value = time.Days;
                        unit = "day";
                        time -= TimeSpan.FromDays(value);
                    }
                }
                else if (time.Hours > 0)
                {
                    value = time.Hours;
                    unit = "hour";
                    time -= TimeSpan.FromHours(value);
                }
                else if (time.Minutes > 0)
                {
                    value = time.Minutes;
                    unit = "minute";
                    time -= TimeSpan.FromMinutes(value);
                }
                else
                {
                    value = Math.Max(1, time.Seconds);
                    unit = "second";
                    time = TimeSpan.Zero;
                }
                if (value != 1) unit += "s";
                parts.Add(value.ToString("N0"));
                parts.Add(unit);

                if (time == TimeSpan.Zero) break;
            }


            return string.Join(' ', parts);
        }
        #endregion

        #region ConfirmRemove Dialog
        public static void ConfirmRemove(this IDialogService service, Action<IDialogResult> callback, string type, string name)
            => ConfirmRemove(service, callback, type, name, null, null);
        public static void ConfirmRemove(this IDialogService service, Action<IDialogResult> callback, string type, string name, string extra, string custom)
        {
            DialogParameters param = new DialogParameters
            {
                { "type", type },
                { "name", name },
            };
            if (!string.IsNullOrWhiteSpace(extra)) param.Add("extra", extra);
            if (!string.IsNullOrWhiteSpace(custom)) param.Add("custom", custom);

            service.Show(nameof(ConfirmRemoveDialog), param, callback, nameof(GeneralDialogWindow));
        }
        public static void ConfirmRemove(this IDialogService service, Action<bool> callback, string type, string name) => ConfirmRemove(service, callback, type, name, null);
        public static void ConfirmRemove(this IDialogService service, Action<bool> callback, string type, string name, string extra) => ConfirmRemove(service, res => callback?.Invoke(res.Result == ButtonResult.Yes), type, name, extra, null);
        public static void ConfirmRemove(this IDialogService service, Action trueCallback, string type, string name, string extra) => ConfirmRemove(service, res => {
            if (res.Result == ButtonResult.Yes) trueCallback?.Invoke();
        },
            type, name, extra, null);
        public static void ConfirmRemove(this IDialogService service, Action trueCallback, string type, string name) => ConfirmRemove(service, trueCallback, type, name, null);
        public static void ConfirmRemove(this IDialogService service, Action<bool> callback, string custom) => ConfirmRemove(service, res => callback?.Invoke(res.Result == ButtonResult.Yes), null, null, null, custom);
        public static void ConfirmRemove(this IDialogService service, Action trueCallback, string custom) => ConfirmRemove(service, res => {
            if (res.Result == ButtonResult.Yes) trueCallback?.Invoke();
        },
            null, null, null, custom);
        #endregion

        #region Message Dialog
        public static void Message(this IDialogService service, Action<IDialogResult> result, string title, string message, ButtonResult defaultResult = ButtonResult.None, params ButtonInfo[] buttons)
        {
            DialogParameters p = new DialogParameters();
            if (message != null) p.Add("message", message);
            if (title != null) p.Add("title", title);
            p.Add("default", defaultResult);
            p.Add("buttons", buttons);

            service.ShowDialog(nameof(MessageDialog), p, result, nameof(GeneralDialogWindow));
        }
        public static void Message(this IDialogService service, Action<IDialogResult> result, string title, string message, params ButtonResult[] buttons)
        {
            ButtonInfo[] btns = new ButtonInfo[buttons.Length];
            for (int i = 0; i < buttons.Length; i++)
            {
                ButtonResult res = buttons[i];
                ButtonType type = ButtonType.Normal;
                if (res == ButtonResult.OK | res == ButtonResult.Yes) type = ButtonType.Primary;
                else if (res == ButtonResult.Abort | res == ButtonResult.No | res == ButtonResult.Cancel) type = ButtonType.Cancel;

                string content = res.ToString().ToLower();
                btns[i] = new ButtonInfo(type, content, res);
            }
            Message(service, result, title, message, ButtonResult.None, btns);
        }
        public static void Message(this IDialogService service, Action<ButtonResult> result, string title, string message, params ButtonResult[] buttons) =>
            Message(service, (IDialogResult res) => result?.Invoke(res.Result), title, message, buttons);
        public static void Message(this IDialogService service, Action<ButtonResult> result, string title, string message, ButtonResult defaultResult = ButtonResult.None, params ButtonInfo[] buttons) =>
            Message(service, (IDialogResult res) => result?.Invoke(res.Result), title, message, defaultResult, buttons);
        #endregion

        #region Input Dialog
        public static void Input(this IDialogService service, Action<IDialogResult> result, string title, string message, string hint, string input, Func<string, string> validation, bool allowCancel, ButtonResult defaultResult = ButtonResult.None, params ButtonInfo[] buttons)
        {
            DialogParameters p = new DialogParameters();
            if (message != null) p.Add("message", message);
            if (title != null) p.Add("title", title);
            if (validation != null) p.Add("validation", validation);
            if (hint != null) p.Add("hint", hint);
            if (input != null) p.Add("input", input);

            p.Add("default", defaultResult);
            p.Add("buttons", buttons);
            p.Add("allow-cancel", allowCancel);

            service.ShowDialog(nameof(TextInputDialog), p, result, nameof(GeneralDialogWindow));
        }
        public static void Input(this IDialogService service, Action<IDialogResult> result, string title, string message, string hint, string input, Func<string, string> validation, bool allowCancel, params ButtonResult[] buttons)
        {
            ButtonInfo[] btns = new ButtonInfo[buttons.Length];
            for (int i = 0; i < buttons.Length; i++)
            {
                ButtonResult res = buttons[i];
                ButtonType type = ButtonType.Normal;
                if (res == ButtonResult.OK | res == ButtonResult.Yes) type = ButtonType.Primary;
                else if (res == ButtonResult.Abort | res == ButtonResult.No | res == ButtonResult.Cancel) type = ButtonType.Cancel;

                string content = res.ToString().ToLower();
                btns[i] = new ButtonInfo(type, content, res);
            }
            Input(service, result, title, message, hint, input, validation, allowCancel, ButtonResult.None, btns);
        }
        public static void Input(this IDialogService service, Action<IDialogResult> result, string title, string message, string hint, string input, Func<string, string> validation, params ButtonResult[] buttons) => Input(service, result, title, message, hint, input, validation, true, buttons);
        public static void Input(this IDialogService service, Action<IDialogResult> result, string title, string message, string hint, string input, params ButtonResult[] buttons) => Input(service, result, title, message, hint, input, null, true, buttons);
        public static void Input(this IDialogService service, Action<IDialogResult> result, string title, string message, string hint, Func<string, string> validation, params ButtonResult[] buttons) => Input(service, result, title, message, hint, null, validation, true, buttons);
        public static void Input(this IDialogService service, Action<IDialogResult> result, string title, string message, string hint, params ButtonResult[] buttons) => Input(service, result, title, message, hint, null, null, true, buttons);

        public static void Input(this IDialogService service, Action<ButtonResult, string> result, string title, string message, string hint, string input, Func<string, string> validation, bool allowCancel, params ButtonResult[] buttons) => Input(service,
            (IDialogResult res) => { res.Parameters.TryGetValue("input", out string input); result?.Invoke(res.Result, input); },
            title, message, hint, input, validation, allowCancel, buttons);
        public static void Input(this IDialogService service, Action<ButtonResult, string> result, string title, string message, string hint, string input, Func<string, string> validation, params ButtonResult[] buttons) => Input(service, result, title, message, hint, input, validation, true, buttons);
        public static void Input(this IDialogService service, Action<ButtonResult, string> result, string title, string message, string hint, string input, params ButtonResult[] buttons) => Input(service, result, title, message, hint, input, null, true, buttons);
        public static void Input(this IDialogService service, Action<ButtonResult, string> result, string title, string message, string hint, params ButtonResult[] buttons) => Input(service, result, title, message, hint, null, null, true, buttons);

        public static void Input(this IDialogService service, Action<ButtonResult, string> result, string title, string message, string hint, string input, Func<string, string> validation, bool allowCancel, ButtonResult defaultResult = ButtonResult.None, params ButtonInfo[] buttons) => Input(service,
             (IDialogResult res) => { res.Parameters.TryGetValue("input", out string input); result?.Invoke(res.Result, input); },
             title, message, hint, input, validation, allowCancel, defaultResult, buttons);
        public static void Input(this IDialogService service, Action<ButtonResult, string> result, string title, string message, string hint, string input, Func<string, string> validation, ButtonResult defaultResult = ButtonResult.None, params ButtonInfo[] buttons) => Input(service, result, title, message, hint, input, validation, true, defaultResult, buttons);
        public static void Input(this IDialogService service, Action<ButtonResult, string> result, string title, string message, string hint, Func<string, string> validation, ButtonResult defaultResult = ButtonResult.None, params ButtonInfo[] buttons) => Input(service, result, title, message, hint, null, validation, true, defaultResult, buttons);
        public static void Input(this IDialogService service, Action<ButtonResult, string> result, string title, string message, string hint, string input, ButtonResult defaultResult = ButtonResult.None, params ButtonInfo[] buttons) => Input(service, result, title, message, hint, input, null, true, defaultResult, buttons);
        public static void Input(this IDialogService service, Action<ButtonResult, string> result, string title, string message, string hint, ButtonResult defaultResult = ButtonResult.None, params ButtonInfo[] buttons) => Input(service, result, title, message, hint, null, null, true, defaultResult, buttons);
        #endregion

        #region Enum Write
        public static void WriteLogType(this IAdvancedBitWriter w, LogType type) => w.WriteNum((byte)type, 3);
        public static void WriteEntryLogType(this IAdvancedBitWriter w, EntryLogType type) => w.WriteNum((byte)type, 2);
        public static void WriteTamperType(this IAdvancedBitWriter w, TamperType type) => w.WriteNum((byte)type, 1);
        public static LogType ReadLogType(this IAdvancedBitReader r) => (LogType)(byte)r.ReadNum(3);
        public static EntryLogType ReadEntryLogType(this IAdvancedBitReader r) => (EntryLogType)(byte)r.ReadNum(2);
        public static TamperType ReadTamperType(this IAdvancedBitReader r) => (TamperType)(byte)r.ReadNum(1);
        #endregion

        #region Other
        public static bool TryPeek<T>(this IList<T> collection, out T item)
        {
            if (collection.Count > 0)
            {
                item = collection[0];
                return true;
            }
            item = default;
            return false;
        }
        public static T Dequeue<T>(this IList<T> collection)
        {
            if (collection.Count == 0) throw new InvalidOperationException("The collection is empty");

            T item = collection[0];
            collection.RemoveAt(0);
            return item;
        }
        public static void Add(this IList<ButtonInfo> buttons, ButtonType type, object content, object parameter) => buttons.Add(new ButtonInfo(type, content, parameter));
        public static void Add(this IList<ButtonInfo> buttons, object content, object parameter) => buttons.Add(new ButtonInfo(content, parameter));
        public static void Add(this IList<ButtonInfo> buttons, object content) => buttons.Add(new ButtonInfo(content));
        #endregion
    }
}