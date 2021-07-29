using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Prism.Services.Dialogs;

namespace Pete.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for GeneralDialogWindow.xaml
    /// </summary>
    public partial class GeneralDialogWindow : Window, IDialogWindow
    {
        #region Properties
        public IDialogResult Result { get; set; }
        #endregion
        public GeneralDialogWindow()
        {
            InitializeComponent();
        }

    }
}
