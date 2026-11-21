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

namespace VepfDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mouse.log");

        public MainWindow()
        {
            InitializeComponent();
        }
        private void Button_Click_MsgBox(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Hello VePF!");
        }

        private void ShowControls_Click(object sender, RoutedEventArgs e)
        {
            ControlsWindow secondWindow = new ControlsWindow();
            // Show() allows interacting with both windows; 
            // ShowDialog() forces focus on the new window
            secondWindow.Show(); 
        }
        private void ShowFontViewer_Click(object sender, RoutedEventArgs e)
        {
            var secondWindow = new FontViewerWindow();
            secondWindow.Show();
        }
        private void TextBoxDemo_Click(object sender, RoutedEventArgs e)
        {
            var secondWindow = new TextBoxDemoWindow();
            secondWindow.Show();
        }
        private void ScottPlotDemo_Click(object sender, RoutedEventArgs e)
        {
#if LINUX            
            var secondWindow = new ScottPlotDemoWindow();
            secondWindow.Show();
#endif            
        }

        private readonly Dictionary<string, Func<Window>> _windowFactories = new()
        {
            ["Image"] = () => new ImageDemoWindow(),
            ["Color"] = () => new ColorDemoWindow(),
        };

        private void OpenDemo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: string key } &&
                _windowFactories.TryGetValue(key, out var createWindow))
            {
                createWindow().Show();
            }
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void ExitApp_Click(object sender, RoutedEventArgs e)
        {
            // Terminate the entire WPF application process
            Application.Current.Shutdown();
        }
    }
}
