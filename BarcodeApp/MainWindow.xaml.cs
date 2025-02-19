using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using BarcodeLib;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Controls;
using System.Drawing.Printing;
using FontFamily = System.Windows.Media.FontFamily;

namespace BarcodeApp
{
    public partial class MainWindow : Window
    {
        private Bitmap generatedImage;
        private string generatedCode;
        private string generatedName;
        private string generatedPrice;
        private string selectedFont;

        public MainWindow()
        {
            InitializeComponent();
            outputSection.Visibility = Visibility.Collapsed;

            // Populate the font combo box with available system fonts
            PopulateFonts();
            // Populate the printer combo box with available system fonts
            PopulatePrinters();
        }

        private void PopulateFonts()
        {
            try
            {
                // Populate fonts in the combo box
                var fonts = Fonts.SystemFontFamilies;
                foreach (var font in fonts)
                {
                    cmbFonts.Items.Add(new ComboBoxItem { Content = font.Source });
                }

                // Select the first font and store it
                if (cmbFonts.Items.Count > 0)
                {
                    cmbFonts.SelectedIndex = 0;
                    selectedFont = ((ComboBoxItem)cmbFonts.SelectedItem).Content.ToString();
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

                // Select the default printer
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
                generatedCode = txtCode.Text.Trim();
                generatedName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(txtName.Text.Trim().ToLower());
                generatedPrice = txtPrice.Text.Trim();

                if (string.IsNullOrWhiteSpace(generatedCode))
                {
                    MessageBox.Show("Please enter a code!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (string.IsNullOrWhiteSpace(generatedName))
                {
                    MessageBox.Show("Please enter a Name!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (string.IsNullOrWhiteSpace(generatedPrice))
                {
                    MessageBox.Show("Please enter a Price!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (!decimal.TryParse(generatedPrice, out _))
                {
                    MessageBox.Show("Price must be a valid number!", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Generate QR Code or Barcode
                if (radioQRCode.IsChecked == true)
                {
                    generatedImage = BarcodeGenerator.GenerateQRCode(generatedCode, 250);
                    imgPreview.LayoutTransform = new ScaleTransform(0.5, 0.5); //  scale for QR codes
                }
                else
                {
                    generatedImage = BarcodeGenerator.GenerateBarcode(generatedCode, 300, 100);
                    imgPreview.LayoutTransform = new ScaleTransform(0.8, 0.8); // Scale  for barcodes
                }

                imgPreview.Source = ConvertBitmapToImageSource(generatedImage);
                txtBlockCode.Text = generatedCode;

                // Apply selected font
                selectedFont = cmbFonts.Text;
                txtBlockName.FontFamily = new FontFamily(selectedFont);
                txtBlockPrice.FontFamily = new FontFamily(selectedFont);
                txtBlockCode.FontFamily = new FontFamily(selectedFont);

                // Handle visibility for name and price
                txtBlockName.Text = chkName.IsChecked == true ? generatedName : string.Empty;
                txtBlockName.Visibility = chkName.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

                txtBlockPrice.Text = chkPrice.IsChecked == true ? generatedPrice : string.Empty;
                txtBlockPrice.Visibility = chkPrice.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

                txtBlockOutput.Visibility = Visibility.Collapsed;
                outputSection.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            try
            {
                if (generatedImage == null)
                {
                    MessageBox.Show("Please generate a barcode or QR code first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string selectedSize = cmbPaperSize.Text;
                if (string.IsNullOrEmpty(selectedSize))
                {
                    MessageBox.Show("Please select a paper size.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string selectedPrinter = cmbPrinter.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(selectedPrinter))
                {
                    MessageBox.Show("Please select a printer.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                BarcodeGenerator.PrintBarcode(selectedSize, printableArea, selectedPrinter);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while printing: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}