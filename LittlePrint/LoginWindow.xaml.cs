using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;

namespace LittlePrint
{
    public partial class LoginWindow : Window
    {
        private readonly string LocalUrl = "http://localhost:3000";

        string CurrentURL = Properties.Resources.CurrentURL;

        public LoginWindow()
        {
            InitializeComponent();
        }

        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text;
            string password = txtPassword.Password;

            var user = new
            {
                Login = login,
                Password = password
            };

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Необходимо заполнить все поля для входа.");
                return;
            }

            try
            {
                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.PostAsJsonAsync($"{CurrentURL}/users/login", user);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var loggedInUser = JsonConvert.DeserializeObject<User>(responseContent); // Предполагаем, что сервер возвращает информацию о пользователе

                        MessageBox.Show("Вход успешен.");
                        // Переход на основное окно приложения
                        MainWindow mainWindow = new MainWindow(loggedInUser);
                        mainWindow.Show();
                        this.Close(); // Закрываем текущее окно авторизации
                    }
                    else
                    {
                        MessageBox.Show($"Ошибка входа: {responseContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при попытке входа: {ex.Message}");
            }
        }


        private void lnkRegister_Click(object sender, RoutedEventArgs e)
        {
            RegistrationWindow registrationWindow = new RegistrationWindow();
            registrationWindow.Show();
            this.Close();
        }
    }
}
