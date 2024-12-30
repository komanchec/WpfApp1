using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using WpfApp1.Commands;

namespace WpfApp1.Models
{
    public class DrawingManager
    {
        private readonly ObservableCollection<DrawingElement> elements;
        private DrawingElement? selectedElement;
        private readonly Stack<ICommand> undoStack;
        private readonly Stack<ICommand> redoStack;

        public event EventHandler? SelectionChanged;
        public event EventHandler? ElementsChanged;

        public DrawingManager()
        {
            elements = new ObservableCollection<DrawingElement>();
            undoStack = new Stack<ICommand>();
            redoStack = new Stack<ICommand>();
        }

        public IEnumerable<DrawingElement> Elements => elements;

        public DrawingElement? SelectedElement
        {
            get => selectedElement;
            set
            {
                if (selectedElement != value)
                {
                    if (selectedElement != null)
                        selectedElement.IsSelected = false;

                    selectedElement = value;

                    if (selectedElement != null)
                        selectedElement.IsSelected = true;

                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void AddElement(DrawingElement element)
        {
            var command = new AddElementCommand(this, element);
            command.Execute();
            undoStack.Push(command);
            redoStack.Clear();
            ElementsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RemoveElement(DrawingElement element)
        {
            var command = new RemoveElementCommand(this, element);
            command.Execute();
            undoStack.Push(command);
            redoStack.Clear();

            if (selectedElement == element)
                SelectedElement = null;

            ElementsChanged?.Invoke(this, EventArgs.Empty);
        }

        public DrawingElement? HitTest(Point point)
        {
            return elements.LastOrDefault(e => e.IsVisible && e.Contains(point));
        }

        public void Draw(DrawingContext context)
        {
            foreach (var element in elements.Where(e => e.IsVisible))
            {
                element.Draw(context);
            }
        }

        public void Undo()
        {
            if (undoStack.Count > 0)
            {
                var command = undoStack.Pop();
                command.Undo();
                redoStack.Push(command);
                ElementsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Redo()
        {
            if (redoStack.Count > 0)
            {
                var command = redoStack.Pop();
                command.Execute();
                undoStack.Push(command);
                ElementsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Clear()
        {
            elements.Clear();
            selectedElement = null;
            undoStack.Clear();
            redoStack.Clear();
            ElementsChanged?.Invoke(this, EventArgs.Empty);
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
