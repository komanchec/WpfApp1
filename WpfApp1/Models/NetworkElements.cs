using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfApp1.Models
{
    public abstract class NetworkElement
    {
        protected NetworkElement()
        {
            Id = Guid.NewGuid().ToString();
            Name = string.Empty;
            LayerName = string.Empty;
        }

        public string Id { get; }
        public string Name { get; set; }
        public Point Location { get; set; }
        public double Rotation { get; set; }
        public bool IsSelected { get; set; }
        public string LayerName { get; set; }

        public abstract UIElement CreateVisual();
        public abstract void UpdateVisual(UIElement visual);
    }

    public class PowerLine : NetworkElement
    {
        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }
        public double Voltage { get; set; }
        public string ConductorType { get; set; } = string.Empty;
        public double Load { get; set; }
        public double Length => Math.Sqrt(Math.Pow(EndPoint.X - StartPoint.X, 2) + Math.Pow(EndPoint.Y - StartPoint.Y, 2));

        public override UIElement CreateVisual()
        {
            var line = new Line
            {
                X1 = StartPoint.X,
                Y1 = StartPoint.Y,
                X2 = EndPoint.X,
                Y2 = EndPoint.Y,
                Stroke = GetVoltageColor(),
                StrokeThickness = GetVoltageThickness()
            };
            return line;
        }

        public override void UpdateVisual(UIElement visual)
        {
            if (visual is Line line)
            {
                line.X1 = StartPoint.X;
                line.Y1 = StartPoint.Y;
                line.X2 = EndPoint.X;
                line.Y2 = EndPoint.Y;
                line.Stroke = GetVoltageColor();
                line.StrokeThickness = GetVoltageThickness();
            }
        }

        private Brush GetVoltageColor() => Voltage switch
        {
            0.4 => Brushes.Green,
            10.0 => Brushes.Red,
            34.5 => Brushes.Blue,
            _ => Brushes.Black
        };

        private double GetVoltageThickness() => Voltage switch
        {
            0.4 => 1.0,
            10.0 => 1.5,
            34.5 => 2.0,
            _ => 1.0
        };
    }

    public class Transformer : NetworkElement
    {
        public double Power { get; set; }
        public double PrimaryVoltage { get; set; }
        public double SecondaryVoltage { get; set; }
        public string ConnectionType { get; set; } = string.Empty;
        public double Load { get; set; }
        public double Efficiency { get; set; }

        public override UIElement CreateVisual()
        {
            var group = new Canvas { Width = 60, Height = 60 };

            var body = new Rectangle
            {
                Width = 40,
                Height = 40,
                Fill = Brushes.White,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };

            var primary = new Ellipse
            {
                Width = 20,
                Height = 40,
                Fill = Brushes.Transparent,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };

            var secondary = new Ellipse
            {
                Width = 20,
                Height = 40,
                Fill = Brushes.Transparent,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };

            Canvas.SetLeft(body, 10);
            Canvas.SetTop(body, 10);
            Canvas.SetLeft(primary, 5);
            Canvas.SetTop(primary, 10);
            Canvas.SetLeft(secondary, 35);
            Canvas.SetTop(secondary, 10);

            group.Children.Add(body);
            group.Children.Add(primary);
            group.Children.Add(secondary);

            var powerText = new TextBlock
            {
                Text = $"{Power} kVA",
                FontSize = 10,
                TextAlignment = TextAlignment.Center
            };
            Canvas.SetLeft(powerText, 10);
            Canvas.SetTop(powerText, 45);
            group.Children.Add(powerText);

            return group;
        }

        public override void UpdateVisual(UIElement visual)
        {
            if (visual is Canvas canvas)
            {
                var powerText = canvas.Children.OfType<TextBlock>().FirstOrDefault();
                if (powerText != null)
                {
                    powerText.Text = $"{Power} kVA";
                }
            }
        }

        public double CalculateEfficiency() => Load / Power * 100 switch
        {
            <= 25 => 0.93,
            <= 50 => 0.95,
            <= 75 => 0.96,
            <= 100 => 0.97,
            _ => 0.95
        };
    }


        public override void UpdateVisual(UIElement visual)
        {
            if (visual is Canvas canvas)
            {
                var powerText = canvas.Children.OfType<TextBlock>().FirstOrDefault();
                if (powerText != null)
                {
                    powerText.Text = $"{Power} kVA";
                }
            }
        }

        public double CalculateEfficiency()
        {
            double loadPercentage = (Load / Power) * 100;
            if (loadPercentage <= 25) return 0.93;
            if (loadPercentage <= 50) return 0.95;
            if (loadPercentage <= 75) return 0.96;
            if (loadPercentage <= 100) return 0.97;
            return 0.95;
        }
    }

    public class Pole : NetworkElement
    {
        public string Type { get; set; }
        public double Height { get; set; }
        public double Force { get; set; }
        public List<Insulator> Insulators { get; set; }
        public string Description { get; set; }

        public Pole()
        {
            Insulators = new List<Insulator>();
        }

        public override UIElement CreateVisual()
        {
            var group = new Canvas { Width = 40, Height = 100 };

            var body = new Path
            {
                Fill = Brushes.White,
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                Data = new PathGeometry
                {
                    Figures = new PathFigureCollection
                    {
                        new PathFigure
                        {
                            StartPoint = new Point(15, 0),
                            Segments = new PathSegmentCollection
                            {
                                new LineSegment(new Point(25, 0), true),
                                new LineSegment(new Point(30, 90), true),
                                new LineSegment(new Point(10, 90), true),
                                new LineSegment(new Point(15, 0), true)
                            }
                        }
                    }
                }
            };

            foreach (var insulator in Insulators)
            {
                var arm = CreateInsulatorArm(insulator);
                group.Children.Add(arm);
            }

            group.Children.Add(body);

            var typeText = new TextBlock
            {
                Text = Type,
                FontSize = 10,
                TextAlignment = TextAlignment.Center
            };
            Canvas.SetLeft(typeText, 0);
            Canvas.SetTop(typeText, 95);
            group.Children.Add(typeText);

            return group;
        }

        private UIElement CreateInsulatorArm(Insulator insulator)
        {
            return new Path
            {
                Fill = Brushes.Transparent,
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Data = new PathGeometry
                {
                    Figures = new PathFigureCollection
                    {
                        new PathFigure
                        {
                            StartPoint = new Point(20, insulator.Height),
                            Segments = new PathSegmentCollection
                            {
                                new LineSegment(new Point(40, insulator.Height), true)
                            }
                        }
                    }
                }
            };
        }

        public override void UpdateVisual(UIElement visual)
        {
            if (visual is Canvas canvas)
            {
                var typeText = canvas.Children.OfType<TextBlock>().FirstOrDefault();
                if (typeText != null)
                {
                    typeText.Text = Type;
                }
            }
        }
    }

    public class Insulator
    {
        public string Type { get; set; }
        public double Height { get; set; }
        public double Length { get; set; }
        public string Material { get; set; }
        public double Voltage { get; set; }
    }

    public class Note : NetworkElement
    {
        public string Text { get; set; }
        public double FontSize { get; set; }
        public Color TextColor { get; set; }
        public bool HasFrame { get; set; }

        public override UIElement CreateVisual()
        {
            var group = new Canvas();

            if (HasFrame)
            {
                var frame = new Rectangle
                {
                    Fill = Brushes.White,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    RadiusX = 5,
                    RadiusY = 5
                };
                group.Children.Add(frame);
            }

            var textBlock = new TextBlock
            {
                Text = Text,
                FontSize = FontSize,
                Foreground = new SolidColorBrush(TextColor),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(5)
            };

            group.Children.Add(textBlock);
            return group;
        }

        public override void UpdateVisual(UIElement visual)
        {
            if (visual is Canvas canvas)
            {
                var textBlock = canvas.Children.OfType<TextBlock>().FirstOrDefault();
                if (textBlock != null)
                {
                    textBlock.Text = Text;
                    textBlock.FontSize = FontSize;
                    textBlock.Foreground = new SolidColorBrush(TextColor);
                }
            }
        }
    }

    public class Measurement : NetworkElement
    {
        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }
        public string Unit { get; set; }
        public double Value { get; set; }
        public bool ShowArrows { get; set; }

        public override UIElement CreateVisual()
        {
            var group = new Canvas();

            var line = new Line
            {
                X1 = StartPoint.X,
                Y1 = StartPoint.Y,
                X2 = EndPoint.X,
                Y2 = EndPoint.Y,
                Stroke = Brushes.Blue,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 5, 5 }
            };
            group.Children.Add(line);

            if (ShowArrows)
            {
                AddArrows(group);
            }

            var text = new TextBlock
            {
                Text = $"{Value} {Unit}",
                Foreground = Brushes.Blue,
                FontSize = 12
            };

            Point midPoint = new Point(
                (StartPoint.X + EndPoint.X) / 2,
                (StartPoint.Y + EndPoint.Y) / 2
            );

            Canvas.SetLeft(text, midPoint.X);
            Canvas.SetTop(text, midPoint.Y);
            group.Children.Add(text);

            return group;
        }

        private void AddArrows(Canvas canvas)
        {
            // Ok başları eklenecek
            // TODO: Ok başlarının implementasyonu
        }

        public override void UpdateVisual(UIElement visual)
        {
            if (visual is Canvas canvas)
            {
                var line = canvas.Children.OfType<Line>().FirstOrDefault();
                var text = canvas.Children.OfType<TextBlock>().FirstOrDefault();

                if (line != null)
                {
                    line.X1 = StartPoint.X;
                    line.Y1 = StartPoint.Y;
                    line.X2 = EndPoint.X;
                    line.Y2 = EndPoint.Y;
                }

                if (text != null)
                {
                    text.Text = $"{Value} {Unit}";
                    Canvas.SetLeft(text, (StartPoint.X + EndPoint.X) / 2);
                    Canvas.SetTop(text, (StartPoint.Y + EndPoint.Y) / 2);
                }
            }
        }
    }
}
