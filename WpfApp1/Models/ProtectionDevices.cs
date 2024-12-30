using System;
using System.Collections.Generic;
using System.Windows;
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
    public enum ProtectionDeviceType
    {
        MCCB,   // Molded Case Circuit Breaker
        VCB,    // Vacuum Circuit Breaker
        SF6,    // SF6 Circuit Breaker
        Fuse    // Sigorta
    }

    public class ProtectionDevice
    {
        public string Name { get; set; }
        public ProtectionDeviceType Type { get; set; }
        public double Rating { get; set; }
        public TripCurve TripCurve { get; set; }
        public bool IsEnabled { get; set; } = true;
        public Dictionary<string, string> Settings { get; set; }

        public ProtectionDevice()
        {
            Settings = new Dictionary<string, string>();
        }

        public void UpdateSettings(string key, string value)
        {
            Settings[key] = value;
            OnSettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler OnSettingsChanged;
    }

    public class TripCurve
    {
        private readonly List<Point> curvePoints;
        private readonly double tolerance;

        public TripCurve(double tolerance = 0.1)
        {
            this.tolerance = tolerance;
            curvePoints = new List<Point>();
        }

        public void AddPoint(double current, double time)
        {
            curvePoints.Add(new Point(current, time));
            curvePoints.Sort((a, b) => a.X.CompareTo(b.X));
        }

        public double CalculateTime(double currentRatio)
        {
            if (curvePoints.Count < 2)
                throw new InvalidOperationException("Trip eğrisi en az 2 nokta içermelidir.");

            // Interpolasyon ile açma süresini hesapla
            for (int i = 0; i < curvePoints.Count - 1; i++)
            {
                if (currentRatio >= curvePoints[i].X && currentRatio <= curvePoints[i + 1].X)
                {
                    return InterpolateTime(curvePoints[i], curvePoints[i + 1], currentRatio);
                }
            }

            // Eğri dışındaki değerler için
            if (currentRatio < curvePoints[0].X)
                return double.PositiveInfinity;

            return curvePoints[^1].Y;
        }

        private double InterpolateTime(Point p1, Point p2, double current)
        {
            double logC1 = Math.Log10(p1.X);
            double logC2 = Math.Log10(p2.X);
            double logT1 = Math.Log10(p1.Y);
            double logT2 = Math.Log10(p2.Y);
            double logC = Math.Log10(current);

            return Math.Pow(10, logT1 + (logT2 - logT1) * (logC - logC1) / (logC2 - logC1));
        }
    }
}
