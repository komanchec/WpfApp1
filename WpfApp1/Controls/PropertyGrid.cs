using System.Windows.Controls;
using System.Windows;
using System.ComponentModel;
using System.Reflection;
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
    public class PropertyGrid : Grid
    {
        private object selectedObject;
        private StackPanel propertyPanel;

        public PropertyGrid()
        {
            propertyPanel = new StackPanel();
            Children.Add(propertyPanel);
        }

        public void SetObject(object obj)
        {
            selectedObject = obj;
            RefreshProperties();
        }

        private void RefreshProperties()
        {
            propertyPanel.Children.Clear();

            if (selectedObject == null) return;

            var properties = selectedObject.GetType().GetProperties()
                .Where(p => p.GetCustomAttribute<BrowsableAttribute>()?.Browsable ?? true);

            foreach (var property in properties)
            {
                var row = new DockPanel { Margin = new Thickness(2) };

                var label = new TextBlock
                {
                    Text = property.Name,
                    Width = 120,
                    VerticalAlignment = VerticalAlignment.Center
                };
                DockPanel.SetDock(label, Dock.Left);

                var editor = CreateEditorForProperty(property);

                row.Children.Add(label);
                row.Children.Add(editor);

                propertyPanel.Children.Add(row);
            }
        }

        private FrameworkElement CreateEditorForProperty(PropertyInfo property)
        {
            if (property.PropertyType == typeof(bool))
            {
                var checkbox = new CheckBox();
                checkbox.IsChecked = (bool?)property.GetValue(selectedObject);
                checkbox.Checked += (s, e) => UpdateProperty(property, true);
                checkbox.Unchecked += (s, e) => UpdateProperty(property, false);
                return checkbox;
            }
            else if (property.PropertyType.IsEnum)
            {
                var comboBox = new ComboBox();
                comboBox.ItemsSource = Enum.GetValues(property.PropertyType);
                comboBox.SelectedItem = property.GetValue(selectedObject);
                comboBox.SelectionChanged += (s, e) => UpdateProperty(property, comboBox.SelectedItem);
                return comboBox;
            }
            else
            {
                var textBox = new TextBox
                {
                    Text = property.GetValue(selectedObject)?.ToString()
                };
                textBox.TextChanged += (s, e) => UpdateProperty(property, textBox.Text);
                return textBox;
            }
        }

        private void UpdateProperty(PropertyInfo property, object value)
        {
            try
            {
                if (property.PropertyType == typeof(double))
                {
                    property.SetValue(selectedObject, Convert.ToDouble(value));
                }
                else if (property.PropertyType == typeof(int))
                {
                    property.SetValue(selectedObject, Convert.ToInt32(value));
                }
                else
                {
                    property.SetValue(selectedObject, value);
                }
            }
            catch (Exception)
            {
                // Property değeri dönüştürülemedi
            }
        }
    }
}
