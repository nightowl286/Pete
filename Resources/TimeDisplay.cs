using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Pete.Resources
{
    public class TimeDisplay : TextBlock
    {
        #region Consts
        private const string DEFAULT_DATE_FORMAT = "dd/MM/yyyy HH:mm:ss";
        #endregion

        #region Dependency Properties
        public static readonly DependencyProperty ToolTipDateFormatProperty = DependencyProperty.Register(nameof(ToolTipDateFormat), typeof(string), typeof(TimeDisplay), new PropertyMetadata(DEFAULT_DATE_FORMAT, new PropertyChangedCallback(UpdateDate)));
        public static readonly DependencyProperty PostfixProperty = DependencyProperty.Register(nameof(Postfix), typeof(string), typeof(TimeDisplay), new PropertyMetadata(null, new PropertyChangedCallback(UpdateDate)));
        public static readonly DependencyProperty DateProperty = DependencyProperty.Register(nameof(Date), typeof(DateTime?), typeof(TimeDisplay), new PropertyMetadata(null, new PropertyChangedCallback(UpdateDate)));
        public static readonly DependencyProperty RelativeDateProperty = DependencyProperty.Register(nameof(RelativeDate), typeof(DateTime?), typeof(TimeDisplay), new PropertyMetadata(null, new PropertyChangedCallback(UpdateDate)));
        public static readonly DependencyProperty UnitCountProperty = DependencyProperty.Register(nameof(UnitCount), typeof(int), typeof(TimeDisplay), new PropertyMetadata(1, new PropertyChangedCallback(UpdateDate)));
        public static readonly DependencyProperty ShowPreciseProperty = DependencyProperty.Register(nameof(ShowPrecise), typeof(bool), typeof(TimeDisplay), new PropertyMetadata(false, new PropertyChangedCallback(UpdateDate)));
        #endregion

        #region Static
        private static ulong _InstanceCounter = 0;
        private static DispatcherTimer _UpdateTimer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(5), IsEnabled = false };
        #endregion

        #region Properties
        public int UnitCount { get => (int)GetValue(UnitCountProperty); set => SetValue(UnitCountProperty, value); }
        public bool ShowPrecise { get => (bool)GetValue(ShowPreciseProperty); set => SetValue(ShowPreciseProperty, value); }
        public string ToolTipDateFormat { get => GetValue(ToolTipDateFormatProperty) as string; set => SetValue(ToolTipDateFormatProperty, value); }
        public string Postfix { get => GetValue(PostfixProperty) as string; set => SetValue(PostfixProperty, value); }
        public DateTime? Date { get => GetValue(DateProperty) as DateTime?; set => SetValue(DateProperty, value); }
        public DateTime? RelativeDate { get => GetValue(RelativeDateProperty) as DateTime?; set => SetValue(RelativeDateProperty, value); }
        #endregion
        public TimeDisplay()
        {
            Text = "???";
            ToolTip = null;

            IncrementCounter();
            _UpdateTimer.Tick += _UpdateTimer_Tick;
        }
        ~TimeDisplay()
        {
            DecrementCounter();
            _UpdateTimer.Tick -= _UpdateTimer_Tick;
        }

        #region Methods
        public void UpdateDisplay()
        {
            if (Date.HasValue)
            {
                DateTime rel = RelativeDate.HasValue ? RelativeDate.Value.ToUniversalTime() : DateTime.UtcNow;

                string tooltipText = Date.Value.ToLocalTime().ToString(ToolTipDateFormat ?? DEFAULT_DATE_FORMAT);
                ToolTip = tooltipText;

                if (ShowPrecise)
                    Text = tooltipText;
                else
                {
                    string str = rel.Subtract(Date.Value.ToUniversalTime()).BiggestUnit(Math.Max(1, UnitCount));
                    if (Postfix != null) str += Postfix;
                    Text = str;
                }
            }
            else
            {
                Text = "???";
                ToolTip = null;
            }
        }
        #endregion

        #region Events
        private void _UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (Date.HasValue & !RelativeDate.HasValue)
                UpdateDisplay();
        }
        #endregion

        #region Functions
        private static void IncrementCounter()
        {
            _InstanceCounter++;
            if (_InstanceCounter == 1)
                _UpdateTimer.Start();
        }
        private static void DecrementCounter()
        {
            _InstanceCounter--;
            if (_InstanceCounter == 0)
                _UpdateTimer.Stop();
        }
        private static void UpdateDate(DependencyObject obj, DependencyPropertyChangedEventArgs e) 
        {
            TimeDisplay display = obj as TimeDisplay;
            display.UpdateDisplay();
        }
        #endregion
    }
}
