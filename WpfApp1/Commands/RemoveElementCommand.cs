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
    public class RemoveElementCommand : ICommand
    {
        private readonly DrawingManager drawingManager;
        private readonly DrawingElement element;

        public RemoveElementCommand(DrawingManager drawingManager, DrawingElement element)
        {
            this.drawingManager = drawingManager;
            this.element = element;
        }

        public void Execute()
        {
            drawingManager.RemoveElement(element);
        }

        public void Undo()
        {
            drawingManager.AddElement(element);
        }
    }
}
