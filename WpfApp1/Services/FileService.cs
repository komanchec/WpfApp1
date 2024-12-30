using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;
using WpfApp1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Reflection;


namespace WpfApp1.Services
{
    public class FileService
    {
        private readonly DrawingManager drawingManager;
        private string currentFilePath;

        public FileService(DrawingManager drawingManager)
        {
            this.drawingManager = drawingManager;
        }

        public void New()
        {
            if (MessageBox.Show("Yeni dosya oluşturmak istediğinizden emin misiniz?",
                "Yeni Dosya", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                drawingManager.Clear();
                currentFilePath = null;
            }
        }

        public void Open()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Elektrik Şebekesi Dosyası (*.net)|*.net|Tüm Dosyalar (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var json = File.ReadAllText(dialog.FileName);
                    var elements = JsonSerializer.Deserialize<List<DrawingElement>>(json);

                    drawingManager.Clear();
                    foreach (var element in elements)
                    {
                        drawingManager.AddElement(element);
                    }

                    currentFilePath = dialog.FileName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Dosya açılırken hata oluştu: {ex.Message}", "Hata",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void Save()
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                SaveAs();
            }
            else
            {
                SaveToFile(currentFilePath);
            }
        }

        public void SaveAs()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Elektrik Şebekesi Dosyası (*.net)|*.net|Tüm Dosyalar (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                SaveToFile(dialog.FileName);
                currentFilePath = dialog.FileName;
            }
        }

        private void SaveToFile(string filePath)
        {
            try
            {
                var json = JsonSerializer.Serialize(drawingManager.Elements.ToList());
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Dosya kaydedilirken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
