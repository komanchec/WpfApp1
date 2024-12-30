using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using WpfApp1.Models;
using WpfApp1.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Reflection;


namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private readonly DrawingManager drawingManager;
        private readonly FileService fileService;
        private readonly PrintService printService;
        private double zoomLevel = 1.0;
        private const double ZOOM_FACTOR = 1.2;
        private const double MIN_ZOOM = 0.1;
        private const double MAX_ZOOM = 10.0;
        private Point? lastMousePosition;
        private bool isPanning;

        public MainWindow()
        {
            InitializeComponent();
            drawingManager = new DrawingManager();
            fileService = new FileService(drawingManager);
            printService = new PrintService(drawingManager);
            InitializeEvents();
            InitializeCommands();
            SelectTool.IsChecked = true;
        }

        private void InitializeEvents()
        {
            DrawingCanvas.MouseMove += DrawingCanvas_MouseMove;
            DrawingCanvas.MouseWheel += DrawingCanvas_MouseWheel;
            DrawingCanvas.MouseDown += DrawingCanvas_MouseDown;
            DrawingCanvas.MouseUp += DrawingCanvas_MouseUp;

            SelectTool.Checked += ToolButton_Checked;
            PanTool.Checked += ToolButton_Checked;
            ZoomTool.Checked += ToolButton_Checked;
            LineTool.Checked += ToolButton_Checked;
            TransformerTool.Checked += ToolButton_Checked;
            PoleTool.Checked += ToolButton_Checked;
            NoteTool.Checked += ToolButton_Checked;
            MeasureTool.Checked += ToolButton_Checked;
        }

        private void InitializeCommands()
        {
            CommandBindings.Add(new CommandBinding(ApplicationCommands.New,
                (s, e) => fileService.New()));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Open,
                (s, e) => fileService.Open()));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Save,
                (s, e) => fileService.Save()));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.SaveAs,
                (s, e) => fileService.SaveAs()));
        }

        private void DrawingCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point currentPosition = e.GetPosition(DrawingCanvas);
            CoordinatesStatus.Text = $"X: {currentPosition.X:F0}  Y: {currentPosition.Y:F0}";

            if (isPanning && lastMousePosition.HasValue && e.LeftButton == MouseButtonState.Pressed)
            {
                var delta = currentPosition - lastMousePosition.Value;
                var scrollViewer = DrawingCanvas.Parent as ScrollViewer;
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - delta.X);
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - delta.Y);
                }
            }

            lastMousePosition = currentPosition;
        }

        private void DrawingCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Delta > 0 && zoomLevel < MAX_ZOOM)
                    zoomLevel *= ZOOM_FACTOR;
                else if (e.Delta < 0 && zoomLevel > MIN_ZOOM)
                    zoomLevel /= ZOOM_FACTOR;

                CanvasScale.ScaleX = zoomLevel;
                CanvasScale.ScaleY = zoomLevel;
                ZoomStatus.Text = $"Zoom: {zoomLevel * 100:F0}%";

                e.Handled = true;
            }
        }

        private void DrawingCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (PanTool.IsChecked == true)
            {
                isPanning = true;
                DrawingCanvas.Cursor = Cursors.Hand;
            }
        }

        private void DrawingCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isPanning = false;
            DrawingCanvas.Cursor = Cursors.Arrow;
        }

        private void ToolButton_Checked(object sender, RoutedEventArgs e)
        {
            var button = sender as ToggleButton;
            if (button?.IsChecked == true)
            {
                foreach (var child in (button.Parent as ToolBar)?.Items ?? new UIElement[] { })
                {
                    if (child is ToggleButton otherButton && otherButton != button)
                    {
                        otherButton.IsChecked = false;
                    }
                }

                ToolStatus.Text = $"Araç: {button.ToolTip}";
                UpdateCursor(button.Name);
            }
        }

        private void UpdateCursor(string toolName)
        {
            DrawingCanvas.Cursor = toolName switch
            {
                "PanTool" => Cursors.Hand,
                "ZoomTool" => Cursors.SizeAll,
                "LineTool" => Cursors.Cross,
                "TransformerTool" => Cursors.Cross,
                "PoleTool" => Cursors.Cross,
                "NoteTool" => Cursors.IBeam,
                "MeasureTool" => Cursors.Cross,
                _ => Cursors.Arrow
            };
        }
    }
}
