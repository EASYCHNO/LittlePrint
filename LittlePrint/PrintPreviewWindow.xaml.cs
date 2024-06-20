using Newtonsoft.Json;
using Spire.Pdf;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LittlePrint
{
    public partial class PrintPreviewWindow : Window
    {
        string CurrentURL = Properties.Resources.CurrentURL;
        private readonly int orderId;
        private string pdfFilePath;

        public delegate void OrderStatusUpdatedEventHandler();
        public event OrderStatusUpdatedEventHandler OrderStatusUpdated;

        public PrintPreviewWindow(string pdfFilePath, int orderId, OrderWithFile selectedOrder)
        {
            InitializeComponent();
            this.orderId = orderId;
            this.pdfFilePath = pdfFilePath;

            txbUserName.Text = $"{selectedOrder.Surname} {selectedOrder.Name} {selectedOrder.Lastname}";
            txbOrderDate.Text = selectedOrder.OrderDate.ToString("dd.MM.yyyy");
            txbFileName.Text = selectedOrder.FileName;
            txbPrice.Text = selectedOrder.OrderPrice.ToString("C2");

            // Проверяем, что путь к PDF файлу не пустой или null
            if (string.IsNullOrEmpty(pdfFilePath))
            {
                MessageBox.Show("Путь к PDF файлу пустой или null.");
                return;
            }

            string fileUri = GetFileUri(pdfFilePath);
            if (fileUri == null)
            {
                MessageBox.Show("Не удалось преобразовать путь к файлу в корректный URI.");
                return;
            }

            Debug.WriteLine($"Navigating to file URI: {fileUri}");
            webBrowser.Navigate(fileUri);

            this.Closing += PrintPreviewWindow_Closing;
        }

        private string GetFileUri(string filePath)
        {
            try
            {
                var fileUri = new Uri(filePath).AbsoluteUri;
                Debug.WriteLine($"Преобразованный URI: {fileUri}");
                return fileUri;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при преобразовании пути к файлу в URI: " + ex.Message);
                return null;
            }
        }


        private void PrintPreviewWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            webBrowser.Navigate("about:blank");
            webBrowser.Dispose();
        }

        private async void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var documentPath = await DownloadDocumentAsync(pdfFilePath);
                if (documentPath != null)
                {
                    PdfDocument pdfDocument = new PdfDocument();
                    pdfDocument.LoadFromFile(documentPath);
                    pdfDocument.Print();

                    await UpdateOrderStatusAsync(orderId, 2);
                    await SaveCompletedOrderAsync(orderId, pdfDocument.Pages.Count);

                    OrderStatusUpdated?.Invoke();

                    Close();
                }
                else
                {
                    MessageBox.Show("Не удалось загрузить документ для печати.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при печати или изменении статуса заказа: " + ex.Message);
            }
        }

        private async Task<string> DownloadDocumentAsync(string url)
        {
            using (var httpClient = new HttpClient())
            {
                try
                {
                    if (Uri.IsWellFormedUriString(url, UriKind.Absolute) && (url.StartsWith("http://") || url.StartsWith("https://")))
                    {
                        var response = await httpClient.GetAsync(url);
                        response.EnsureSuccessStatusCode();

                        var fileBytes = await response.Content.ReadAsByteArrayAsync();
                        var filePath = Path.Combine(Path.GetTempPath(), Path.GetFileName(url));
                        Debug.WriteLine($"Сохранение файла во временную директорию: {filePath}");

                        File.WriteAllBytes(filePath, fileBytes);

                        return filePath;
                    }
                    else
                    {
                        return url;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при загрузке документа: " + ex.Message);
                    return null;
                }
            }
        }


        private async Task UpdateOrderStatusAsync(int orderId, int newStatusId)
        {
            using (var httpClient = new HttpClient())
            {
                var jsonContent = new StringContent($"{{\"StatusID\": {newStatusId}}}", Encoding.UTF8, "application/json");
                var response = await httpClient.PutAsync($"{CurrentURL}/orders/{orderId}", jsonContent);
                response.EnsureSuccessStatusCode();
            }
        }

        private async Task SaveCompletedOrderAsync(int orderId, int paperCount)
        {
            using (var httpClient = new HttpClient())
            {
                var completedOrderData = new
                {
                    OrderID = orderId,
                    PaperCount = paperCount,
                    PrintDate = DateTime.Now
                };

                var jsonContent = JsonConvert.SerializeObject(completedOrderData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync($"{CurrentURL}/completedorders", content);
                response.EnsureSuccessStatusCode();
            }
        }
    }
}
