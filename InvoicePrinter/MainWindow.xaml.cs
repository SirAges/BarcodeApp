using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing.Printing;
using ZXing;
using ZXing.Common;
using ZXing.Rendering;
using System.Text.RegularExpressions;
using FontFamily = System.Windows.Media.FontFamily;
using System.Windows.Controls;



namespace InvoicePrinter;

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
    public string SelectedPrinter { get; set; }
    public bool isQrCode { get; set; }

    public clsResponse Validate()
    {
        var installedPrinters = PrinterSettings.InstalledPrinters.Cast<string>().ToList();

        if (string.IsNullOrWhiteSpace(Code))
        {
            return new clsResponse { Message = "Please input code for this item", Success = false };
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            return new clsResponse { Message = "Please input a name for this item. You are recommended to use meaningful names", Success = false };
        }

        if (string.IsNullOrWhiteSpace(Price))
        {
            return new clsResponse { Message = "Please input a price for this item.", Success = false };
        }

        if (!Regex.IsMatch(PaperSize, @"(\d+)\s*x\s*(\d+)"))
        {
            return new clsResponse { Message = "Invalid paper size format. Use 'WidthxHeight' (e.g., '90x60' or 'A9 (37x52)').", Success = false };
        }
        if (string.IsNullOrEmpty(PaperSize))
        {
            return new clsResponse { Message = "Please select a paper size.", Success = false };
        }

        if (!string.IsNullOrEmpty(SelectedPrinter) && !installedPrinters.Contains(SelectedPrinter))
        {
            return new clsResponse { Message = "Printer is not installed on device. Please use an available printer.", Success = false };
        }


        return new clsResponse { Message = "Validation successful.", Success = true };
    }
}

public class clsResponse
{
    public string Message { get; set; }
    public bool Success { get; set; }
}
public partial class MainWindow : Window
{
    public BarcodeModel model;

    public MainWindow()
    {
        InitializeComponent();
         model = new BarcodeModel();
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

            if (model == null)
            {
                BarcodeModel model = new BarcodeModel();

            }

            model.Code = txtCode.Text.Trim();
            model.Name = txtName.Text.Trim();
            model.Price = txtPrice.Text.Trim();
            model.CodeFont = new FontFamily(cmbFonts.Text);
            model.IncludePrice = chkPrice.IsChecked == true;
            model.PaperSize = cmbPaperSize.SelectedItem.ToString();
            model.LabelPrinterName = txtName.Text.Trim();
            model.IncludeItemName = chkName.IsChecked == true;
            model.SelectedPrinter = cmbPrinter.SelectedItem.ToString();
            model.isQrCode = (bool)radioQRCode.IsChecked;
            GenerateBarcodeModel(model);
        }
        catch (Exception ex)
        {
            MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public clsResponse GenerateBarcodeModel(BarcodeModel model)
    {


        if (!string.IsNullOrWhiteSpace(model.Price) && decimal.TryParse(model.Price, out _))
        {
            outputSection.Visibility = Visibility.Visible;
        }

        var validationResponse = model.Validate();
        if (!validationResponse.Success)
        {
            MessageBox.Show(validationResponse.Message, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return new clsResponse { Message = validationResponse.Message, Success = false };
            ;
        }


        txtBlockName.Text = chkName.IsChecked == true ? model.Name : string.Empty;
        txtBlockPrice.Text = chkPrice.IsChecked == true ? model.Price : string.Empty;
        txtBlockName.Visibility = chkName.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        txtBlockPrice.Visibility = chkPrice.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        txtBlockOutput.Visibility = Visibility.Collapsed;
        outputSection.Visibility = Visibility.Visible;

        txtBlockCode.Text = model.Code;
        txtBlockName.FontFamily = model.CodeFont;
        txtBlockPrice.FontFamily = model.CodeFont;
        txtBlockCode.FontFamily = model.CodeFont;
        txtCode.Text = model.Code;
        txtName.Text = model.Name;
        cmbFonts.Text = model.CodeFont?.Source;
        cmbPrinter.SelectedItem = model.SelectedPrinter;
        cmbPaperSize.SelectedItem = model.PaperSize;
        chkPrice.IsChecked = model.IncludePrice;
        chkName.IsChecked = model.IncludeItemName;
        radioQRCode.IsChecked = model.isQrCode;

        imgPreview.LayoutTransform = model.isQrCode == true ? new ScaleTransform(0.5, 0.5) : new ScaleTransform(0.8, 0.8);
        Bitmap generatedImage = model.isQrCode == true ? GenerateBarcodeImage(model.Code, model.isQrCode, 250) : GenerateBarcodeImage(model.Code, model.isQrCode, 300, 100);
        imgPreview.Source = ConvertBitmapToImageSource(generatedImage);
        if (imgPreview.Source != null)
        {
            return new clsResponse { Message = "Code generated successfully", Success = true };

        }
        return new clsResponse { Message = "Code generation failed", Success = false };

    }


    private Bitmap GenerateBarcodeImage(string code, bool isQRCode, int width, int height = 0)
    {
        if (string.IsNullOrWhiteSpace(code) || width <= 0 || height < 0)
            return null;

        var writer = new BarcodeWriterPixelData
        {
            Format = isQRCode ? BarcodeFormat.QR_CODE : BarcodeFormat.CODE_128,
            Options = new EncodingOptions
            {
                Width = width,
                Height = isQRCode ? width : height,
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

        if (model == null)
        {
            BarcodeModel model = new BarcodeModel();

        }

        if (!outputSection.IsVisible)
        {
            MessageBox.Show("Printing error: please generate a QRCODE or BARCODE to print", "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        model.PaperSize = cmbPaperSize.SelectedItem.ToString();
        model.Price = txtPrice.Text.Trim();
        model.LabelPrinterName = txtName.Text.Trim();
        model.IncludeItemName = chkName.IsChecked == true;
        model.SelectedPrinter = cmbPrinter.SelectedItem.ToString();
        if (!outputSection.IsVisible)
        {
            MessageBox.Show("Printing error: please generate a QRCODE or BARCODE to print", "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var validationResponse = model.Validate();
        if (!validationResponse.Success)
        {
            MessageBox.Show(validationResponse.Message, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        PrintBarcode(model);
    }

    public void PrintBarcode(BarcodeModel model)
    {
        try
        {


            Match match = Regex.Match(model.PaperSize, @"(\d+)\s*x\s*(\d+)");
            if (!match.Success)
            {
                MessageBox.Show("Invalid paper size format. Use 'WidthxHeight' (e.g., '60x40').", "Print Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            double conversionFactor = 100.0 / 25.4;
            int paperWidth = (int)(int.Parse(match.Groups[1].Value) * conversionFactor);
            int paperHeight = (int)(int.Parse(match.Groups[2].Value) * conversionFactor);

            TextOptions.SetTextRenderingMode(printableArea, TextRenderingMode.ClearType);

            printableArea.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            printableArea.Arrange(new Rect(printableArea.DesiredSize));

            int dpi = 600;
            int renderWidth = (int)(printableArea.ActualWidth * dpi / 96);
            int renderHeight = (int)(printableArea.ActualHeight * dpi / 96);

            if (renderWidth <= 0 || renderHeight <= 0)
            {
                MessageBox.Show("Error: Render size is invalid. Printable area is not fully loaded.", "Rendering Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            RenderTargetBitmap rtb = new RenderTargetBitmap(renderWidth, renderHeight, dpi, dpi, PixelFormats.Pbgra32);
            rtb.Render(printableArea);

            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using (MemoryStream stream = new MemoryStream())
            {
                encoder.Save(stream);
                using (Bitmap bitmap = new Bitmap(stream))
                {
                    PrintDocument printDocument = new PrintDocument();

                    printDocument.PrinterSettings.PrinterName = model.SelectedPrinter;
                    printDocument.DocumentName = model.Name + " Invoice";
                    printDocument.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
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
                            ev.HasMorePages = false;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                            MessageBox.Show("Printing error: " + ex.Message, "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    };
                    if (string.IsNullOrWhiteSpace(model.SelectedPrinter))
                    {
                        PrintDialog printDialog = new PrintDialog();

                        bool? printIsClicked = printDialog.ShowDialog();
                        if (printIsClicked == true)
                        {
                            printDocument.Print();
                        }
                    }
                    else
                    {
                        printDocument.Print();

                    }

                    txtBlockOutput.Visibility = Visibility.Visible;
                    outputSection.Visibility = Visibility.Collapsed;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("An error occurred while printing: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }



}