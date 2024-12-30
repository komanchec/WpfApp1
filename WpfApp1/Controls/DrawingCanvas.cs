using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfApp1.Models;
using WpfApp1.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Reflection;


namespace WpfApp1.Controls
{
    public class DrawingCanvas : Canvas
    {
        private DrawingManager drawingManager;
        private Point? dragStart;
        private DrawingElement? draggedElement;
        private bool isDragging;

        public DrawingCanvas()
        {
            drawingManager = new DrawingManager();
            Background = Brushes.White;
            ClipToBounds = true;
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            DrawGrid(dc);
            drawingManager.Draw(dc);
        }

        private void DrawGrid(DrawingContext dc)
        {
            double gridSize = 20;
            var gridBrush = new SolidColorBrush(Colors.LightGray) { Opacity = 0.5 };
            var gridPen = new Pen(gridBrush, 0.5);

            for (double x = 0; x < ActualWidth; x += gridSize)
            {
                dc.DrawLine(gridPen, new Point(x, 0), new Point(x, ActualHeight));
            }

            for (double y = 0; y < ActualHeight; y += gridSize)
            {
                dc.DrawLine(gridPen, new Point(0, y), new Point(ActualWidth, y));
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var hitElement = drawingManager.HitTest(e.GetPosition(this));
                if (hitElement != null)
                {
                    draggedElement = hitElement;
                    dragStart = e.GetPosition(this);
                    isDragging = true;
                    drawingManager.SelectedElement = hitElement;
                    CaptureMouse();
                }
                else
                {
                    drawingManager.SelectedElement = null;
                }
                InvalidateVisual();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (isDragging && draggedElement != null && dragStart.HasValue)
            {
                var currentPos = e.GetPosition(this);
                var delta = currentPos - dragStart.Value;

                var newPos = new Point(
                    draggedElement.Position.X + delta.X,
                    draggedElement.Position.Y + delta.Y);

                var moveCommand = new MoveElementCommand(draggedElement, draggedElement.Position, newPos);
                moveCommand.Execute();

                dragStart = currentPos;
                InvalidateVisual();
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            if (isDragging)
            {
                isDragging = false;
                draggedElement = null;
                dragStart = null;
                ReleaseMouseCapture();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Delete && drawingManager.SelectedElement != null)
            {
                drawingManager.RemoveElement(drawingManager.SelectedElement);
                InvalidateVisual();
            }
        }

        public void AddElement(DrawingElement element)
        {
            drawingManager.AddElement(element);
            InvalidateVisual();
        }

        public void Clear()
        {
            drawingManager.Clear();
            InvalidateVisual();
        }

        public void Undo()
        {
            drawingManager.Undo();
            InvalidateVisual();
        }

        public void Redo()
        {
            drawingManager.Redo();
            InvalidateVisual();
        }
    }
}
