using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using NLog;

namespace VepfDemo
{
    public partial class FontViewerWindow : Window
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public FontViewerWindow()
        {
            InitializeComponent();
            LoadFonts();
        }

        private void LoadFonts()
        {
            try
            {
                var fonts = Fonts.SystemFontFamilies
                    // .Where(f => f.Source.Contains("Emoji", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(f => f.Source).ToList();
                FontListBox.ItemsSource = fonts;
                this.Title = $"Font Viewer (total {fonts.Count} fonts)";
            }
            catch (Exception)
            {
                throw;

            }
        }
    }
}
