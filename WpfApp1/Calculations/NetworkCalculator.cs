using System;
using System.Collections.Generic;
using System.Linq;
using WpfApp1.Models;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Reflection;


namespace WpfApp1.Calculations
{
    public class NetworkCalculator
    {
        private List<NetworkElement> elements;
        private Dictionary<string, double> voltageDrops;
        private Dictionary<string, double> loads;

        public NetworkCalculator(List<NetworkElement> elements)
        {
            this.elements = elements;
            voltageDrops = new Dictionary<string, double>();
            loads = new Dictionary<string, double>();
        }

        public void CalculateNetwork()
        {
            CalculateLoads();
            CalculateVoltageDrops();
            CalculateShortCircuit();
            OptimizeNetwork();
        }

        private void CalculateLoads()
        {
            foreach (var element in elements)
            {
                switch (element)
                {
                    case PowerLine line:
                        CalculateLineLoad(line);
                        break;
                    case Transformer transformer:
                        CalculateTransformerLoad(transformer);
                        break;
                }
            }
        }

        private void CalculateLineLoad(PowerLine line)
        {
            // Hat yükü hesaplama
            double resistance = GetLineResistance(line);
            double current = line.Load / line.Voltage;
            double powerLoss = Math.Pow(current, 2) * resistance;

            loads[line.Id] = powerLoss;
        }

        // ...

        private void CalculateTransformerLoad(Transformer transformer)
        {
            // Trafo yükü hesaplama
            double loadFactor = transformer.Load / transformer.Power;
            double efficiency = transformer.Efficiency; // Use the property directly instead of calling a method

            double powerLoss = transformer.Power * (1 - efficiency) * loadFactor;

            loads[transformer.Id] = powerLoss;
        }

        private double GetLineResistance(PowerLine line)
        {
            // İletken direnci hesaplama
            double resistivity = GetConductorResistivity(line.ConductorType);
            return (resistivity * line.Length) / GetConductorCrossSection(line.ConductorType);
        }
        private void CalculateVoltageDrops()
        {
            var sortedLines = GetSortedPowerLines();

            foreach (var line in sortedLines)
            {
                double current = line.Load / line.Voltage;
                double resistance = GetLineResistance(line);
                double reactance = GetLineReactance(line);

                double voltageDrop = Math.Sqrt(3) * current *
                    Math.Sqrt(Math.Pow(resistance, 2) + Math.Pow(reactance, 2));

                voltageDrops[line.Id] = voltageDrop;
            }
        }

        private List<PowerLine> GetSortedPowerLines()
        {
            var lines = elements.OfType<PowerLine>().ToList();
            var graph = BuildNetworkGraph(lines);
            return TopologicalSort(graph);
        }

        private Dictionary<string, List<string>> BuildNetworkGraph(List<PowerLine> lines)
        {
            var graph = new Dictionary<string, List<string>>();

            foreach (var line in lines)
            {
                if (!graph.ContainsKey(line.Id))
                {
                    graph[line.Id] = new List<string>();
                }

                // Bağlantılı hatları bul
                var connectedLines = lines.Where(l =>
                    l.StartPoint == line.EndPoint ||
                    l.EndPoint == line.StartPoint).ToList();

                foreach (var connectedLine in connectedLines)
                {
                    graph[line.Id].Add(connectedLine.Id);
                }
            }

            return graph;
        }

        private List<PowerLine> TopologicalSort(Dictionary<string, List<string>> graph)
        {
            var visited = new HashSet<string>();
            var sorted = new List<PowerLine>();

            foreach (var nodeId in graph.Keys)
            {
                if (!visited.Contains(nodeId))
                {
                    TopologicalSortUtil(nodeId, visited, sorted, graph);
                }
            }

            return sorted;
        }

        private void TopologicalSortUtil(string nodeId, HashSet<string> visited,
            List<PowerLine> sorted, Dictionary<string, List<string>> graph)
        {
            visited.Add(nodeId);

            foreach (var neighborId in graph[nodeId])
            {
                if (!visited.Contains(neighborId))
                {
                    TopologicalSortUtil(neighborId, visited, sorted, graph);
                }
            }

            var line = elements.OfType<PowerLine>()
                .FirstOrDefault(l => l.Id == nodeId);
            if (line != null)
            {
                sorted.Add(line);
            }
        }
        private void CalculateShortCircuit()
        {
            foreach (var line in elements.OfType<PowerLine>())
            {
                double impedance = CalculateLineImpedance(line);
                double shortCircuitCurrent = line.Voltage / (Math.Sqrt(3) * impedance);
                double shortCircuitPower = Math.Sqrt(3) * line.Voltage * shortCircuitCurrent;

                // Kısa devre dayanım kontrolü
                ValidateShortCircuitRating(line, shortCircuitCurrent);
            }
        }

        private double CalculateLineImpedance(PowerLine line)
        {
            double resistance = GetLineResistance(line);
            double reactance = GetLineReactance(line);
            return Math.Sqrt(Math.Pow(resistance, 2) + Math.Pow(reactance, 2));
        }

        private void ValidateShortCircuitRating(PowerLine line, double shortCircuitCurrent)
        {
            double ratedCurrent = GetRatedCurrent(line.ConductorType);
            if (shortCircuitCurrent > ratedCurrent * 1.5)
            {
                throw new InvalidOperationException(
                    $"Hat {line.Name} için kısa devre akımı izin verilen değerin üzerinde!");
            }
        }

        private void OptimizeNetwork()
        {
            bool optimizationNeeded = true;
            while (optimizationNeeded)
            {
                optimizationNeeded = false;

                foreach (var line in elements.OfType<PowerLine>())
                {
                    if (voltageDrops[line.Id] > GetMaxVoltageDrop(line.Voltage))
                    {
                        OptimizeLine(line);
                        optimizationNeeded = true;
                    }
                }
            }
        }

        private void OptimizeLine(PowerLine line)
        {
            var availableConductors = GetAvailableConductors(line.Voltage);
            foreach (var conductor in availableConductors)
            {
                if (IsValidConductor(conductor, line))
                {
                    line.ConductorType = conductor;
                    return;
                }
            }
        }

        private bool IsValidConductor(string conductorType, PowerLine line)
        {
            double resistance = GetLineResistance(line);
            double current = line.Load / line.Voltage;
            double voltageDrop = Math.Sqrt(3) * current * resistance;

            return voltageDrop <= GetMaxVoltageDrop(line.Voltage);
        }
        #region Helper Methods

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

        private double GetLineReactance(PowerLine line)
        {
            // İletken reaktansı hesaplama (X = 2πfL)
            const double frequency = 50.0; // Hz
            double inductance = GetConductorInductance(line.ConductorType);
            return 2 * Math.PI * frequency * inductance * line.Length;
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

        private double GetRatedCurrent(string conductorType)
        {
            return conductorType switch
            {
                "SWALLOW" => 101,
                "RAVEN" => 154,
                "PIGEON" => 198,
                _ => throw new ArgumentException($"Bilinmeyen iletken tipi: {conductorType}")
            };
        }

        private double GetMaxVoltageDrop(double voltage)
        {
            return voltage switch
            {
                0.4 => 0.05,  // %5
                10.0 => 0.07, // %7
                34.5 => 0.07, // %7
                _ => throw new ArgumentException($"Bilinmeyen gerilim seviyesi: {voltage}")
            };
        }

        private List<string> GetAvailableConductors(double voltage)
        {
            return voltage switch
            {
                0.4 => new List<string> { "SWALLOW", "RAVEN" },
                10.0 => new List<string> { "RAVEN", "PIGEON" },
                34.5 => new List<string> { "PIGEON" },
                _ => throw new ArgumentException($"Bilinmeyen gerilim seviyesi: {voltage}")
            };
        }

        #endregion
    }
}

