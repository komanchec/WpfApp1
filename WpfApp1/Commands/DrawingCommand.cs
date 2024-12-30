using System.Windows.Input;
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
    public static class DrawingCommands
    {
        public static readonly RoutedUICommand ZoomIn = new RoutedUICommand(
            "Zoom In",
            "ZoomIn",
            typeof(DrawingCommands),
            new InputGestureCollection { new KeyGesture(Key.OemPlus, ModifierKeys.Control) }
        );

        public static readonly RoutedUICommand ZoomOut = new RoutedUICommand(
            "Zoom Out",
            "ZoomOut",
            typeof(DrawingCommands),
            new InputGestureCollection { new KeyGesture(Key.OemMinus, ModifierKeys.Control) }
        );

        public static readonly RoutedUICommand ZoomFit = new RoutedUICommand(
            "Zoom Fit",
            "ZoomFit",
            typeof(DrawingCommands),
            new InputGestureCollection { new KeyGesture(Key.D0, ModifierKeys.Control) }
        );

        public static readonly RoutedUICommand Pan = new RoutedUICommand(
            "Pan",
            "Pan",
            typeof(DrawingCommands)
        );

        public static readonly RoutedUICommand AddLine = new RoutedUICommand(
            "Add Line",
            "AddLine",
            typeof(DrawingCommands)
        );

        public static readonly RoutedUICommand AddTransformer = new RoutedUICommand(
            "Add Transformer",
            "AddTransformer",
            typeof(DrawingCommands)
        );

        public static readonly RoutedUICommand AddPole = new RoutedUICommand(
            "Add Pole",
            "AddPole",
            typeof(DrawingCommands)
        );
    }
}
