using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ZXing;
using ZXing.Common;
using ZXing.Rendering;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Drawing.Printing;

namespace BarcodeLib
{
    public class BarcodeGenerator
    {
        public static System.Drawing.Bitmap GenerateBarcode(string code, int width = 300, int height = 100)
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

        public static System.Drawing.Bitmap GenerateQRCode(string code, int size = 250)
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

        private static System.Drawing.Bitmap PixelDataToBitmap(PixelData pixelData)
        {
            if (pixelData == null || pixelData.Pixels == null || pixelData.Pixels.Length == 0)
                return null;

            var bmp = new System.Drawing.Bitmap(pixelData.Width, pixelData.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            try
            {
                var data = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                    System.Drawing.Imaging.ImageLockMode.WriteOnly, bmp.PixelFormat);
                System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, data.Scan0, pixelData.Pixels.Length);
                bmp.UnlockBits(data);
                return new System.Drawing.Bitmap(bmp);
            }
            finally
            {
                bmp.Dispose();
            }
        }



        public static void PrintBarcode(string paperSize, StackPanel layoutToPrint, string selectedPrinter)
        {
            try
            {
                Match match = Regex.Match(paperSize, @"(\d+)\s*x\s*(\d+)");
                if (!match.Success)
                {
                    MessageBox.Show("Invalid paper size format. Use 'WidthxHeight' (e.g., '90x60' or 'A9 (37x52)').", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
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

                        printDocument.Print();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}