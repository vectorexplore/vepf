using System.Windows;
using System.Windows.Media;
using System.Reflection;

namespace VepfDemo
{
    public class ColorItem
    {
        public required string Name { get; set; }
        public required Brush ColorBrush { get; set; }
    }

    public partial class ColorDemoWindow : Window
    {
        public ColorDemoWindow()
        {
            InitializeComponent();
            LoadColors();
        }

        private void LoadColors()
        {
            // 1. Change 'Colors' to 'Brushes'
            var colorProperties = typeof(Brushes).GetProperties(BindingFlags.Public | BindingFlags.Static);
            List<ColorItem> colorList = [];

            foreach (var prop in colorProperties)
            {
                // 2. Check for 'Brush' (or SolidColorBrush)
                if (typeof(Brush).IsAssignableFrom(prop.PropertyType))
                {
                    if (prop.GetValue(null) is Brush brush)
                    {
                        colorList.Add(new ColorItem { Name = prop.Name, ColorBrush = brush });
                    }
                    // var brush = (Brush) prop.GetValue(null)!;
                    // colorList.Add(new ColorItem { Name = prop.Name, ColorBrush = brush });
                }
            }
            colorList.Sort((x, y) => string.Compare(x.Name, y.Name));

            // --- Dynamic Gradient Section ---
            colorList.Add(new ColorItem { Name = "Gradient Section", ColorBrush = CreateLinear(Colors.YellowGreen, Colors.DeepSkyBlue) });

            // Define some color pairs for the gradients
            var gradientSchemes = new[] {
                new { Name = "Sky", C1 = Colors.DeepSkyBlue, C2 = Colors.White },
                new { Name = "Fire", C1 = Colors.Red, C2 = Colors.Yellow },
                new { Name = "Forest", C1 = Colors.DarkGreen, C2 = Colors.LightGreen },
                new { Name = "Royal", C1 = Colors.Purple, C2 = Colors.Gold },
                new { Name = "Slate", C1 = Colors.Black, C2 = Colors.Gray }
            };

            foreach (var scheme in gradientSchemes)
            {
                colorList.Add(new ColorItem
                {
                    Name = $"Linear{scheme.Name}",
                    ColorBrush = CreateLinear(scheme.C1, scheme.C2)
                });
                colorList.Add(new ColorItem
                {
                    Name = $"Radial{scheme.Name}",
                    ColorBrush = CreateRadial(scheme.C1, scheme.C2)
                });
            }

            ColorsListBox.ItemsSource = colorList;
        }


        // Helper for Linear Grushes
        private static LinearGradientBrush CreateLinear(Color c1, Color c2) =>
            new(c1, c2, new Point(0, 0), new Point(1, 0));

        // Helper for Radial Brushes
        private RadialGradientBrush CreateRadial(Color c1, Color c2)
        {
            var rgb = new RadialGradientBrush(c1, c2)
            {
                RadiusX = 1,
                RadiusY = 1 // Ensures it fills the rectangle well
            };
            return rgb;
        }
    }
}
