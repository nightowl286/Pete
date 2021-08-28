using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Pete
{
    public class TextboxHint : TextBox
    {
        #region Dependencies
        public static readonly DependencyProperty HintProperty = DependencyProperty.Register(nameof(Hint), typeof(string), typeof(TextboxHint));
        public static readonly DependencyProperty HintVisibilityProperty = DependencyProperty.Register(nameof(HintVisibility), typeof(Visibility), typeof(TextboxHint));
        public static readonly DependencyProperty HintForegroundProperty = DependencyProperty.Register(nameof(HintForeground), typeof(Brush), typeof(TextboxHint));
        public static readonly DependencyProperty ErrorProperty = DependencyProperty.Register(nameof(Error), typeof(string), typeof(TextboxHint), new PropertyMetadata(null, new PropertyChangedCallback((o, e) => (o as TextboxHint).HasError = e.NewValue != null)));
        public static readonly DependencyProperty HasErrorProperty = DependencyProperty.Register(nameof(HasError), typeof(bool), typeof(TextboxHint));
        #endregion

        #region Properties
        public Visibility HintVisibility
        {
            get => (Visibility)GetValue(HintVisibilityProperty);
            private set => SetValue(HintVisibilityProperty, value);
        }
        public string Hint
        {
            get => GetValue(HintProperty) as string;
            set => SetValue(HintProperty, value);
        }
        public Brush HintForeground { get => GetValue(HintForegroundProperty) as Brush; set => SetValue(HintForegroundProperty, value); }
        public string Error
        {
            get => GetValue(ErrorProperty) as string;
            set => SetValue(ErrorProperty, value);
        }
        public bool HasError
        {
            get => (bool)GetValue(HasErrorProperty);
            private set => SetValue(HasErrorProperty, value);
        }
        #endregion
        static TextboxHint()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TextboxHint), new FrameworkPropertyMetadata(typeof(TextboxHint)));
        }
        public TextboxHint()
        {
            Unloaded += TextboxHint_Unloaded;
            TextChanged += TextboxHint_TextChanged;
        }


        #region Events
        private void TextboxHint_Unloaded(object sender, RoutedEventArgs e)
        {
            TextChanged -= TextboxHint_TextChanged;
            Unloaded -= TextboxHint_Unloaded;
        }
        private void TextboxHint_TextChanged(object sender, TextChangedEventArgs e) => HintVisibility = string.IsNullOrEmpty(Text) ? Visibility.Visible : Visibility.Hidden;
        #endregion
    }
}
