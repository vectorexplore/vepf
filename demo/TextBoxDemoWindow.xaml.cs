using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using NLog;
using System.Diagnostics;
using System.Windows.Navigation;
using System.Globalization;
using System.Windows.Markup; // Required for XmlLanguage
using System.Collections.Generic;
using System.Linq;

namespace VepfDemo
{
    public partial class TextBoxDemoWindow : Window
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private Random random = new Random();

        // Cached arrays for randomization
        private readonly Brush[] foregroundColors = [Brushes.Red, Brushes.DodgerBlue, Brushes.SeaGreen, Brushes.Purple, Brushes.DarkOrange, Brushes.Black, Brushes.Navy];
        private readonly Brush[] backgroundColors = [Brushes.Transparent, Brushes.LightYellow, Brushes.LightCyan, Brushes.LightPink, Brushes.Honeydew, Brushes.Lavender];
        private readonly FontWeight[] fontWeights = [FontWeights.Normal, FontWeights.Bold, FontWeights.Light, FontWeights.UltraBold];

        // Store original text for the reset functionality
        private readonly string originalTextBlockText;
        private readonly string originalTextBoxText;

        public TextBoxDemoWindow()
        {
            InitializeComponent();

            // Save initial state for the reset button
            originalTextBlockText = tb1.Text;
            originalTextBoxText = InputTextBox.Text;

            UpdateTextFormatting();

            TestGlyphApis();
            TestPublicGlyphApis();
        }

        private void UpdateTextFormatting()
        {
            // string textToDisplay = "VepfDemo!";
            // double currentFontSize = 24.0; 
            // double stepSize = 16.0;
            string textToDisplay = "WPF ON LINUX";
            double currentFontSize = 18.0;
            double stepSize = 8.0;         

            // 1. Clear any existing text from the XAML control
            MySteppingTextBlock.Inlines.Clear();

            // 2. Loop through the characters
            foreach (char c in textToDisplay)
            {
                Run run = new Run(c.ToString())
                {
                    FontSize = currentFontSize
                };

                // 3. Add the formatted character to our XAML-defined TextBlock
                MySteppingTextBlock.Inlines.Add(run);

                // 4. Increase the size for the next character
                currentFontSize += stepSize;
            }
        }

        // Feature 1: Property randomization logic
        private void BtnRandomize_Click(object sender, RoutedEventArgs e)
        {
            logger.Info("Randomizing TextBlock properties.");

            tb1.FontSize = random.Next(12, 28);
            tb1.Foreground = foregroundColors[random.Next(foregroundColors.Length)];
            tb1.Background = backgroundColors[random.Next(backgroundColors.Length)];
            tb1.FontWeight = fontWeights[random.Next(fontWeights.Length)];

            var alignments = (HorizontalAlignment[])Enum.GetValues(typeof(HorizontalAlignment));
            tb1.HorizontalAlignment = alignments[random.Next(alignments.Length)];

            // Append a marker to TextBox to show interaction
            InputTextBox.Text += "$";
        }

        // Feature 2: Showcasing TextBlock's Rich Text via C# Inlines
        private void BtnRichText_Click(object sender, RoutedEventArgs e)
        {
            logger.Info("Generating Rich Text in TextBlock.");

            tb1.Inlines.Clear(); // Clear simple text

            // Build a dynamic paragraph using Inlines
            tb1.Inlines.Add(new Run("TextBlock is incredibly flexible! ") { Foreground = Brushes.Black, FontSize = 14 });
            tb1.Inlines.Add(new Run("C# Inlines ") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold, FontSize = 18 });
            tb1.Inlines.Add(new Run("allow you to mix ") { Foreground = Brushes.Black, FontSize = 14 });

            tb1.Inlines.Add(new Italic(new Run("italics, ")));
            tb1.Inlines.Add(new Run("highlighted colors, ") { Background = Brushes.Yellow, Foreground = Brushes.Black });
            tb1.Inlines.Add(new Run("and sizes ") { FontSize = 22, Foreground = Brushes.Purple });
            tb1.Inlines.Add(new Run("all within a single flow of text programmatically."));

            // Normalize layout for readability
            tb1.HorizontalAlignment = HorizontalAlignment.Left;
            tb1.Background = Brushes.Transparent;
        }

        // Feature 3: Showcasing TextBox's Caret & Selection manipulation
        private void BtnManipulateTextBox_Click(object sender, RoutedEventArgs e)
        {
            logger.Info("Manipulating TextBox caret and selection.");

            InputTextBox.Focus();

            string insertString = "[C# INJECTION]";
            int currentCaretPosition = InputTextBox.CaretIndex;

            // Insert text exactly where the user left their blinking cursor
            InputTextBox.Text = InputTextBox.Text.Insert(currentCaretPosition, insertString);

            // Highlight the inserted text
            InputTextBox.Select(currentCaretPosition, insertString.Length);
        }

        // Feature 4: Reset everything back to normal
        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            logger.Info("Resetting demo to initial state.");

            // Reset TextBlock
            tb1.Inlines.Clear();
            tb1.Text = originalTextBlockText;
            tb1.FontSize = 14;
            tb1.Foreground = Brushes.Black;
            tb1.Background = Brushes.Transparent;
            tb1.FontWeight = FontWeights.Normal;
            tb1.HorizontalAlignment = HorizontalAlignment.Left;

            // Reset TextBox
            InputTextBox.Text = originalTextBoxText;
        }
        private void BtnGlyphTypeface_Click(object sender, RoutedEventArgs e)
        {
            TestGlyphApis();
        }
        private void TestGlyphApis()
        {
            // Note: Ensure this font is actually installed on the system, 
            // otherwise WPF will silently substitute a fallback font.
            Typeface typeface = new Typeface("DejaVu Sans");

            if (typeface.TryGetGlyphTypeface(out GlyphTypeface glyphTypeface))
            {
                // This is safe, but redundant as TryGetGlyphTypeface ensures it isn't null if it returns true.
                if (glyphTypeface is null) return;
                
                var cInfo = CultureInfo.GetCultureInfo("en-US");
                logger.Info($"g_glyphTypeface : {glyphTypeface}  cultureInfo={cInfo.Name}");
                
                var fNames = glyphTypeface.FamilyNames;
                
                // SAFELY extract the family name
                string loadedFamilyName = "Unknown";
                if (fNames.TryGetValue(cInfo, out string? name) && name != null)
                {
                    loadedFamilyName = name; // Found exactly "en-US"
                }
                else if (fNames.Count > 0)
                {
                    // Fallback: If "en-US" doesn't exist, just grab the first available name
                    loadedFamilyName = fNames.Values.First(); 
                }

                logger.Info($"Loaded: {loadedFamilyName}\n");

                IDictionary<ushort, double> leftSideBearings = glyphTypeface.LeftSideBearings;
                int count = 0;
                
                foreach (KeyValuePair<ushort, double> bearing in leftSideBearings)
                {
                    if (count++ >= 10) 
                    {
                        logger.Info("... ");
                        break; 
                    }

                    ushort glyphIndex = bearing.Key;
                    double distance = bearing.Value;
                    string status = distance >= 0 ? "inside rect" : "outside rect";

                    logger.Info($"glyph index: {glyphIndex}, left dist: {distance:F4} ({status})");
                }

                if (leftSideBearings.TryGetValue(0, out double specificBearing))
                {
                    logger.Info($"\n index 0 glyph bearing: {specificBearing}");
                }
            }
            else
            {
                logger.Info("Failed to get GlyphTypeface. Font might not be installed or supported.");
            }
        }


        private void TestPublicGlyphApis()
        {
            Typeface typeface = new Typeface("DejaVu Sans");

            if (typeface.TryGetGlyphTypeface(out GlyphTypeface glyphTypeface))
            {
                // Define the cultures we want to test against
                CultureInfo[] culturesToTest = new[] 
                { 
                    CultureInfo.GetCultureInfo("en-US"), 
                    CultureInfo.GetCultureInfo("en-GB")
                };

                foreach (var culture in culturesToTest)
                {
                    logger.Info($"\n=== Font Info (Preferred Culture: {culture.Name}) ===");

                    LogString("FamilyNames", glyphTypeface.FamilyNames, culture);
                    LogString("FaceNames", glyphTypeface.FaceNames, culture);
                    LogString("Win32FamilyNames", glyphTypeface.Win32FamilyNames, culture);
                    LogString("Win32FaceNames", glyphTypeface.Win32FaceNames, culture);
                    LogString("VersionStrings", glyphTypeface.VersionStrings, culture);
                    LogString("Copyrights", glyphTypeface.Copyrights, culture);
                    LogString("ManufacturerNames", glyphTypeface.ManufacturerNames, culture);
                    LogString("Trademarks", glyphTypeface.Trademarks, culture);
                    LogString("DesignerNames", glyphTypeface.DesignerNames, culture);
                    LogString("Descriptions", glyphTypeface.Descriptions, culture);
                    LogString("VendorUrls", glyphTypeface.VendorUrls, culture);
                    LogString("DesignerUrls", glyphTypeface.DesignerUrls, culture);
                    LogString("LicenseDescriptions", glyphTypeface.LicenseDescriptions, culture);
                    LogString("SampleTexts", glyphTypeface.SampleTexts, culture);
                }
            }
            else
            {
                logger.Info("Failed to load GlyphTypeface.");
            }
        }

        /// <summary>
        /// Helper to pull a string from the dictionary, falling back to the first available if preferred culture is missing.
        /// </summary>
        private void LogString(string label, IDictionary<CultureInfo, string> dict, CultureInfo preferredCulture)
        {
            if (dict == null || dict.Count == 0)
            {
                logger.Info($"{label}: [Not Provided]");
                return;
            }

            if (!dict.TryGetValue(preferredCulture, out string? value) && value != null)
            {
                // If the exact culture isn't found, grab the first one we can find and note it in the log
                value = dict.Values.FirstOrDefault() ?? "[Empty]";
                logger.Info($"{label}: {value} (Fallback)");
            }
            else
            {
                // Exact match found
                logger.Info($"{label}: {value}");
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // This safely opens the default web browser to the provided URL
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}