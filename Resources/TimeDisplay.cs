using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Pete.Resources
{
    public class TimeDisplay : TextBlock
    {
        #region Consts
        private const string DEFAULT_DATE_FORMAT = "dd/MM/yyyy hh:mm:ss";
        #endregion

        #region Dependency Properties
        public static readonly DependencyProperty ToolTipDateFormatProperty = DependencyProperty.Register(nameof(ToolTipDateFormat), typeof(string), typeof(TimeDisplay), new PropertyMetadata(DEFAULT_DATE_FORMAT, new PropertyChangedCallback(UpdateDate)));
        public static readonly DependencyProperty PostfixProperty = DependencyProperty.Register(nameof(Postfix), typeof(string), typeof(TimeDisplay), new PropertyMetadata(null, new PropertyChangedCallback(UpdateDate)));
        public static readonly DependencyProperty DateProperty = DependencyProperty.Register(nameof(Date), typeof(DateTime?), typeof(TimeDisplay), new PropertyMetadata(null, new PropertyChangedCallback(UpdateDate)));
        public static readonly DependencyProperty RelativeDateProperty = DependencyProperty.Register(nameof(RelativeDate), typeof(DateTime?), typeof(TimeDisplay), new PropertyMetadata(null, new PropertyChangedCallback(UpdateDate)));
        #endregion

        #region Properties
        public string ToolTipDateFormat { get => GetValue(ToolTipDateFormatProperty) as string; set => SetValue(ToolTipDateFormatProperty, value); }
        public string Postfix { get => GetValue(PostfixProperty) as string; set => SetValue(PostfixProperty, value); }
        public DateTime? Date { get => GetValue(DateProperty) as DateTime?; set => SetValue(DateProperty, value); }
        public DateTime? RelativeDate { get => GetValue(RelativeDateProperty) as DateTime?; set => SetValue(RelativeDateProperty, value); }
        #endregion
        public TimeDisplay()
        {
            Text = "???";
            ToolTip = null;
        }

        #region Methods
        public void UpdateDisplay()
        {
            if (Date.HasValue)
            {
                DateTime rel = RelativeDate.HasValue ? RelativeDate.Value.ToUniversalTime() : DateTime.UtcNow;

                string str = rel.Subtract(Date.Value.ToUniversalTime()).BiggestUnit();
                if (Postfix != null) str += Postfix;
                Text = str;

                ToolTip = Date.Value.ToLocalTime().ToString(ToolTipDateFormat ?? DEFAULT_DATE_FORMAT);
            }
            else
            {
                Text = "???";
                ToolTip = null;
            }
        }
        #endregion

        #region Functions
        private static void UpdateDate(DependencyObject obj, DependencyPropertyChangedEventArgs e) 
        {
            TimeDisplay display = obj as TimeDisplay;
            display.UpdateDisplay();
        }
        #endregion
    }
}
