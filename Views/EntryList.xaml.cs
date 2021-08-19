using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using System.Windows.Data;

namespace Pete.Views
{
    /// <summary>
    /// Interaction logic for EntryList
    /// </summary>
    public partial class EntryList : UserControl
    {
        public EntryList()
        {
            InitializeComponent();
        }

        #region Events
        private void UserControl_Main_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Focus();
            GenerateKeyBindings();
        }
        #endregion

        #region Methods
        private void GenerateKeyBindings()
        {
            GenerateKeyBindings(Key.F1, Key.F24, ModifierKeys.None);
            GenerateKeyBindings(Key.NumPad1, Key.NumPad9, ModifierKeys.None);
            GenerateKeyBindings(new List<Key>()
            { Key.D1, Key.D2, Key.D3, Key.D4, Key.D5, Key.D6, Key.D7, Key.D8, Key.D9, Key.D0 }, ModifierKeys.None);
        }

        private void GenerateKeyBindings(Key from, Key to, ModifierKeys mod) => GenerateKeyBindings(Enumerable.Range((int)from, to - from + 1), mod);
        private void GenerateKeyBindings(IEnumerable<int> enumValues, ModifierKeys mod) => GenerateKeyBindings(enumValues.Select(i => (Key)i), mod);
        private void GenerateKeyBindings(IEnumerable<Key> keys, ModifierKeys mod)
        {
            Binding commandBinding = new Binding("ShowCategoryCommand")
            {
                Mode = BindingMode.OneWay,
                Source = DataContext
            };

            int counter = 0;
            foreach (Key key in keys)
            {
                KeyBinding gesture = new KeyBinding();
                gesture.Key = key;
                gesture.Modifiers = mod;
                gesture.CommandParameter = (uint?)counter++;
                BindingOperations.SetBinding(gesture, KeyBinding.CommandProperty, commandBinding);

                InputBindings.Add(gesture);

            }
        }
        #endregion
    }
}
