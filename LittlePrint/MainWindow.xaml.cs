using Newtonsoft.Json;
using Spire.Pdf;
using Spire.Pdf.Graphics;
using Spire.Pdf.Tables;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;

namespace LittlePrint
{
    public partial class MainWindow : Window
    {
        string CurrentURL = Properties.Resources.CurrentURL;

        private User loggedInUser;
        private DispatcherTimer timer;

        public MainWindow(User user)
        {
            InitializeComponent();
            LoadOrdersWithFilesAsync();
            LoadLocalOrdersAsync();
            loggedInUser = user;
            DisplayUserInfo();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(10);
            timer.Tick += async (s, e) => await LoadOrdersWithFilesAsync();
            timer.Start(); 
        }

        private void DisplayUserInfo()
        {
            lblUserInfo.Content = $"Работник: {loggedInUser.Surname} {loggedInUser.Name} {loggedInUser.Lastname}";
        }

        //Загрузка данных о заказах с сервера
        private async Task LoadOrdersWithFilesAsync()
        {
            using (var httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.GetAsync(CurrentURL + "/orderswithfiles");
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync();
                    var ordersWithFiles = JsonConvert.DeserializeObject<List<OrderWithFile>>(json);

                    fileDataGrid.ItemsSource = ordersWithFiles;
                }
                catch (HttpRequestException ex)
                {
                    MessageBox.Show($"Ошибка HTTP запроса: {ex.Message}\nURL: {CurrentURL}/orderswithfiles");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Произошла ошибка: {ex.Message}");
                }
            }
        }

        private async Task LoadLocalOrdersAsync()
        {
            using (var httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.GetAsync(CurrentURL + "/localorders");
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync();
                    var localOrders = JsonConvert.DeserializeObject<List<LocalOrders>>(json);

                    localFileDataGrid.ItemsSource = localOrders;
                }
                catch (HttpRequestException ex)
                {
                    MessageBox.Show($"Ошибка HTTP запроса: {ex.Message}\nURL: {CurrentURL}/localorders");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Произошла ошибка: {ex.Message}");
                }
            }
        }

        private async void fileDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OrderWithFile selectedOrder = (OrderWithFile)fileDataGrid.SelectedItem;

            if (selectedOrder != null)
            {
                int fileId = selectedOrder.FileID;
                string pdfFilePath = await GetPdfFilePathFromServer(fileId);

                if (!string.IsNullOrEmpty(pdfFilePath))
                {
                    PrintPreviewWindow detailsWindow = new PrintPreviewWindow(pdfFilePath, selectedOrder.OrderID, selectedOrder);


                    // Подписка на событие обновления статуса
                    detailsWindow.OrderStatusUpdated += async () => await LoadOrdersWithFilesAsync();

                    detailsWindow.Show();
                }
            }
        }

        private async Task<string> GetPdfFilePathFromServer(int fileId)
        {
            using (var httpClient = new HttpClient())
            {
                try
                {
                    var requestUrl = $"{CurrentURL}/files/{fileId}";
                    Debug.WriteLine($"Отправка запроса на: {requestUrl}");

                    var response = await httpClient.GetAsync(requestUrl);
                    Debug.WriteLine($"Ответ получен с кодом: {response.StatusCode}");
                    response.EnsureSuccessStatusCode();

                    var fileData = await response.Content.ReadFromJsonAsync<FileDetails>();
                    Debug.WriteLine($"Получены данные о файле: {JsonConvert.SerializeObject(fileData)}");

                    if (fileData == null || string.IsNullOrEmpty(fileData.FilePath))
                    {
                        MessageBox.Show("Ошибка: данные о файле не найдены или путь к файлу пуст.");
                        return null;
                    }

                    // Закодируем URL файла
                    string absoluteFileUrl = $"{CurrentURL}/uploads/{Uri.EscapeDataString(Path.GetFileName(fileData.FilePath))}";
                    Debug.WriteLine($"Проверка доступности файла по URL: {absoluteFileUrl}");

                    var fileCheckResponse = await httpClient.GetAsync(absoluteFileUrl);
                    Debug.WriteLine($"Результат проверки файла: {fileCheckResponse.StatusCode}");
                    if (!fileCheckResponse.IsSuccessStatusCode)
                    {
                        MessageBox.Show($"Произошла ошибка: файл по URL \"{absoluteFileUrl}\" не существует.");
                        return null;
                    }

                    string fileExtension = Path.GetExtension(fileData.FilePath).ToLower();
                    if (fileExtension == ".pdf")
                    {
                        return absoluteFileUrl;
                    }
                    else if (fileExtension == ".doc" || fileExtension == ".docx")
                    {
                        string localDocPath = Path.Combine(Path.GetTempPath(), Path.GetFileName(fileData.FilePath));
                        string pdfFilePath = Path.Combine(Path.GetTempPath(), Path.ChangeExtension(Path.GetFileName(fileData.FilePath), ".pdf"));

                        // Скачивание файла
                        Debug.WriteLine($"Скачивание файла из {absoluteFileUrl} в {localDocPath}");
                        using (var fileResponse = await httpClient.GetAsync(absoluteFileUrl))
                        {
                            fileResponse.EnsureSuccessStatusCode();
                            var fileBytes = await fileResponse.Content.ReadAsByteArrayAsync();
                            File.WriteAllBytes(localDocPath, fileBytes);
                        }

                        // Конвертация файла
                        return ConvertDocToPdf(localDocPath, pdfFilePath);
                    }
                    else
                    {
                        MessageBox.Show($"Неподдерживаемый формат файла: {fileExtension}");
                        return null;
                    }
                }
                catch (HttpRequestException ex)
                {
                    MessageBox.Show($"Ошибка HTTP запроса: {ex.Message}\nURL: {CurrentURL}/files/{fileId}");
                    Debug.WriteLine($"Ошибка HTTP запроса: {ex.Message}\nURL: {CurrentURL}/files/{fileId}");
                    return null;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Произошла ошибка: {ex.Message}");
                    Debug.WriteLine($"Произошла ошибка: {ex.Message}");
                    return null;
                }
            }
        }


        private static string ConvertDocToPdf(string inputFilePath, string outputFilePath)
        {
            try
            {
                Debug.WriteLine($"Начало конвертации файла: {inputFilePath} в {outputFilePath}");
                Spire.Doc.Document document = new Spire.Doc.Document();
                document.LoadFromFile(inputFilePath);
                document.SaveToFile(outputFilePath, Spire.Doc.FileFormat.PDF);
                Debug.WriteLine($"Документ успешно конвертирован: {outputFilePath}");
                return outputFilePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка конвертации файла: {ex.Message}");
                Debug.WriteLine($"Ошибка конвертации файла: {ex.Message}");
                return null;
            }
        }



        private void btnLocalChoose_Click(object sender, RoutedEventArgs e)
        {
            LocalPrintWindow localPrintWindow = new LocalPrintWindow();
            localPrintWindow.ShowDialog();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadOrdersWithFilesAsync();
            LoadLocalOrdersAsync();
            //LoadCompletedOrdersAsync();
        }

        private async void btnGenerateReport_Click(object sender, RoutedEventArgs e)
        {
            ReportWindow reportDialog = new ReportWindow();
            if (reportDialog.ShowDialog() == true)
            {
                // Если диалог закрыт с OK, формируем отчет
                await GenerateReportAsync(reportDialog.StartDate, reportDialog.EndDate);
            }
        }

        private async Task GenerateReportAsync(DateTime startDate, DateTime endDate)
        {
            using (var httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.GetAsync($"{CurrentURL}/completedorders?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync();
                    var completedOrders = JsonConvert.DeserializeObject<List<CompletedOrder>>(json);

                    // Создание PDF-документа
                    PdfDocument pdf = new PdfDocument();
                    PdfPageBase page = pdf.Pages.Add();

                    // Добавление данных в PDF
                    PdfTrueTypeFont font = new PdfTrueTypeFont(new Font("Arial", 12f), true);
                    PdfStringFormat format = new PdfStringFormat(PdfTextAlignment.Left);
                    float y = 50;

                    // Заголовок
                    page.Canvas.DrawString($"Отчет о выполненных заказах за период с {startDate:dd.MM.yyyy} по {endDate:dd.MM.yyyy}", font, PdfBrushes.Black, 50, y, format);
                    y += 20;

                    // Подсчет общего количества заказов, суммы заказов и количества локальных/нелокальных заказов
                    int totalOrders = completedOrders.Count;
                    double totalSum = completedOrders.Sum(o => o.TotalPrice);
                    int localOrders = completedOrders.Count(o => o.IsLocal);
                    int nonLocalOrders = totalOrders - localOrders;

                    // Вывод информации перед таблицей
                    page.Canvas.DrawString($"Всего заказов: {totalOrders}", font, PdfBrushes.Black, 50, y, format);
                    y += 20;
                    page.Canvas.DrawString($"Общая сумма заказов: {totalSum:C2}", font, PdfBrushes.Black, 50, y, format);
                    y += 20;
                    page.Canvas.DrawString($"Количество локальных заказов: {localOrders}", font, PdfBrushes.Black, 50, y, format);
                    y += 20;
                    page.Canvas.DrawString($"Количество онлайн заказов: {nonLocalOrders}", font, PdfBrushes.Black, 50, y, format);
                    y += 40;


                    // Таблица
                    PdfTable table = new PdfTable();
                    table.Style.CellPadding = 2;
                    table.Style.BorderPen = new PdfPen(PdfBrushes.Black, 0.75f);
                    table.Style.DefaultStyle.Font = new PdfTrueTypeFont(new Font("Arial", 10f), true);
                    table.Style.AlternateStyle = new PdfCellStyle();
                    table.Style.AlternateStyle.BackgroundBrush = PdfBrushes.LightGray;
                    table.Style.AlternateStyle.Font = new PdfTrueTypeFont(new Font("Arial", 10f), true);

                    // Заголовки столбцов
                    string[] columnHeaders = { "Дата печати", "Количество листов", "Сумма (руб.)", "Тип заказа" };
                    object[,] data = new object[completedOrders.Count + 1, columnHeaders.Length];
                    for (int i = 0; i < columnHeaders.Length; i++)
                    {
                        data[0, i] = columnHeaders[i];
                    }

                    // Данные
                    for (int i = 0; i < completedOrders.Count; i++)
                    {
                        var order = completedOrders[i];
                        data[i + 1, 0] = order.PrintDate.ToString("dd.MM.yyyy");
                        //data[i + 1, 1] = $"{order.Surname} {order.Name} {order.Lastname}";
                        data[i + 1, 1] = order.PaperCount.ToString();
                        data[i + 1, 2] = order.TotalPrice.ToString("C2"); // Добавляем символ рубля
                        data[i + 1, 3] = order.IsLocal ? "Локальный" : "Онлайн";
                    }

                    table.DataSource = data;

                    // Добавление таблицы на страницу
                    table.Draw(page, new PointF(50, y));

                    // Сохранение PDF
                    string reportPath = "report.pdf";
                    pdf.SaveToFile(reportPath);

                    // Открытие PDF
                    System.Diagnostics.Process.Start(reportPath);
                }
                catch (HttpRequestException ex)
                {
                    MessageBox.Show($"Ошибка HTTP запроса: {ex.Message}\nURL: {CurrentURL}/completedorders?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Произошла ошибка: {ex.Message}");
                }
            }
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            this.Close();
            loginWindow.Show();
        }
    }
}
