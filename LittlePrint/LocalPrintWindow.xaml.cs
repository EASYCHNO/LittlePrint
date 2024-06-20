using System.Windows;
using Microsoft.Win32;
using System.Net.Http;
using System.Net.Http.Json;
using System.IO;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using Spire.Pdf;
using System.Windows.Controls;
using System.Drawing.Printing;
using System.Drawing;
using Spire.Pdf.Graphics;


namespace LittlePrint
{
    public partial class LocalPrintWindow : Window
    {
        //private readonly string LocalUrl = "http://localhost:3000";

        string CurrentURL = Properties.Resources.CurrentURL;

        private string selectedFilePath;

        public LocalPrintWindow()
        {
            InitializeComponent();
            lblPrice = FindName("lblPrice") as Label;
        }

        private void btnChooseFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "All files (*.*)|*.*|PDF files (*.pdf)|*.pdf|Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg";
            if (openFileDialog.ShowDialog() == true)
            {
                selectedFilePath = openFileDialog.FileName;
                txtFileName.Text = selectedFilePath;
            }
        }
        private void btnCalculatePrice_Click(object sender, RoutedEventArgs e)
        {
            if (selectedFilePath != null)
            {
                // Вычисляем стоимость заказа
                int pageCount = GetLocalOrderPageCount(selectedFilePath);
                double pricePerSheet = chkOwnPaper.IsChecked == true ? 3.5 : 5; 
                double totalPrice = pageCount * pricePerSheet;

                // Обновляем значение цены в UI
                lblPrice.Content = $"Стоимость: {totalPrice} рублей";
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите файл для расчета стоимости.");
            }
        }

        private async void btnPrintLocal_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
            {
                return;
            }

            try
            {
                // Вычисляем стоимость заказа
                int pageCount = GetLocalOrderPageCount(selectedFilePath);
                double pricePerSheet = chkOwnPaper.IsChecked == true ? 3.5 : 5; 
                double totalPrice = pageCount * pricePerSheet;

                // Обновляем значение цены в UI
                lblPrice.Content = $"Стоимость: {totalPrice} рублей";

                // Печать выбранного файла
                PrintDocument(selectedFilePath);

                // Отправка данных локального заказа на сервер
                await SendLocalOrderDataAsync();

                // Получение идентификатора созданного локального заказа
                int localOrderId = await GetLatestLocalOrderId();
                bool isLocal = true;

                // Отправка данных завершенного заказа на сервер
                await SendCompletedOrderDataAsync(0, localOrderId, DateTime.Now, pageCount, totalPrice, isLocal);

                MessageBox.Show("Локальный заказ успешно создан.");
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при обработке локального заказа: " + ex.Message);
            }
        }

        private async Task<int> GetLatestLocalOrderId()
        {
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetStringAsync($"{CurrentURL}/localorders/latest");
                return JsonConvert.DeserializeObject<int>(response);
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrEmpty(selectedFilePath))
            {
                MessageBox.Show("Необходимо выбрать файл.");
                return false;
            }

            return true;
        }
        private async Task SendCompletedOrderDataAsync(int orderId, int localOrderId, DateTime printDate, int paperCount, double totalPrice, bool isLocal)
        {
            using (var httpClient = new HttpClient())
            {
                var completedOrderData = new
                {
                    OrderID = orderId,
                    LocalOrderID = localOrderId,
                    PrintDate = printDate.ToString("yyyy-MM-dd"),
                    PaperCount = paperCount,
                    TotalPrice = totalPrice,
                    IsLocal = isLocal // Добавляем логическое значение для типа заказа
                };

                var response = await httpClient.PostAsJsonAsync($"{CurrentURL}/completedorders", completedOrderData);
                response.EnsureSuccessStatusCode();
            }
        }

        private void PrintDocument(string filePath)
        {
            try
            {
                PrintDocument printDoc = new PrintDocument();

                string extension = Path.GetExtension(filePath).ToLower();

                if (extension == ".pdf")
                {
                    int currentPage = 0;
                    PdfDocument pdfDocument = new PdfDocument();
                    pdfDocument.LoadFromFile(filePath);

                    printDoc.PrintPage += (s, e) =>
                    {
                        if (currentPage >= pdfDocument.Pages.Count)
                        {
                            e.HasMorePages = false;
                            return;
                        }

                        // Сохраняем текущую страницу PDF в изображение
                        System.Drawing.Image image = pdfDocument.SaveAsImage(currentPage);

                        // Печатаем изображение
                        e.Graphics.DrawImage(image, new Rectangle(0, 0, e.PageBounds.Width, e.PageBounds.Height));

                        // Увеличиваем номер текущей страницы
                        currentPage++;

                        // Проверяем, есть ли еще страницы для печати
                        e.HasMorePages = currentPage < pdfDocument.Pages.Count;
                    };
                }
                else if (extension == ".png" || extension == ".jpg" || extension == ".jpeg")
                {
                    using (System.Drawing.Image image = System.Drawing.Image.FromFile(filePath))
                    {
                        if (image.Width > image.Height)
                        {
                            // Если изображение горизонтальное, устанавливаем альбомную ориентацию
                            printDoc.DefaultPageSettings.Landscape = true;
                        }
                        else
                        {
                            // Если изображение вертикальное, устанавливаем книжную ориентацию
                            printDoc.DefaultPageSettings.Landscape = false;
                        }
                    }

                    printDoc.PrintPage += (s, e) =>
                    {
                        using (System.Drawing.Image image = System.Drawing.Image.FromFile(filePath))
                        {
                            // Получаем размеры изображения
                            var imageSize = image.Size;

                            // Получаем размеры области печати
                            var printArea = e.PageBounds;

                            // Коэффициенты масштабирования для вписывания изображения в область печати
                            float scale = Math.Min((float)printArea.Width / imageSize.Width, (float)printArea.Height / imageSize.Height);

                            // Новые размеры изображения
                            int newWidth = (int)(imageSize.Width * scale);
                            int newHeight = (int)(imageSize.Height * scale);

                            // Определяем верхний левый угол для центрирования изображения
                            int x = (printArea.Width - newWidth) / 2;
                            int y = (printArea.Height - newHeight) / 2;

                            // Печатаем изображение
                            e.Graphics.DrawImage(image, x, y, newWidth, newHeight);
                        }
                        e.HasMorePages = false; // Только одна страница для изображения
                    };
                }
                else if (extension == ".docx")
                {
                    int currentPage = 0;

                    printDoc.PrintPage += (s, e) =>
                    {
                        using (Spire.Doc.Document doc = new Spire.Doc.Document())
                        {
                            doc.LoadFromFile(filePath);

                            if (currentPage >= doc.PageCount)
                            {
                                e.HasMorePages = false;
                                return;
                            }

                            // Рендерим текущую страницу
                            doc.RenderToSize(currentPage, e.Graphics, 0, 0, e.PageBounds.Width, e.PageBounds.Height);

                            // Увеличиваем номер текущей страницы
                            currentPage++;

                            // Проверяем, есть ли еще страницы для печати
                            e.HasMorePages = currentPage < doc.PageCount;
                        }
                    };
                }
                else
                {
                    throw new NotSupportedException("Неподдерживаемый формат файла.");
                }

                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDoc.Print();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при печати документа: " + ex.Message);
            }
        }

        private async Task SendLocalOrderDataAsync()
        {
            using (var httpClient = new HttpClient())
            {
                // Вычисляем стоимость заказа
                int pageCount = GetLocalOrderPageCount(selectedFilePath);
                double pricePerSheet = chkOwnPaper.IsChecked == true ? 3.5 : 5; // 3.5 рубля, если своя бумага
                double totalPrice = pageCount * pricePerSheet;

                // Объект с данными заказа, включая стоимость
                var localOrderData = new
                {
                    LocalFileName = Path.GetFileName(selectedFilePath),
                    LocalOrderPrice = totalPrice, // Добавляем стоимость
                    OwnPaper = chkOwnPaper.IsChecked // Добавляем информацию о собственной бумаге
                };

                var response = await httpClient.PostAsJsonAsync($"{CurrentURL}/localorders", localOrderData);
                response.EnsureSuccessStatusCode();
            }
        }
        private int GetLocalOrderPageCount(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();

            if (extension == ".pdf")
            {
                using (var pdfDocument = new PdfDocument())
                {
                    pdfDocument.LoadFromFile(filePath);
                    return pdfDocument.Pages.Count;
                }
            }
            else if (extension == ".png" || extension == ".jpg" || extension == ".jpeg")
            {
                return 1; // Для изображений всегда одна страница
            }
            else
            {
                throw new NotSupportedException("Неподдерживаемый формат файла.");
            }
        }

    }
}