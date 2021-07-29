using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Pete.Views.Login
{
    /// <summary>
    /// Interaction logic for LoginPassword
    /// </summary>
    public partial class LoginPassword : UserControl
    {
        public LoginPassword()
        {
            InitializeComponent();
        }

        private void TextboxMasked_Loaded(object sender, RoutedEventArgs e) => Keyboard.Focus(sender as IInputElement);
    }
}
