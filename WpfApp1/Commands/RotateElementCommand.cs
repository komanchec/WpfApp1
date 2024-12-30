using WpfApp1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Reflection;

namespace WpfApp1.Commands
{
    public class RotateElementCommand : ICommand
    {
        private readonly DrawingElement element;
        private readonly double oldRotation;
        private readonly double newRotation;

        public RotateElementCommand(DrawingElement element, double oldRotation, double newRotation)
        {
            this.element = element;
            this.oldRotation = oldRotation;
            this.newRotation = newRotation;
        }

        public void Execute()
        {
            element.Rotation = newRotation;
        }

        public void Undo()
        {
            element.Rotation = oldRotation;
        }
    }
}
