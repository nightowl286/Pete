using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Pete.Views.Registration
{
    /// <summary>
    /// Interaction logic for RegistrationPassword
    /// </summary>
    public partial class RegistrationPassword : UserControl
    {
        public RegistrationPassword()
        {
            InitializeComponent();
        }

        #region Events
        private void TextboxMasked_Loaded(object sender, RoutedEventArgs e)
        {
            Keyboard.Focus(sender as IInputElement);
        }
        #endregion
    }
}
