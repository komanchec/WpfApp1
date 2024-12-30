using System.Windows;
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
    public class MoveElementCommand : ICommand
    {
        private readonly DrawingElement element;
        private readonly Point oldPosition;
        private readonly Point newPosition;

        public MoveElementCommand(DrawingElement element, Point oldPosition, Point newPosition)
        {
            this.element = element;
            this.oldPosition = oldPosition;
            this.newPosition = newPosition;
        }

        public void Execute()
        {
            element.Position = newPosition;
        }

        public void Undo()
        {
            element.Position = oldPosition;
        }
    }
}
