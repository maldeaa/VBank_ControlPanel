using BCrypt.Net;
using MahApps.Metro.Controls;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VBank_ControlPanel.Classes;
using VBank_ControlPanel.Models;
using VBank_ControlPanel.Windows;

namespace VBank_ControlPanel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private VBankContent context;
        private bool isUserLogin;

        public MainWindow(bool isUserLogin = false)
        {
            InitializeComponent();
            this.isUserLogin = isUserLogin;
            SetupLoginFields();

            Task.Run(async () =>
            {
                context = new VBankContent();
                await context.Database.CanConnectAsync();
            });
        }

        private bool isPasswordVisible = false;
        private bool isPassportVisible = false;

        private void TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
        {
            isPasswordVisible = !isPasswordVisible;

            if (isPasswordVisible)
            {
                PasswordRevealBox.Text = PasswordBox.Password;
                PasswordBox.Visibility = Visibility.Collapsed;
                PasswordRevealBox.Visibility = Visibility.Visible;
                TogglePasswordVisibility.Content = "🙈";
            }
            else
            {
                PasswordBox.Password = PasswordRevealBox.Text;
                PasswordBox.Visibility = Visibility.Visible;
                PasswordRevealBox.Visibility = Visibility.Collapsed;
                TogglePasswordVisibility.Content = "👁️";
            }
        }

        private void TogglePassportVisibility_Click(object sender, RoutedEventArgs e)
        {
            isPassportVisible = !isPassportVisible;

            if (isPassportVisible)
            {
                PassportRevealBox.Text = PassportBox.Password;
                PassportBox.Visibility = Visibility.Collapsed;
                PassportRevealBox.Visibility = Visibility.Visible;
                TogglePassportVisibility.Content = "🙈";
            }
            else
            {
                PassportBox.Password = PassportRevealBox.Text;
                PassportBox.Visibility = Visibility.Visible;
                PassportRevealBox.Visibility = Visibility.Collapsed;
                TogglePassportVisibility.Content = "👁️";
            }
        }

        private void SetupLoginFields()
        {
            if (isUserLogin)
            {
                EmployeeLoginPanel.Visibility = Visibility.Collapsed;
                CustomerLoginPanel.Visibility = Visibility.Visible;
                ToggleLoginButton.Content = "Я сотрудник";
            }
            else
            {
                EmployeeLoginPanel.Visibility = Visibility.Visible;
                CustomerLoginPanel.Visibility = Visibility.Collapsed;
                ToggleLoginButton.Content = "Я клиент";
            }
        }

        private void ToggleLoginButton_Click(object sender, RoutedEventArgs e)
        {
            isUserLogin = !isUserLogin;
            SetupLoginFields();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (isPassportVisible)
                PassportBox.Password = PassportRevealBox.Text;
            if (isPasswordVisible)
                PasswordBox.Password = PasswordRevealBox.Text;

            if (isUserLogin)
            {
                string fullName = FullNameTextBox.Text;
                string passport = PassportBox.Password;

                if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(passport))
                {
                    ShowError("Введите ФИО и паспортные данные");
                    return;
                }

                try
                {
                    var customer = context.Customers
                        .AsNoTracking()
                        .FirstOrDefault(c => c.FullName == fullName.Trim() && c.PassportData.Replace(" ", "") == passport.Replace(" ", ""));

                    if (customer == null)
                    {
                        ShowError("Клиент не найден");
                        return;
                    }

                    MessageBoxHelper.Show($"Добро пожаловать, {customer.FullName}!", "Успех", "ОК");
                    new CustomerDashboardWindow(customer).Show();
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBoxHelper.Show($"Ошибка при авторизации: {ex.Message}", "Ошибка", "ОК", canCopy: true);
                }
            }
            else
            {
                string login = LoginTextBox.Text;
                string password = PasswordBox.Password;

                if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
                {
                    ShowError("Введите логин и пароль");
                    return;
                }

                try
                {
                    var employee = context.Employees
                        .AsNoTracking()
                        .FirstOrDefault(e => e.Login == login);

                    if (employee == null || !BCrypt.Net.BCrypt.Verify(password, employee.PasswordHash))
                    {
                        ShowError("Неверный логин или пароль");
                        return;
                    }

                    MessageBoxHelper.Show($"Добро пожаловать, {employee.FullName}!", "Успех", "ОК");
                    new DashboardWindow(employee).Show();
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBoxHelper.Show($"Ошибка при авторизации: {ex.Message}", "Ошибка", "ОК", canCopy: true);
                }
            }
        }

        private void ShowError(string message)
        {
            ErrorMessage.Text = message;
            ErrorMessage.BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.3),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(2)
            });
        }

        private void LoginTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ErrorMessage.Opacity > 0)
            {
                ErrorMessage.BeginAnimation(OpacityProperty, new DoubleAnimation
                {
                    To = 0,
                    Duration = TimeSpan.FromSeconds(0.2)
                });
            }
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            new ClientLandingWindow().Show();
            Close();
        }
    }
}