using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Globalization;
using System.Drawing.Printing;
using ZXing;
using ZXing.Common;
using ZXing.Rendering;
using System.Text.RegularExpressions;
using FontFamily = System.Windows.Media.FontFamily;
using System.Windows.Controls;
using System.Windows.Forms;
using UserControl = System.Windows.Controls.UserControl;
using MessageBox = System.Windows.MessageBox;
using PrintDialog = System.Windows.Forms.PrintDialog;
using System.Diagnostics;


namespace BarcodeGenerator

{
    public class BarcodeModel
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public string Price { get; set; }
        public FontFamily CodeFont { get; set; }
        public string LabelPrinterName { get; set; }
        public bool IncludeItemName { get; set; }
        public bool IncludePrice { get; set; }
        public string PaperSize { get; set; }
        public StackPanel StackPanel { get; set; }
        public TextBlock BlockOutput { get; set; }
        public string SelectedPrinter { get; set; }
        public Bitmap GeneratedImage { get; set; }

        public clsResponse Validate()
        {
            if (string.IsNullOrWhiteSpace(Code))
            {
                return new clsResponse { Message = "Code is required!", Success = false };
            }
            if (!Regex.IsMatch(Code, @"^[a-zA-Z0-9]+$"))
            {
                return new clsResponse { Message = "Code must be alphanumeric!", Success = false };
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                return new clsResponse { Message = "Item Name is required!", Success = false };
            }

            if (string.IsNullOrWhiteSpace(Price))
            {
                return new clsResponse { Message = "Price is required!", Success = false };
            }
            
            if (!Regex.IsMatch(PaperSize, @"(\d+)\s*x\s*(\d+)"))
            {
                return new clsResponse { Message = "Invalid paper size format. Use 'WidthxHeight' (e.g., '90x60' or 'A9 (37x52)').", Success = false };
            }
            if (string.IsNullOrEmpty(PaperSize))
            {
                return new clsResponse { Message = "Please select a paper size.", Success = false };
            }

            if (string.IsNullOrEmpty(SelectedPrinter))
            {
                return new clsResponse { Message = "Please select a printer.", Success = false };
            }
            if (GeneratedImage == null)
            {
                return new clsResponse { Message = "Please generate a Barcode or QR code first.", Success = false };

            }
            if (!StackPanel.IsVisible)
            {
                return new clsResponse { Message = "Please the output must first be initialized. Generate a Barcode or QR code first.", Success = false };

            }
            return new clsResponse { Message = "Validation successful.", Success = true };
        }
    }

    // Response class for validation
    public class clsResponse
    {
        public string Message { get; set; }
        public bool Success { get; set; }
    }

    public partial class BarcodeGenerator : UserControl
    {
        public BarcodeModel model;

        public BarcodeGenerator()
        {

            InitializeComponent();
            outputSection.Visibility = Visibility.Collapsed;
            PopulateFonts();
            PopulatePrinters();
        }

        private void PopulateFonts()
        {
            try
            {
                var fonts = Fonts.SystemFontFamilies;
                foreach (var font in fonts)
                {
                    cmbFonts.Items.Add(new ComboBoxItem { Content = font.Source });
                }

                if (cmbFonts.Items.Count > 0)
                {
                    cmbFonts.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show($"Error loading fonts: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulatePrinters()
        {
            try
            {
                var printers = PrinterSettings.InstalledPrinters;
                foreach (string printer in printers)
                {
                    cmbPrinter.Items.Add(printer);
                }

                string defaultPrinter = new PrinterSettings().PrinterName;
                cmbPrinter.SelectedItem = defaultPrinter;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading printers: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                model = new BarcodeModel
                {
                    Code = txtCode.Text.Trim(),
                    Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(txtName.Text.Trim().ToLower()),
                    Price = txtPrice.Text.Trim(),
                    CodeFont = new FontFamily(cmbFonts.Text),
                    LabelPrinterName = txtName.Text.Trim(),
                    IncludeItemName = chkName.IsChecked == true,
                    IncludePrice = chkPrice.IsChecked == true,
                    PaperSize = cmbPaperSize.SelectedItem.ToString(),
                    BlockOutput = txtBlockOutput,
                    StackPanel = outputSection,
                    SelectedPrinter = cmbPrinter.SelectedItem.ToString(),
                    GeneratedImage = radioQRCode.IsChecked == true ? GenerateQRCode(txtCode.Text.Trim(), 250) : GenerateBarcode(txtCode.Text.Trim(), 300, 100)
                };

                if (model.GeneratedImage != null&& !string.IsNullOrWhiteSpace(model.Price))
                {
                    model.StackPanel.Visibility = Visibility.Visible;
                    model.Price = 'N' + model.Price;
                }

                var validationResponse = model.Validate();
                if (!validationResponse.Success)
                {
                    MessageBox.Show(validationResponse.Message, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                Console.WriteLine(model.PaperSize);

                if (radioQRCode.IsChecked == true)
                {
                    imgPreview.LayoutTransform = new ScaleTransform(0.5, 0.5);
                }
                else
                {
                    imgPreview.LayoutTransform = new ScaleTransform(0.8, 0.8);
                }

                imgPreview.Source = ConvertBitmapToImageSource(model.GeneratedImage);
                txtBlockCode.Text = model.Code;


                txtBlockName.FontFamily = new FontFamily(model.CodeFont.ToString());
                txtBlockPrice.FontFamily = new FontFamily(model.CodeFont.ToString());
                txtBlockCode.FontFamily = new FontFamily(model.CodeFont.ToString());

                txtBlockName.Text = chkName.IsChecked == true ? model.Name : string.Empty;
                txtBlockName.Visibility = chkName.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

                txtBlockPrice.Text = chkPrice.IsChecked == true ? model.Price : string.Empty;
                txtBlockPrice.Visibility = chkPrice.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

                model.BlockOutput.Visibility = Visibility.Collapsed;
                model.StackPanel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Bitmap GenerateBarcode(string code, int width = 300, int height = 100)
        {
            if (string.IsNullOrWhiteSpace(code) || width <= 0 || height <= 0)
                return null;

            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Width = width,
                    Height = height,
                    Margin = 2
                }
            };

            var pixelData = writer.Write(code);
            return pixelData != null && pixelData.Pixels.Length > 0 ? PixelDataToBitmap(pixelData) : null;
        }

        private Bitmap GenerateQRCode(string code, int size = 250)
        {
            if (string.IsNullOrWhiteSpace(code) || size <= 0)
                return null;

            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions
                {
                    Width = size,
                    Height = size,
                    Margin = 2
                }
            };

            var pixelData = writer.Write(code);
            return pixelData != null && pixelData.Pixels.Length > 0 ? PixelDataToBitmap(pixelData) : null;
        }

        private Bitmap PixelDataToBitmap(PixelData pixelData)
        {
            if (pixelData == null || pixelData.Pixels == null || pixelData.Pixels.Length == 0)
                return null;

            var bmp = new Bitmap(pixelData.Width, pixelData.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            try
            {
                var data = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                    System.Drawing.Imaging.ImageLockMode.WriteOnly, bmp.PixelFormat);
                System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, data.Scan0, pixelData.Pixels.Length);
                bmp.UnlockBits(data);
                return new Bitmap(bmp);
            }
            finally
            {
                bmp.Dispose();
            }
        }

        private BitmapImage ConvertBitmapToImageSource(Bitmap bitmap)
        {
            try
            {
                using (MemoryStream memory = new MemoryStream())
                {
                    bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                    memory.Position = 0;
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    return bitmapImage;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to convert image: " + ex.Message, "Image Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            model = new BarcodeModel
            {
                Code = txtCode.Text.Trim(),
                Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(txtName.Text.Trim().ToLower()),
                Price = txtPrice.Text.Trim(),
                CodeFont = new FontFamily(cmbFonts.Text),
                LabelPrinterName = txtName.Text.Trim(),
                IncludeItemName = chkName.IsChecked == true,
                IncludePrice = chkPrice.IsChecked == true,
                PaperSize = cmbPaperSize.SelectedItem?.ToString(),
                StackPanel = outputSection,
                BlockOutput = txtBlockOutput,
                SelectedPrinter = cmbPrinter.SelectedItem.ToString(),
                GeneratedImage = radioQRCode.IsChecked == true ? GenerateQRCode(txtCode.Text.Trim(), 250) : GenerateBarcode(txtCode.Text.Trim(), 300, 100)
            };
            var validationResponse = model.Validate();
            if (!validationResponse.Success)
            {
                MessageBox.Show(validationResponse.Message, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {

                PrintBarcode(model.PaperSize, printableArea, model.SelectedPrinter);
                model.BlockOutput.Visibility = Visibility.Visible;
                model.StackPanel.Visibility = Visibility.Collapsed;

            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while printing: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void PrintBarcode(string paperSize, StackPanel layoutToPrint, string selectedPrinter)
        {

            try
            {
                Match match = Regex.Match(paperSize, @"(\d+)\s*x\s*(\d+)");
                if (!match.Success)
                {
                    MessageBox.Show("Invalid paper size format. Use 'WidthxHeight' (e.g., '90x60').", "Print Warning", MessageBoxButton.OK, MessageBoxImage.Warning);

                }

                int paperWidth = int.Parse(match.Groups[1].Value) * 10;
                int paperHeight = int.Parse(match.Groups[2].Value) * 10;

                TextOptions.SetTextRenderingMode(layoutToPrint, TextRenderingMode.ClearType);

                int dpi = 600;
                int renderWidth = (int)(layoutToPrint.ActualWidth * dpi / 96);
                int renderHeight = (int)(layoutToPrint.ActualHeight * dpi / 96);

                RenderTargetBitmap rtb = new RenderTargetBitmap(renderWidth, renderHeight, dpi, dpi, PixelFormats.Pbgra32);
                layoutToPrint.Measure(new System.Windows.Size(layoutToPrint.ActualWidth, layoutToPrint.ActualHeight));
                layoutToPrint.Arrange(new Rect(new System.Windows.Size(layoutToPrint.ActualWidth, layoutToPrint.ActualHeight)));
                rtb.Render(layoutToPrint);

                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));

                using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
                {
                    encoder.Save(stream);
                    using (Bitmap bitmap = new Bitmap(stream))
                    {
                        PrintDocument printDocument = new PrintDocument();
                        printDocument.PrinterSettings.PrinterName = selectedPrinter;
                        printDocument.DefaultPageSettings.PaperSize = new PaperSize("Custom", paperWidth, paperHeight);

                        printDocument.PrintPage += (s, ev) =>
                        {
                            try
                            {
                                int pageWidth = ev.PageBounds.Width;
                                int pageHeight = ev.PageBounds.Height;

                                float scale = Math.Min((float)pageWidth / bitmap.Width, (float)pageHeight / bitmap.Height);
                                int imgWidth = (int)(bitmap.Width * scale);
                                int imgHeight = (int)(bitmap.Height * scale);
                                int posX = (pageWidth - imgWidth) / 2;
                                int posY = (pageHeight - imgHeight) / 2;

                                ev.Graphics.DrawImage(bitmap, posX, posY, imgWidth, imgHeight);

                            }
                            catch (Exception ex)
                            {

                                MessageBox.Show("Printing error: " + ex.Message, "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        };

                        PrintDialog printDialog = new PrintDialog();
                        printDialog.PrinterSettings = printDocument.PrinterSettings;
                        if (printDialog.ShowDialog() == DialogResult.OK)
                        {
                            printDocument.Print();
                            MessageBox.Show("Printing successful.", "Print Success", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }


                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message, "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);

            }
        }
    }
}
