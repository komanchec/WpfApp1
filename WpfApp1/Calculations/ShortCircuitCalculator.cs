using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;
using WpfApp1.Models;
using System.Linq;

namespace WpfApp1.Calculations
{
    public class ShortCircuitCalculator
    {
        private readonly List<NetworkElement> elements;
        private readonly Dictionary<string, Complex> impedances;
        private readonly Dictionary<string, double> shortCircuitCurrents;
        private readonly double systemVoltage;
        private readonly double systemPower;

        public ShortCircuitCalculator(List<NetworkElement> elements, double voltage, double power)
        {
            this.elements = elements;
            this.systemVoltage = voltage;
            this.systemPower = power;
            impedances = new Dictionary<string, Complex>();
            shortCircuitCurrents = new Dictionary<string, double>();
        }

        public void CalculateShortCircuit()
        {
            CalculateSystemBaseImpedance();
            CalculateElementImpedances();
            CalculateTotalImpedance();
            CalculateFaultCurrents();
            ValidateProtectionDevices();
        }

        private void CalculateSystemBaseImpedance()
        {
            double baseImpedance = Math.Pow(systemVoltage, 2) / systemPower;
            impedances["SYSTEM"] = new Complex(0.1 * baseImpedance, baseImpedance);
        }

        private void CalculateElementImpedances()
        {
            foreach (var element in elements)
            {
                switch (element)
                {
                    case PowerLine line:
                        CalculateLineImpedance(line);
                        break;
                    case Transformer transformer:
                        CalculateTransformerImpedance(transformer);
                        break;
                }
            }
        }

        private void CalculateLineImpedance(PowerLine line)
        {
            double resistance = GetLineResistance(line);
            double reactance = GetLineReactance(line);

            // Per-unit dönüşümü
            double baseImpedance = Math.Pow(line.Voltage, 2) / systemPower;
            double rPU = resistance / baseImpedance;
            double xPU = reactance / baseImpedance;

            impedances[line.Id] = new Complex(rPU, xPU);
        }

        private void CalculateTransformerImpedance(Transformer transformer)
        {
            // Trafo empedansı hesaplama
            double baseImpedance = Math.Pow(transformer.PrimaryVoltage, 2) / transformer.Power;
            double impedancePercent = 0.05; // %5 kısa devre empedansı

            Complex zPU = new Complex(
                impedancePercent * 0.3, // R/X oranı yaklaşık 0.3
                impedancePercent * 0.95
            );

            impedances[transformer.Id] = zPU;
        }

        private void CalculateTotalImpedance()
        {
            foreach (var element in elements)
            {
                if (element is PowerLine line)
                {
                    Complex totalZ = impedances["SYSTEM"];

                    // Hat boyunca empedansları topla
                    var path = FindPathToSource(line);
                    foreach (var pathElement in path)
                    {
                        totalZ += impedances[pathElement.Id];
                    }

                    impedances[$"TOTAL_{line.Id}"] = totalZ;
                }
            }
        }

        private void CalculateFaultCurrents()
        {
            double baseVoltage = systemVoltage / Math.Sqrt(3); // Faz-nötr gerilimi
            double baseCurrent = systemPower / (Math.Sqrt(3) * systemVoltage);

            foreach (var element in elements)
            {
                if (element is PowerLine line)
                {
                    Complex totalZ = impedances[$"TOTAL_{line.Id}"];
                    double faultCurrent = baseVoltage / totalZ.Magnitude * baseCurrent;
                    shortCircuitCurrents[line.Id] = faultCurrent;
                }
            }
        }

        private void ValidateProtectionDevices()
        {
            foreach (var element in elements)
            {
                if (element is PowerLine line)
                {
                    double faultCurrent = shortCircuitCurrents[line.Id];
                    double breakingCapacity = GetBreakerRating(line.Voltage);

                    if (faultCurrent > breakingCapacity)
                    {
                        throw new InvalidOperationException(
                            $"Hat {line.Name} için kesici kapasitesi yetersiz! " +
                            $"Gerekli: {faultCurrent:F2} kA, Mevcut: {breakingCapacity:F2} kA");
                    }

                    // Koruma koordinasyonu kontrolü
                    ValidateProtectionCoordination(line, faultCurrent);
                }
            }
        }

        private List<NetworkElement> FindPathToSource(PowerLine targetLine)
        {
            var path = new List<NetworkElement>();
            var currentElement = (NetworkElement)targetLine; // Açık dönüşüm

            while (currentElement != null)
            {
                path.Add(currentElement);
                currentElement = FindUpstreamElement(currentElement);
            }

            path.Reverse();
            return path;
        }

        private NetworkElement FindUpstreamElement(NetworkElement current)
        {
            if (current is PowerLine line)
            {
                // Başlangıç noktasına bağlı elemanı bul
                return elements.Find(e =>
                    e is PowerLine pl && pl.EndPoint == line.StartPoint ||
                    e is Transformer t && new Point(t.Location.X, t.Location.Y) == line.StartPoint)
                    ?? throw new InvalidOperationException("Bağlantılı eleman bulunamadı."); // Olası null dönüşüm hatası için
            }
            return null; // Bu durumda null dönebilir
        }

        private double GetBreakerRating(double voltage)
        {
            return voltage switch
            {
                0.4 => 50.0,   // 50 kA
                10.0 => 25.0,  // 25 kA
                34.5 => 16.0,  // 16 kA
                _ => throw new ArgumentException($"Gerilim seviyesi için kesici değeri tanımlı değil: {voltage} kV")
            };
        }

        private void ValidateProtectionCoordination(PowerLine line, double faultCurrent)
        {
            var protectionDevices = GetUpstreamProtectionDevices(line);
            double previousTripTime = 0;

            foreach (var device in protectionDevices)
            {
                double tripTime = CalculateTripTime(device, faultCurrent);

                if (tripTime <= previousTripTime)
                {
                    throw new InvalidOperationException(
                        $"Koruma koordinasyonu hatası! Hat: {line.Name}, Cihaz: {device.Name}");
                }

                previousTripTime = tripTime;
            }
        }

        private List<ProtectionDevice> GetUpstreamProtectionDevices(PowerLine line)
        {
            var devices = new List<ProtectionDevice>();
            var path = FindPathToSource(line);

            foreach (var element in path)
            {
                if (element is PowerLine pl)
                {
                    var device = new ProtectionDevice
                    {
                        Name = $"CB_{pl.Name}",
                        Type = GetProtectionDeviceType(pl.Voltage),
                        Rating = GetDeviceRating(pl.Voltage, pl.Load),
                        TripCurve = GetTripCurve(pl.Voltage)
                    };
                    devices.Add(device);
                }
            }

            return devices;
        }

        private double CalculateTripTime(ProtectionDevice device, double faultCurrent)
        {
            double currentRatio = faultCurrent / device.Rating;
            return device.TripCurve.CalculateTime(currentRatio);
        }

        private ProtectionDeviceType GetProtectionDeviceType(double voltage)
        {
            return voltage switch
            {
                0.4 => ProtectionDeviceType.MCCB,
                10.0 => ProtectionDeviceType.VCB,
                34.5 => ProtectionDeviceType.VCB,
                _ => throw new ArgumentException($"Gerilim seviyesi için koruma cihazı tanımlı değil: {voltage} kV")
            };
        }

        public Dictionary<string, double> GetShortCircuitResults()
        {
            return new Dictionary<string, double>(shortCircuitCurrents);
        }

        private double GetLineResistance(PowerLine line)
        {
            // İletken direnci hesaplama
            double resistivity = GetConductorResistivity(line.ConductorType);
            return (resistivity * line.Length) / GetConductorCrossSection(line.ConductorType);
        }

        private double GetLineReactance(PowerLine line)
        {
            // İletken reaktansı hesaplama (X = 2πfL)
            const double frequency = 50.0; // Hz
            double inductance = GetConductorInductance(line.ConductorType);
            return 2 * Math.PI * frequency * inductance * line.Length;
        }

        private double GetConductorResistivity(string conductorType)
        {
            return conductorType switch
            {
                "SWALLOW" => 0.0283,
                "RAVEN" => 0.0265,
                "PIGEON" => 0.0252,
                _ => throw new ArgumentException($"Bilinmeyen iletken tipi: {conductorType}")
            };
        }

        private double GetConductorCrossSection(string conductorType)
        {
            return conductorType switch
            {
                "SWALLOW" => 16.0,
                "RAVEN" => 35.0,
                "PIGEON" => 50.0,
                _ => throw new ArgumentException($"Bilinmeyen iletken tipi: {conductorType}")
            };
        }

        private double GetConductorInductance(string conductorType)
        {
            return conductorType switch
            {
                "SWALLOW" => 0.00040,
                "RAVEN" => 0.00038,
                "PIGEON" => 0.00035,
                _ => throw new ArgumentException($"Bilinmeyen iletken tipi: {conductorType}")
            };
        }

        private double GetDeviceRating(double voltage, double load)
        {
            double fullLoadCurrent = load / (Math.Sqrt(3) * voltage);
            double rating = fullLoadCurrent * 1.25; // %25 yedek kapasite

            return StandardizeRating(rating);
        }

        private double StandardizeRating(double rating)
        {
            double[] standardRatings = { 16, 25, 32, 40, 63, 80, 100, 125, 160, 200, 250, 315, 400, 630, 800, 1000, 1250, 1600, 2000, 2500, 3150 };

            return standardRatings.First(r => r >= rating);
        }

        private TripCurve GetTripCurve(double voltage)
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
    }
}
