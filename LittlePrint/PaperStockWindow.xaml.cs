using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LittlePrint
{
    /// <summary>
    /// Логика взаимодействия для PaperStockWindow.xaml
    /// </summary>
    public partial class PaperStockWindow : Window
    {
        private readonly string LocalUrl = "http://localhost:3000";
        //private readonly string NetWork = "https://test-bri6.onrender.com";

        // Событие для уведомления об изменении количества бумаги
        public event EventHandler PaperQuantityUpdated;

        public PaperStockWindow()
        {
            InitializeComponent();
            Loaded += PaperInventoryWindow_Loaded;
        }

        private async void PaperInventoryWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await UpdateCurrentPaperQuantityDisplay();
        }

        // Метод для обновления отображения текущего количества бумаги
        private async Task UpdateCurrentPaperQuantityDisplay()
        {
            int currentQuantity = await GetPaperQuantityFromDatabaseAsync();
            CurrentPaperQuantityTextBlock.Text = currentQuantity.ToString();
        }

        private async void UpdatePaperQuantityButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(AddPaperQuantityTextBox.Text, out int addQuantity))
            {
                int currentQuantity = await GetPaperQuantityFromDatabaseAsync();
                int newQuantity = currentQuantity + addQuantity;
                await UpdatePaperQuantityInDatabaseAsync(newQuantity);
                await UpdateCurrentPaperQuantityDisplay();

                // Вызываем событие для уведомления об изменении количества бумаги
                PaperQuantityUpdated?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                MessageBox.Show("Введите корректное число.");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Метод для получения количества бумаги из базы данных
        private async Task<int> GetPaperQuantityFromDatabaseAsync()
        {
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync($"{LocalUrl}/paperinventory");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                // Проверяем, является ли JSON массивом или объектом
                if (json.StartsWith("["))
                {
                    // JSON массив - десериализуем в список
                    var paperInventoryList = JsonConvert.DeserializeObject<List<PaperInventory>>(json);
                    return paperInventoryList?.FirstOrDefault()?.Quantity ?? 0;
                }
                else
                {
                    // JSON объект - десериализуем в один объект
                    var paperInventory = JsonConvert.DeserializeObject<PaperInventory>(json);
                    return paperInventory?.Quantity ?? 0;
                }
            }
        }

        // Метод для обновления количества бумаги в базе данных
        private async Task UpdatePaperQuantityInDatabaseAsync(int newQuantity)
        {
            using (var httpClient = new HttpClient())
            {
                // Создаем JSON объект с ключом "newQuantity"
                var data = new { newQuantity = newQuantity };
                var content = new StringContent(JsonConvert.SerializeObject(data), System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.PutAsync($"{LocalUrl}/paperinventory", content);
                response.EnsureSuccessStatusCode();
            }
        }
    }

}
