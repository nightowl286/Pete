using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Pete
{
    public class InfoCircle : Control
    {
        #region Dependency Properties
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(InfoCircle));
        public static readonly DependencyProperty MaxTextWidthProperty = DependencyProperty.Register(nameof(MaxTextWidth), typeof(double), typeof(InfoCircle), new PropertyMetadata(275d));
        internal const string ToolTipHostTemplateName = "PART_ToolTip";
        #endregion

        #region Properties
        public string Text { get => GetValue(TextProperty) as string; set => SetValue(TextProperty, value); }
        public double MaxTextWidth { get => (double)GetValue(MaxTextWidthProperty); set => SetValue(MaxTextWidthProperty, value); }
        #endregion
        static InfoCircle()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(InfoCircle), new FrameworkPropertyMetadata(typeof(InfoCircle)));
        }
        public InfoCircle()
        {
            MouseEnter += InfoCircle_MouseEnter;
            IsKeyboardFocusedChanged += InfoCircle_IsKeyboardFocusedChanged;
        }
        ~InfoCircle()
        {
            MouseEnter -= InfoCircle_MouseEnter;
            IsKeyboardFocusedChanged -= InfoCircle_IsKeyboardFocusedChanged;
        }

        #region Functions
        private static ToolTip GetToolTip(InfoCircle infoCircle) => infoCircle.GetTemplateChild(ToolTipHostTemplateName) as ToolTip;
        #endregion

        #region Events
        private void InfoCircle_MouseEnter(object sender, MouseEventArgs e)
        {
            ToolTip tooltip = GetToolTip(this);
            if (IsKeyboardFocused && tooltip.IsOpen)
            {
                tooltip.IsOpen = false;
                tooltip.IsOpen = true;
            }
        }
        private void InfoCircle_IsKeyboardFocusedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is bool b)
                GetToolTip(this).IsOpen = b;
        }
        #endregion
    }
}
