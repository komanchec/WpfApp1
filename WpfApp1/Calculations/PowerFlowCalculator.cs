using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using WpfApp1.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Reflection;


namespace WpfApp1.Calculations
{
    public class PowerFlowCalculator
    {
        private readonly List<NetworkElement> elements;
        private readonly Dictionary<string, Complex> nodePotentials;
        private readonly Dictionary<string, Complex> branchCurrents;
        private readonly double convergenceTolerance = 0.0001;
        private readonly int maxIterations = 100;

        public PowerFlowCalculator(List<NetworkElement> elements)
        {
            this.elements = elements;
            nodePotentials = new Dictionary<string, Complex>();
            branchCurrents = new Dictionary<string, Complex>();
        }

        public void CalculatePowerFlow()
        {
            InitializeNodes();
            bool converged = false;
            int iteration = 0;

            while (!converged && iteration < maxIterations)
            {
                var previousPotentials = new Dictionary<string, Complex>(nodePotentials);

                UpdateNodePotentials();
                CalculateBranchCurrents();

                converged = CheckConvergence(previousPotentials);
                iteration++;
            }

            if (!converged)
            {
                throw new InvalidOperationException("Güç akışı hesaplaması yakınsamadı!");
            }

            CalculatePowerLosses();
        }

        private void InitializeNodes()
        {
            // Başlangıç değerlerini ata
            foreach (var element in elements)
            {
                if (element is PowerLine line)
                {
                    string startNodeId = GetNodeId(line.StartPoint);
                    string endNodeId = GetNodeId(line.EndPoint);

                    if (!nodePotentials.ContainsKey(startNodeId))
                    {
                        nodePotentials[startNodeId] = Complex.FromPolarCoordinates(line.Voltage, 0);
                    }
                    if (!nodePotentials.ContainsKey(endNodeId))
                    {
                        nodePotentials[endNodeId] = Complex.FromPolarCoordinates(line.Voltage, 0);
                    }
                }
            }
        }
        private void UpdateNodePotentials()
        {
            var admittanceMatrix = BuildAdmittanceMatrix();
            var currentVector = BuildCurrentVector();

            foreach (var nodeId in nodePotentials.Keys.ToList())
            {
                if (!IsSlackBus(nodeId))
                {
                    Complex sumAdmittanceVoltage = Complex.Zero;
                    Complex sumCurrents = Complex.Zero;

                    foreach (var otherNodeId in nodePotentials.Keys)
                    {
                        if (nodeId != otherNodeId)
                        {
                            sumAdmittanceVoltage += admittanceMatrix[nodeId][otherNodeId] * nodePotentials[otherNodeId];
                        }
                    }

                    sumCurrents = currentVector[nodeId];

                    nodePotentials[nodeId] = (sumCurrents - sumAdmittanceVoltage) / admittanceMatrix[nodeId][nodeId];
                }
            }
        }

        private Dictionary<string, Dictionary<string, Complex>> BuildAdmittanceMatrix()
        {
            var matrix = new Dictionary<string, Dictionary<string, Complex>>();

            foreach (var nodeId in nodePotentials.Keys)
            {
                matrix[nodeId] = new Dictionary<string, Complex>();
                foreach (var otherNodeId in nodePotentials.Keys)
                {
                    matrix[nodeId][otherNodeId] = Complex.Zero;
                }
            }

            foreach (var element in elements)
            {
                if (element is PowerLine line)
                {
                    string startNodeId = GetNodeId(line.StartPoint);
                    string endNodeId = GetNodeId(line.EndPoint);
                    Complex admittance = GetLineAdmittance(line);

                    // Diagonal elements
                    matrix[startNodeId][startNodeId] += admittance;
                    matrix[endNodeId][endNodeId] += admittance;

                    // Off-diagonal elements
                    matrix[startNodeId][endNodeId] -= admittance;
                    matrix[endNodeId][startNodeId] -= admittance;
                }
            }

            return matrix;
        }

        private Dictionary<string, Complex> BuildCurrentVector()
        {
            var vector = new Dictionary<string, Complex>();

            foreach (var nodeId in nodePotentials.Keys)
            {
                vector[nodeId] = Complex.Zero;
            }

            foreach (var element in elements)
            {
                if (element is PowerLine line)
                {
                    string endNodeId = GetNodeId(line.EndPoint);
                    Complex current = Complex.FromPolarCoordinates(
                        line.Load / line.Voltage,
                        -Math.Acos(0.85) // Güç faktörü 0.85 varsayılan
                    );
                    vector[endNodeId] += current;
                }
            }

            return vector;
        }
        private void CalculateBranchCurrents()
        {
            branchCurrents.Clear();

            foreach (var element in elements)
            {
                if (element is PowerLine line)
                {
                    string startNodeId = GetNodeId(line.StartPoint);
                    string endNodeId = GetNodeId(line.EndPoint);

                    Complex voltageStart = nodePotentials[startNodeId];
                    Complex voltageEnd = nodePotentials[endNodeId];
                    Complex admittance = GetLineAdmittance(line);

                    Complex current = (voltageStart - voltageEnd) * admittance;
                    branchCurrents[line.Id] = current;
                }
            }
        }

        private void CalculatePowerLosses()
        {
            foreach (var element in elements)
            {
                if (element is PowerLine line)
                {
                    Complex current = branchCurrents[line.Id];
                    double resistance = GetLineResistance(line);

                    // P = I²R
                    double powerLoss = Math.Pow(current.Magnitude, 2) * resistance;

                    // Kayıp değerini güncelle
                    UpdateLineLoss(line, powerLoss);
                }
            }
        }

        private Complex GetLineAdmittance(PowerLine line)
        {
            double resistance = GetLineResistance(line);
            double reactance = GetLineReactance(line);
            Complex impedance = new Complex(resistance, reactance);
            return Complex.One / impedance;
        }

        private double GetLineResistance(PowerLine line)
        {
            double resistivity = GetConductorResistivity(line.ConductorType);
            double crossSection = GetConductorCrossSection(line.ConductorType);
            return (resistivity * line.Length) / crossSection;
        }

        private double GetLineReactance(PowerLine line)
        {
            const double frequency = 50.0; // Hz
            double inductance = GetConductorInductance(line.ConductorType);
            return 2 * Math.PI * frequency * inductance * line.Length;
        }

        private bool CheckConvergence(Dictionary<string, Complex> previousPotentials)
        {
            foreach (var nodeId in nodePotentials.Keys)
            {
                if (!IsSlackBus(nodeId))
                {
                    Complex diff = nodePotentials[nodeId] - previousPotentials[nodeId];
                    if (diff.Magnitude > convergenceTolerance)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        #region Helper Methods
        private string GetNodeId(Point point)
        {
            return $"N_{point.X:F2}_{point.Y:F2}";
        }

        private bool IsSlackBus(string nodeId)
        {
            // Slack bus genellikle şebekenin giriş noktasıdır
            var transformers = elements.OfType<Transformer>()
                .Where(t => GetNodeId(new Point(t.Location.X, t.Location.Y)) == nodeId);

            return transformers.Any(t => t.PrimaryVoltage > 10.0);
        }

        private double GetConductorResistivity(string conductorType)
        {
            return conductorType switch
            {
                "SWALLOW" => 0.0283,
                "RAVEN" => 0.0265,
                "PIGEON" => 0.0252,
                "HAWK" => 0.0241,
                "DRAKE" => 0.0236,
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
                "HAWK" => 70.0,
                "DRAKE" => 95.0,
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
                "HAWK" => 0.00033,
                "DRAKE" => 0.00031,
                _ => throw new ArgumentException($"Bilinmeyen iletken tipi: {conductorType}")
            };
        }

        private void UpdateLineLoss(PowerLine line, double powerLoss)
        {
            // Kayıp değerini güncelle ve olayı tetikle
            line.GetType().GetProperty("PowerLoss")?.SetValue(line, powerLoss);
            OnPowerLossCalculated?.Invoke(this, new PowerLossEventArgs(line.Id, powerLoss));
        }

        public event EventHandler<PowerLossEventArgs> OnPowerLossCalculated;
        #endregion
    }

    public class PowerLossEventArgs : EventArgs
    {
        public string ElementId { get; }
        public double PowerLoss { get; }

        public PowerLossEventArgs(string elementId, double powerLoss)
        {
            ElementId = elementId;
            PowerLoss = powerLoss;
        }
    }
}
