using System;
using System.Collections.Generic;
using System.Numerics;
using WpfApp1.Models;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Reflection;


namespace WpfApp1.Calculations
{
    public class HarmonicAnalyzer
    {
        private readonly List<NetworkElement> elements;
        private readonly Dictionary<int, double> harmonicMagnitudes;
        private readonly Dictionary<string, List<Complex>> harmonicCurrents;

        public HarmonicAnalyzer(List<NetworkElement> elements)
        {
            this.elements = elements;
            harmonicMagnitudes = new Dictionary<int, double>();
            harmonicCurrents = new Dictionary<string, List<Complex>>();
            InitializeHarmonicMagnitudes();
        }

        private void InitializeHarmonicMagnitudes()
        {
            // Tipik harmonik bileşenleri
            harmonicMagnitudes.Add(3, 0.15);  // 3. harmonik %15
            harmonicMagnitudes.Add(5, 0.10);  // 5. harmonik %10
            harmonicMagnitudes.Add(7, 0.07);  // 7. harmonik %7
            harmonicMagnitudes.Add(9, 0.05);  // 9. harmonik %5
            harmonicMagnitudes.Add(11, 0.03); // 11. harmonik %3
        }

        public void AnalyzeHarmonics()
        {
            foreach (var element in elements)
            {
                switch (element)
                {
                    case PowerLine line:
                        CalculateLineHarmonics(line);
                        break;
                    case Transformer transformer:
                        CalculateTransformerHarmonics(transformer);
                        break;
                }
            }

            CalculateTotalHarmonicDistortion();
        }

        private void CalculateLineHarmonics(PowerLine line)
        {
            var harmonicCurrentList = new List<Complex>();
            double fundamentalCurrent = line.Load / line.Voltage;

            // Temel bileşen
            harmonicCurrentList.Add(new Complex(fundamentalCurrent, 0));

            // Harmonik bileşenler
            foreach (var harmonic in harmonicMagnitudes)
            {
                int order = harmonic.Key;
                double magnitude = harmonic.Value * fundamentalCurrent;
                double angle = CalculateHarmonicAngle(order);

                harmonicCurrentList.Add(Complex.FromPolarCoordinates(magnitude, angle));
            }

            harmonicCurrents[line.Id] = harmonicCurrentList;
        }
        private void CalculateTransformerHarmonics(Transformer transformer)
        {
            var harmonicCurrentList = new List<Complex>();
            double fundamentalCurrent = transformer.Load / transformer.PrimaryVoltage;

            // Temel bileşen
            harmonicCurrentList.Add(new Complex(fundamentalCurrent, 0));

            // Transformatör harmonik empedansları
            foreach (var harmonic in harmonicMagnitudes)
            {
                int order = harmonic.Key;
                double impedance = CalculateTransformerHarmonicImpedance(transformer, order);
                double magnitude = harmonic.Value * fundamentalCurrent / Math.Sqrt(1 + Math.Pow(order, 2));
                double angle = CalculateHarmonicAngle(order);

                harmonicCurrentList.Add(Complex.FromPolarCoordinates(magnitude, angle));
            }

            harmonicCurrents[transformer.Id] = harmonicCurrentList;
        }

        private double CalculateHarmonicAngle(int order)
        {
            // Her harmonik için faz açısı hesaplama
            return (order - 1) * Math.PI / 6;
        }

        private double CalculateTransformerHarmonicImpedance(Transformer transformer, int harmonicOrder)
        {
            // Transformatör empedansı harmonik derecesiyle orantılı olarak artar
            double baseImpedance = (Math.Pow(transformer.PrimaryVoltage, 2) / transformer.Power) * 0.05; // %5 empedans
            return baseImpedance * harmonicOrder;
        }

        private void CalculateTotalHarmonicDistortion()
        {
            foreach (var elementCurrents in harmonicCurrents)
            {
                double fundamentalMagnitude = elementCurrents.Value[0].Magnitude;
                double sumSquaredHarmonics = 0;

                // İlk eleman temel bileşen olduğu için 1'den başlıyoruz
                for (int i = 1; i < elementCurrents.Value.Count; i++)
                {
                    sumSquaredHarmonics += Math.Pow(elementCurrents.Value[i].Magnitude, 2);
                }

                double thd = Math.Sqrt(sumSquaredHarmonics) / fundamentalMagnitude * 100;
                ValidateHarmonicDistortion(elementCurrents.Key, thd);
            }
        }

        private void ValidateHarmonicDistortion(string elementId, double thd)
        {
            const double maxAllowedTHD = 8.0; // %8 maksimum THD

            if (thd > maxAllowedTHD)
            {
                var element = elements.Find(e => e.Id == elementId);
                throw new InvalidOperationException(
                    $"Toplam harmonik distorsiyon limiti aşıldı! Element: {element.Name}, THD: {thd:F2}%");
            }
        }
    }
}

