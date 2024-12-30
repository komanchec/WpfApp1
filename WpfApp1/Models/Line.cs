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
    public class Line : DrawingElement
    {
        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }
        public double Thickness { get; set; } = 2.0;
        public Color Color { get; set; } = Colors.Black;
        public LineType Type { get; set; }
        public double Voltage { get; set; }
        public double Current { get; set; }
        public string ConductorType { get; set; }

        public override void Draw(DrawingContext context)
        {
            var pen = new Pen(new SolidColorBrush(Color), Thickness);
            if (Type == LineType.Dashed)
            {
                pen.DashStyle = new DashStyle(new double[] { 4, 2 }, 0);
            }
            context.DrawLine(pen, StartPoint, EndPoint);

            if (IsSelected)
            {
                DrawSelectionHandles(context);
            }
        }

        private void DrawSelectionHandles(DrawingContext context)
        {
            var handleBrush = new SolidColorBrush(Colors.Blue);
            var handleSize = 6.0;

            context.DrawRectangle(handleBrush, null,
                new Rect(StartPoint.X - handleSize / 2, StartPoint.Y - handleSize / 2, handleSize, handleSize));
            context.DrawRectangle(handleBrush, null,
                new Rect(EndPoint.X - handleSize / 2, EndPoint.Y - handleSize / 2, handleSize, handleSize));
        }

        public override bool Contains(Point point)
        {
            var distance = DistanceToLine(point);
            return distance <= Thickness + 2;
        }

        private double DistanceToLine(Point point)
        {
            double length = Point.Subtract(EndPoint, StartPoint).Length;
            if (length == 0) return Point.Subtract(point, StartPoint).Length;

            double t = Vector.Multiply(Point.Subtract(point, StartPoint),
                                    Point.Subtract(EndPoint, StartPoint)) / (length * length);

            if (t < 0) return Point.Subtract(point, StartPoint).Length;
            if (t > 1) return Point.Subtract(point, EndPoint).Length;

            var projection = Point.Add(StartPoint,
                Point.Multiply(Point.Subtract(EndPoint, StartPoint), t));
            return Point.Subtract(point, projection).Length;
        }

        public override Rect GetBounds()
        {
            var left = Math.Min(StartPoint.X, EndPoint.X);
            var top = Math.Min(StartPoint.Y, EndPoint.Y);
            var right = Math.Max(StartPoint.X, EndPoint.X);
            var bottom = Math.Max(StartPoint.Y, EndPoint.Y);

            return new Rect(new Point(left, top), new Point(right, bottom));
        }
    }

    public enum LineType
    {
        Solid,
        Dashed
    }
}
