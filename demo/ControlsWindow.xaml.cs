using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System;
using System.IO;
using System.Globalization;

namespace VepfDemo
{
    public partial class ControlsWindow : Window
    {
        public ControlsWindow()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Closes the current window
        }

        private void PickColor_Click(object sender, RoutedEventArgs e)
        {
            OpenColorPicker();
        }

        public void OpenColorPicker()
        {
            CultureInfo currentUICulture = Thread.CurrentThread.CurrentUICulture;
            Console.WriteLine($"UI Culture: {currentUICulture.Name}");

            // 2. Instantiate and show the dialog
            var dialog = new ColorPickerDialog(Colors.SteelBlue)
            {
                Owner = this // Optional: block the main window
            };

            if (dialog.ShowDialog() == true && dialog.SelectedColor.HasValue)
            {
                Color chosenColor = dialog.SelectedColor.Value;

                // Convert the Color struct to a SolidColorBrush and apply it to the Label
                tbDemo.Foreground = new SolidColorBrush(chosenColor);
            }
        }

        private void MyColorCanvas_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            // Make sure the new color isn't null
            if (e.NewValue.HasValue)
            {
                // Grab the raw Color struct
                Color chosenColor = e.NewValue.Value;

                // Convert it to a SolidColorBrush and apply it to the TextBlock
                tbDemo.Foreground = new SolidColorBrush(chosenColor);
            }
        }

    }
}
