using System;
using System.Collections.Generic;
using System.Linq;
using WpfApp1.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Reflection;


namespace WpfApp1.Calculations
{
    public class ProtectionCoordination
    {
        private readonly List<NetworkElement> elements;
        private readonly Dictionary<string, ProtectionDevice> protectionDevices;
        private readonly double timeMargin = 0.3; // 300ms seçicilik aralığı

        public ProtectionCoordination(List<NetworkElement> elements)
        {
            this.elements = elements;
            protectionDevices = new Dictionary<string, ProtectionDevice>();
            InitializeProtectionDevices();
        }

        private void InitializeProtectionDevices()
        {
            foreach (var element in elements)
            {
                if (element is PowerLine line)
                {
                    var device = CreateProtectionDevice(line);
                    protectionDevices[line.Id] = device;
                }
            }
        }

        public void ValidateCoordination()
        {
            foreach (var line in elements.OfType<PowerLine>())
            {
                var protectionPath = GetProtectionPath(line);
                ValidateProtectionPath(protectionPath);
            }
        }

        private List<ProtectionDevice> GetProtectionPath(PowerLine line)
        {
            var path = new List<ProtectionDevice>();
            var currentElement = line;

            while (currentElement != null)
            {
                if (protectionDevices.TryGetValue(currentElement.Id, out var device))
                {
                    path.Add(device);
                }
                currentElement = FindUpstreamLine(currentElement);
            }

            return path;
        }

        private void ValidateProtectionPath(List<ProtectionDevice> protectionPath)
        {
            for (int i = 0; i < protectionPath.Count - 1; i++)
            {
                var downstream = protectionPath[i];
                var upstream = protectionPath[i + 1];

                ValidateDevicePair(downstream, upstream);
            }
        }
        private void ValidateDevicePair(ProtectionDevice downstream, ProtectionDevice upstream)
        {
            var testPoints = GenerateTestPoints(downstream.Rating);

            foreach (double current in testPoints)
            {
                double downstreamTime = downstream.TripCurve.CalculateTime(current / downstream.Rating);
                double upstreamTime = upstream.TripCurve.CalculateTime(current / upstream.Rating);

                if (upstreamTime - downstreamTime < timeMargin)
                {
                    throw new InvalidOperationException(
                        $"Koruma koordinasyonu hatası! {downstream.Name} ve {upstream.Name} " +
                        $"arasında yeterli seçicilik aralığı yok. Akım: {current:F2}A");
                }
            }
        }

        private List<double> GenerateTestPoints(double rating)
        {
            var points = new List<double>();
            double[] multipliers = { 1.5, 2, 3, 4, 5, 6, 8, 10, 12, 15, 20 };

            foreach (double multiplier in multipliers)
            {
                points.Add(rating * multiplier);
            }

            return points;
        }

        private PowerLine FindUpstreamLine(PowerLine currentLine)
        {
            var startPoint = currentLine.StartPoint;
            return elements.OfType<PowerLine>()
                .FirstOrDefault(l => l.EndPoint == startPoint);
        }

        private ProtectionDevice CreateProtectionDevice(PowerLine line)
        {
            var device = new ProtectionDevice
            {
                Name = $"CB_{line.Name}",
                Type = GetDeviceType(line.Voltage),
                Rating = CalculateDeviceRating(line),
                TripCurve = CreateTripCurve(line.Voltage)
            };

            ConfigureDeviceSettings(device, line);
            return device;
        }
        private ProtectionDeviceType GetDeviceType(double voltage)
        {
            return voltage switch
            {
                <= 0.4 => ProtectionDeviceType.MCCB,
                <= 11 => ProtectionDeviceType.VCB,
                _ => ProtectionDeviceType.SF6
            };
        }

        private double CalculateDeviceRating(PowerLine line)
        {
            double fullLoadCurrent = line.Load / (Math.Sqrt(3) * line.Voltage);
            double rating = fullLoadCurrent * 1.25; // %25 yedek kapasite

            return StandardizeRating(rating);
        }

        private double StandardizeRating(double rating)
        {
            double[] standardRatings = { 16, 25, 32, 40, 63, 80, 100, 125, 160, 200, 250, 315, 400, 630, 800, 1000, 1250, 1600, 2000, 2500, 3150 };

            return standardRatings.First(r => r >= rating);
        }

        private TripCurve CreateTripCurve(double voltage)
        {
            var curve = new TripCurve();

            if (voltage <= 0.4)
            {
                // AG MCCB karakteristiği
                curve.AddPoint(1.05, 7200);  // 2 saat
                curve.AddPoint(1.3, 3600);   // 1 saat
                curve.AddPoint(2.0, 60);     // 60 saniye
                curve.AddPoint(3.0, 20);
                curve.AddPoint(5.0, 5);
                curve.AddPoint(10.0, 0.1);
            }
            else
            {
                // OG kesici karakteristiği
                curve.AddPoint(1.05, 14400); // 4 saat
                curve.AddPoint(1.3, 7200);   // 2 saat
                curve.AddPoint(2.0, 120);    // 120 saniye
                curve.AddPoint(3.0, 30);
                curve.AddPoint(5.0, 10);
                curve.AddPoint(10.0, 0.3);
            }

            return curve;
        }

        private void ConfigureDeviceSettings(ProtectionDevice device, PowerLine line)
        {
            switch (device.Type)
            {
                case ProtectionDeviceType.MCCB:
                    ConfigureMCCBSettings(device, line);
                    break;
                case ProtectionDeviceType.VCB:
                    ConfigureVCBSettings(device, line);
                    break;
                case ProtectionDeviceType.SF6:
                    ConfigureSF6Settings(device, line);
                    break;
            }
        }
        private void ConfigureMCCBSettings(ProtectionDevice device, PowerLine line)
        {
            device.Settings["Ir"] = $"{device.Rating}";
            device.Settings["Isd"] = $"{device.Rating * 8}";
            device.Settings["Ii"] = $"{device.Rating * 12}";
            device.Settings["tsd"] = "0.1";
        }

        private void ConfigureVCBSettings(ProtectionDevice device, PowerLine line)
        {
            device.Settings["I>"] = $"{device.Rating * 1.2}";
            device.Settings["t>"] = "0.3";
            device.Settings["I>>"] = $"{device.Rating * 6}";
            device.Settings["t>>"] = "0.1";
            device.Settings["I>>>"] = $"{device.Rating * 12}";
            device.Settings["t>>>"] = "0.05";
        }

        private void ConfigureSF6Settings(ProtectionDevice device, PowerLine line)
        {
            device.Settings["I>"] = $"{device.Rating * 1.1}";
            device.Settings["t>"] = "0.5";
            device.Settings["I>>"] = $"{device.Rating * 5}";
            device.Settings["t>>"] = "0.15";
            device.Settings["I>>>"] = $"{device.Rating * 10}";
            device.Settings["t>>>"] = "0.08";
        }

        public void GenerateReport()
        {
            var report = new ProtectionReport();

            foreach (var device in protectionDevices.Values)
            {
                var deviceReport = new DeviceSettings
                {
                    DeviceName = device.Name,
                    DeviceType = device.Type,
                    Rating = device.Rating,
                    Settings = new Dictionary<string, string>(device.Settings)
                };

                report.Devices.Add(deviceReport);
            }

            report.SaveToFile("protection_settings.json");
        }
    }

    public class ProtectionReport
    {
        public List<DeviceSettings> Devices { get; set; } = new List<DeviceSettings>();

        public void SaveToFile(string filename)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(this, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            System.IO.File.WriteAllText(filename, json);
        }
    }

    public class DeviceSettings
    {
        public string DeviceName { get; set; }
        public ProtectionDeviceType DeviceType { get; set; }
        public double Rating { get; set; }
        public Dictionary<string, string> Settings { get; set; }
    }
}
