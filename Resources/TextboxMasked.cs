using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Pete
{
    public class TextboxMasked : TextBox
    {
        #region Dependencies
        public static readonly DependencyProperty HintProperty = DependencyProperty.Register(nameof(Hint), typeof(string), typeof(TextboxMasked));
        public static readonly DependencyProperty HintVisibilityProperty = DependencyProperty.Register(nameof(HintVisibility), typeof(Visibility), typeof(TextboxMasked));
        public static readonly DependencyProperty HintForegroundProperty = DependencyProperty.Register(nameof(HintForeground), typeof(Brush), typeof(TextboxMasked));
        public static readonly DependencyProperty MaskTextProperty = DependencyProperty.Register(nameof(MaskText), typeof(string), typeof(TextboxMasked));
        public static readonly DependencyProperty ErrorProperty = DependencyProperty.Register(nameof(Error), typeof(string), typeof(TextboxMasked), new PropertyMetadata(null, new PropertyChangedCallback((o, e) => (o as TextboxMasked).HasError = e.NewValue != null)));
        public static readonly DependencyProperty HasErrorProperty = DependencyProperty.Register(nameof(HasError), typeof(bool), typeof(TextboxMasked));
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
        public string MaskText
        {
            get => GetValue(MaskTextProperty) as string;
            private set => SetValue(MaskTextProperty, value);
        }
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
        public Brush HintForeground { get => GetValue(HintForegroundProperty) as Brush; set => SetValue(HintForegroundProperty, value); }
        #endregion
        static TextboxMasked()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TextboxMasked), new FrameworkPropertyMetadata(typeof(TextboxMasked)));
        }
        public TextboxMasked()
        {
            TextChanged += TextboxHint_TextChanged;
        }

        #region Events
        private void TextboxHint_TextChanged(object sender, TextChangedEventArgs e)
        {
            HintVisibility = string.IsNullOrEmpty(Text) ? Visibility.Visible : Visibility.Hidden;
            MaskText = "";
            for (int i = 0; i < Text.Length; i++)
                MaskText += '▪';
        }
        #endregion
    }
}
