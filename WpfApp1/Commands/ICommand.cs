using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Reflection;
using WpfApp1.Models;

namespace WpfApp1.Commands
{
    public interface ICommand : System.Windows.Input.ICommand
    {
        void Execute();
        void Undo();
    }

    public class AddElementCommand : ICommand
    {
        private readonly DrawingManager _drawingManager;
        private readonly DrawingElement _element;

        public event EventHandler? CanExecuteChanged;

        public AddElementCommand(DrawingManager drawingManager, DrawingElement element)
        {
            _drawingManager = drawingManager ?? throw new ArgumentNullException(nameof(drawingManager));
            _element = element ?? throw new ArgumentNullException(nameof(element));
        }

        public bool CanExecute(object? parameter) => true;

        void ICommand.Execute()
        {
            _drawingManager.Elements.Add(_element);
        }

        void System.Windows.Input.ICommand.Execute(object? parameter)
        {
            ((ICommand)this).Execute();
        }

        public void Undo()
        {
            _drawingManager.Elements.Remove(_element);
        }
    }

    public class RemoveElementCommand : ICommand
    {
        private readonly DrawingManager _drawingManager;
        private readonly DrawingElement _element;

        public event EventHandler? CanExecuteChanged;

        public RemoveElementCommand(DrawingManager drawingManager, DrawingElement element)
        {
            _drawingManager = drawingManager ?? throw new ArgumentNullException(nameof(drawingManager));
            _element = element ?? throw new ArgumentNullException(nameof(element));
        }

        public bool CanExecute(object? parameter) => true;

        void ICommand.Execute()
        {
            _drawingManager.Elements.Remove(_element);
        }

        void System.Windows.Input.ICommand.Execute(object? parameter)
        {
            ((ICommand)this).Execute();
        }

        public void Undo()
        {
            _drawingManager.Elements.Add(_element);
        }
    }
}
