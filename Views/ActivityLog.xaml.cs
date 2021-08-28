using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Pete.Views
{
    /// <summary>
    /// Interaction logic for ActivityLog
    /// </summary>
    public partial class ActivityLog : UserControl
    {
        public ActivityLog()
        {
            InitializeComponent();
            FilterPopup.CustomPopupPlacementCallback = PlacePopup;
        }

        #region Methods
        private CustomPopupPlacement[] PlacePopup(Size popupSize, Size targetSize, Point offset)
        {
            Point xy = new Point(0.5d * (ButtonFilter.ActualWidth - popupSize.Width) ,0);
            xy.Offset(offset.X, offset.Y);
            CustomPopupPlacement vertical = new CustomPopupPlacement(xy, PopupPrimaryAxis.Vertical);
            CustomPopupPlacement horizontal = new CustomPopupPlacement(xy, PopupPrimaryAxis.Horizontal);

            return new CustomPopupPlacement[] { vertical, horizontal };

        }
        #endregion

        #region Events
        private void FilterPopup_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && FilterPopup.IsOpen)
            {
                e.Handled = true;
                FilterPopup.IsOpen = false;
                Keyboard.Focus(ButtonFilter);
                FocusManager.SetFocusedElement(this, ButtonFilter);
            }
        }
        private void FilterPopup_Opened(object sender, System.EventArgs e)
        {
            Keyboard.Focus(FilterFirstOption);
            FocusManager.SetFocusedElement(FilterPopup, FilterFirstOption);
        }
        #endregion

    }
}
