using System.Windows;
using System.Windows.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Reflection;


namespace WpfApp1.Models
{
    public abstract class DrawingElement
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public Point Position { get; set; }
        public double Rotation { get; set; }
        public bool IsSelected { get; set; }
        public bool IsVisible { get; set; } = true;
        public string Layer { get; set; }

        public abstract void Draw(DrawingContext context);
        public abstract bool Contains(Point point);
        public abstract Rect GetBounds();
    }
}
