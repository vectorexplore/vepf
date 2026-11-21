using System;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.IO;
using System.Windows.Media;
using NLog;

namespace VepfDemo
{
    public partial class ImageDemoWindow : Window
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public ImageDemoWindow()
        {
            InitializeComponent();
            // LoadDefaultLocalImage();
        }

        // private void LoadDefaultLocalImage()
        // {
        //     // Example of loading from a hardcoded absolute path
        //     string defaultPath = @"/Users/zouyoufang/veweb/assets/img/home-bg-webb-2.jpg";
        //     if (System.IO.File.Exists(defaultPath))
        //     {
        //         // LocalImage.Source = new BitmapImage(new Uri(defaultPath));
        //         // PathLabel.Text = defaultPath;
        //     }
        // }

        private void LoadCustomImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg;*.webp;*.gif)|*.png;*.jpeg;*.jpg;*.webp;*.gif|All files (*.*)|*.*";
            
            if (openFileDialog.ShowDialog() == true)
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(openFileDialog.FileName);
                
                // ADVANCED: OnLoad ensures the file isn't "locked" on disk 
                // so you can delete/move the file while the app is running.
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                
                // ADVANCED: Resize during decode to save memory
                bitmap.DecodePixelWidth = 300;

                bitmap.EndInit();

                LocalImage.Source = bitmap;
                PathLabel.Text = openFileDialog.FileName;

                // 2. Extract and display Codec Info
                try
                {
                    // Use FileStream with FileShare.Read to keep the file unlocked
                    using (var stream = new System.IO.FileStream(openFileDialog.FileName, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
                    {
                        // Create a decoder from the stream
                        BitmapDecoder decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                        System.Text.StringBuilder sb = new();

                        System.Windows.Media.Imaging.BitmapCodecInfo codecInfo = decoder.CodecInfo;
                        if (codecInfo != null)
                        {
                            sb.AppendLine($"Friendly Name: {codecInfo.FriendlyName}");
                            sb.AppendLine($"Author: {codecInfo.Author}");
                            sb.AppendLine($"Version: {codecInfo.Version}");
                            sb.AppendLine();
                            
                            // The properties requested from IWICBitmapCodecInfo
                            sb.AppendLine($"Container Format (GUID): {codecInfo.ContainerFormat}");
                            sb.AppendLine($"Device Manufacturer: {codecInfo.DeviceManufacturer}");
                            sb.AppendLine($"Device Models: {codecInfo.DeviceModels}");
                            sb.AppendLine($"MIME Types: {codecInfo.MimeTypes}");
                            sb.AppendLine($"File Extensions: {codecInfo.FileExtensions}");
                            sb.AppendLine();
                            
                            // Boolean capability flags
                            sb.AppendLine($"Supports Animation: {codecInfo.SupportsAnimation}");
                            sb.AppendLine($"Supports Lossless: {codecInfo.SupportsLossless}");
                            sb.AppendLine($"Supports Multiple Frames: {codecInfo.SupportsMultipleFrames}");
                        }
                        else
                        {
                            tb1.Text = "Codec info is not available for this image format.";
                        }

                        sb.AppendLine();
                        sb.AppendLine("=== METADATA ===");

                        // Output to the TextBlock
                        tb1.Text = sb.ToString();                            
                    }
                }
                catch (Exception ex)
                {
                    tb1.Text = $"Could not read codec info: {ex.Message}";
                }
                
            }
        }
        private void SaveFullContent_Click(object sender, RoutedEventArgs e)
        {
            // 1. Determine the "Extent" (the total size of all content)
            // We use the ScrollViewer's ExtentWidth/Height to know how big the image should be
            double width = MyScrollViewer.ExtentWidth;
            double height = MyScrollViewer.ExtentHeight;

            // 2. Create the RenderTargetBitmap with the FULL size
            RenderTargetBitmap rtb = new RenderTargetBitmap(
                (int)width, (int)height, 96, 96, PixelFormats.Pbgra32);

            // 3. IMPORTANT: Re-layout the content to its full size
            // Otherwise, RenderTargetBitmap will only see the small visible viewport
            Size fullSize = new Size(width, height);
            ScrollContent.Measure(fullSize);
            ScrollContent.Arrange(new Rect(fullSize));

            // 4. Render and Save
            rtb.Render(ScrollContent);
            SaveToAllFormats(rtb, "full_scroll_content");

            // 5. Optional: Return the UI to its original "scrolled" state
            // Measure/Arrange again with original constraints if the UI looks weird
            ScrollContent.InvalidateMeasure(); 
        }
        private void OnOneImgSaveClicked(object sender, RoutedEventArgs e)
        {
            // 1. Get the TransformedBitmap from your UI element
            if (FlipRotateDemoImg.Source is BitmapSource bitmapSource)
            {
                SaveToAllFormats(bitmapSource, "output_flipped_rotated");
            }
        }


        private static void SaveToAllFormats(BitmapSource source, string fileName)
        {
            SaveToByEncoder(new PngBitmapEncoder(), source, fileName+".png");
            SaveToByEncoder(new JpegBitmapEncoder(), source, fileName+".jpg");
            SaveToByEncoder(new BmpBitmapEncoder(), source, fileName+".bmp");
            SaveToByEncoder(new TiffBitmapEncoder(), source, fileName+".tiff");
            // SaveToByEncoder(new GifBitmapEncoder(), source, fileName+".gif");
            SaveToByEncoder(new WmpBitmapEncoder(), source, fileName+".wmp");
            MessageBox.Show($"Saved as {fileName}");
        }
    
        private static void SaveToByEncoder(BitmapEncoder encoder, BitmapSource source, string fileName)
        {
            // Use CodecInfo to get metadata about the encoder
            BitmapCodecInfo info = encoder.CodecInfo;
            logger.Info($"Using {info.FriendlyName} by {info.Author} version:{info.Version} spec_version:{info.SpecificationVersion}");

            encoder.Frames.Add(BitmapFrame.Create(source));

            string targetDirectory = "imgsave";
            System.IO.Directory.CreateDirectory(targetDirectory);
            string fullPath = System.IO.Path.Combine(targetDirectory, fileName);
            using var fs = System.IO.File.OpenWrite(fullPath);
            encoder.Save(fs);
        }

    }
}
