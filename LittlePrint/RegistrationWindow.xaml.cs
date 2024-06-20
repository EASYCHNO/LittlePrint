using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;

namespace LittlePrint
{
    public partial class RegistrationWindow : Window
    {
        private readonly string LocalUrl = "http://localhost:3000";

        string CurrentURL = Properties.Resources.CurrentURL;

        public RegistrationWindow()
        {
            InitializeComponent();
        }

        private async void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            string surname = txtSurname.Text;
            string name = txtName.Text;
            string lastname = txtLastname.Text;
            string email = txtEmail.Text;
            string login = txtLogin.Text;
            string password = txtPassword.Password;
            string confirmPassword = txtConfirmPassword.Password;

            if (password != confirmPassword)
            {
                MessageBox.Show("Пароли не совпадают.");
                return;
            }

            var user = new
            {
                Surname = surname,
                Name = name,
                Lastname = lastname,
                Email = email,
                Login = login,
                Password = password, // Отправляем необработанный пароль, сервер его захэширует
                RoleID = 2 // Роль "Работник"
            };

            try
            {
                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.PostAsJsonAsync($"{CurrentURL}/users/register", user);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Регистрация успешна.");
                        LoginWindow loginWindow = new LoginWindow();
                        loginWindow.Show();
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show($"Ошибка регистрации: {responseContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при попытке регистрации: {ex.Message}");
            }
        }

        private void lnkLogin_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }
    }
}
