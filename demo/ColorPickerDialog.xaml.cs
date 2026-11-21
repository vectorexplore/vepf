using System.Windows;
using System.Windows.Media;

namespace VepfDemo
{
    public partial class ColorPickerDialog : Window
    {
        // Property to hold the final selection
        public Color? SelectedColor { get; private set; }

        public ColorPickerDialog(Color? initialColor = null)
        {
            InitializeComponent();
            
            if (initialColor.HasValue)
            {
                MyColorCanvas.SelectedColor = initialColor.Value;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Capture the selected color and close with success
            SelectedColor = MyColorCanvas.SelectedColor;
            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Cancel and close
            this.DialogResult = false;
            this.Close();
        }
    }
}