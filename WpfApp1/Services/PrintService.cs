using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Printing;
using WpfApp1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Reflection;


namespace WpfApp1.Services
{
    public class PrintService
    {
        private readonly DrawingManager drawingManager;

        public PrintService(DrawingManager drawingManager)
        {
            this.drawingManager = drawingManager;
        }

        public void Print()
        {
            var dialog = new PrintDialog();
            if (dialog.ShowDialog() == true)
            {
                dialog.PrintVisual(CreatePrintVisual(), "Elektrik Şebekesi");
            }
        }

        public void PrintPreview()
        {
            var window = new Window
            {
                Title = "Baskı Önizleme",
                Width = 800,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var viewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = CreatePrintVisual()
            };

            window.Content = viewer;
            window.ShowDialog();
        }

        private Visual CreatePrintVisual()
        {
            var canvas = new Canvas
            {
                Width = 794,  // A4 width in pixels at 96 DPI
                Height = 1123, // A4 height in pixels at 96 DPI
                Background = Brushes.White
            };

            var dc = ((DrawingVisual)canvas.GetVisualChild(0)).RenderOpen();

            // Draw page border
            dc.DrawRectangle(null, new Pen(Brushes.Black, 1),
                new Rect(20, 20, canvas.Width - 40, canvas.Height - 40));

            // Draw title
            var titleText = new FormattedText(
                "Elektrik Şebekesi Tasarımı",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                16,
                Brushes.Black,
                VisualTreeHelper.GetDpi(canvas).PixelsPerDip);

            dc.DrawText(titleText, new Point(
                (canvas.Width - titleText.Width) / 2,
                40));

            // Scale and center the drawing
            var bounds = GetDrawingBounds();
            var scale = Math.Min(
                (canvas.Width - 80) / bounds.Width,
                (canvas.Height - 120) / bounds.Height);

            var transform = new TransformGroup();
            transform.Children.Add(new ScaleTransform(scale, scale));
            transform.Children.Add(new TranslateTransform(
                40 - bounds.X * scale + (canvas.Width - 80 - bounds.Width * scale) / 2,
                80 - bounds.Y * scale + (canvas.Height - 120 - bounds.Height * scale) / 2));

            dc.PushTransform(transform);
            drawingManager.Draw(dc);
            dc.Pop();

            dc.Close();
            return canvas;
        }

        private Rect GetDrawingBounds()
        {
            var bounds = Rect.Empty;
            foreach (var element in drawingManager.Elements)
            {
                bounds.Union(element.GetBounds());
            }
            return bounds;
        }
    }
}
