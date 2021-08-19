using System.Windows.Controls;
using System.Windows.Input;

namespace Pete.Views
{
    /// <summary>
    /// Interaction logic for Dashboard
    /// </summary>
    public partial class Dashboard : UserControl
    {
        public Dashboard()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e) => Focus();
    }
}
