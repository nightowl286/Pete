using System.Windows;
using Pete.Views.Login;
using Pete.Views.Registration;
using Prism.Regions;

namespace Pete.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(IRegionManager manager)
        {
            InitializeComponent();
            manager.Regions.Remove(RegionNames.MainRegion);
            RegionManager.SetRegionName(mainRegion, RegionNames.MainRegion);
            RegionManager.SetRegionManager(mainRegion, manager);

            (App.Current as App).DecideStartScreen(manager);
        }
    }
}
