using MahApps.Metro.Controls;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VBank_ControlPanel.Classes;
using VBank_ControlPanel.Models;

namespace VBank_ControlPanel.Windows
{
    /// <summary>
    /// Логика взаимодействия для ClientLandingWindow.xaml
    /// </summary>
    public partial class ClientLandingWindow : MetroWindow
    {
        private bool isPanelOpen = false;
        private readonly VBankContent _context = new VBankContent();

        public ClientLandingWindow()
        {
            InitializeComponent();
        }

        private void ClientLogin_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow(true).Show();
            Close();
        }

        private void EmployeeLogin_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow().Show();
            Close();
        }

        private void HowToBecomeClient_Click(object sender, RoutedEventArgs e)
        {
            if (!isPanelOpen)
            {
                OpenFloatingPanel();
            }
            else
            {
                CloseFloatingPanel();
            }
        }

        private void ShowRegistrationPanel_Click(object sender, RoutedEventArgs e)
        {
            CloseFloatingPanel();
            RegistrationLayer.Visibility = Visibility.Visible;

            var fadeInOverlay = new DoubleAnimation(0, 0.5, TimeSpan.FromSeconds(0.3));
            var fadeInPanel = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3));

            RegistrationOverlayBackground.BeginAnimation(OpacityProperty, fadeInOverlay);
            RegistrationPanel.BeginAnimation(OpacityProperty, fadeInPanel);
        }

        private void CloseRegistrationPanel_Click(object sender, RoutedEventArgs e)
        {
            CloseRegistrationPanel();
        }

        private void CloseRegistrationPanel()
        {
            var fadeOutOverlay = new DoubleAnimation(0.5, 0, TimeSpan.FromSeconds(0.3));
            var fadeOutPanel = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3));

            fadeOutOverlay.Completed += (s, e) =>
            {
                RegistrationLayer.Visibility = Visibility.Collapsed;
            };

            RegistrationOverlayBackground.BeginAnimation(OpacityProperty, fadeOutOverlay);
            RegistrationPanel.BeginAnimation(OpacityProperty, fadeOutPanel);
        }

        private void SubmitRegistration_Click(object sender, RoutedEventArgs e)
        {
            string fullName = FullNameTextBox.Text.Trim();
            string passportData = PassportTextBox.Text.Trim();
            string phoneNumber = PhoneNumberTextBox.Text.Trim();
            string email = EmailTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(passportData) ||
                string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(email))
            {
                MessageBoxHelper.Show("Заполните все поля!", "Ошибка", "ОК");
                return;
            }

            var customer = new Customer
            {
                FullName = fullName,
                PassportData = passportData,
                PhoneNumber = phoneNumber,
                Email = email,
                IsBanned = false
            };

            try
            {
                _context.Customers.Add(customer);
                _context.SaveChanges();
                MessageBoxHelper.Show("Регистрация успешна! Теперь вы можете войти в личный кабинет.", "Успех", "ОК");
                CloseRegistrationPanel();

                FullNameTextBox.Text = string.Empty;
                PassportTextBox.Text = string.Empty;
                PhoneNumberTextBox.Text = string.Empty;
                EmailTextBox.Text = string.Empty;

                var loginWindow = new MainWindow(true);
                loginWindow.Show();
                Close();
            }
            catch (DbUpdateException ex)
            {
                _context.ChangeTracker.Clear();
                var innerMessage = ex.InnerException?.Message ?? ex.Message;

                if (innerMessage.Contains("Паспортные данные должны быть в формате"))
                {
                    MessageBoxHelper.Show("Паспортные данные должны быть в формате XXXX YYYYYY (4 цифры серии, пробел, 6 цифр номера)!", "Ошибка", "ОК");
                }
                else if (innerMessage.Contains("Номер телефона должен быть в формате"))
                {
                    MessageBoxHelper.Show("Номер телефона должен быть в формате +7XXXXXXXXXX (например, +79991234567)!", "Ошибка", "ОК");
                }
                else if (innerMessage.Contains("Email должен содержать"))
                {
                    MessageBoxHelper.Show("Email должен содержать @ и домен (например, user@domain.com)!", "Ошибка", "ОК");
                }
                else if (innerMessage.Contains("UNIQUE constraint failed") || innerMessage.Contains("The duplicate key value is"))
                {
                    MessageBoxHelper.Show("Клиент с такими паспортными данными уже зарегистрирован!", "Ошибка", "ОК");
                }
                else
                {
                    MessageBoxHelper.Show($"Ошибка при регистрации: {innerMessage}", "Ошибка", "ОК", canCopy: true);
                }
            }
            catch (Exception ex)
            {
                _context.ChangeTracker.Clear();
                MessageBoxHelper.Show($"Неизвестная ошибка: {ex.Message}", "Ошибка", "ОК", canCopy: true);
            }
        }

        private void ClosePanel_Click(object sender, RoutedEventArgs e)
        {
            CloseFloatingPanel();
        }

        private void FloatingPanelAnimation_Completed(object sender, EventArgs e)
        {
            OverlayLayer.IsEnabled = true;
        }

        private void OpenFloatingPanel()
        {
            isPanelOpen = true;
            OverlayLayer.Visibility = Visibility.Visible;
            var storyboard = new Storyboard();
            storyboard.Completed += FloatingPanelAnimation_Completed;

            var panelOpacityAnimation = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3));
            var backgroundOpacityAnimation = new DoubleAnimation(0, 0.5, TimeSpan.FromSeconds(0.3));

            Storyboard.SetTarget(panelOpacityAnimation, FloatingPanel);
            Storyboard.SetTarget(backgroundOpacityAnimation, OverlayBackground);
            Storyboard.SetTargetProperty(panelOpacityAnimation, new PropertyPath("Opacity"));
            Storyboard.SetTargetProperty(backgroundOpacityAnimation, new PropertyPath("Opacity"));

            storyboard.Children.Add(panelOpacityAnimation);
            storyboard.Children.Add(backgroundOpacityAnimation);
            storyboard.Begin();
        }

        private void CloseFloatingPanel()
        {
            isPanelOpen = false;
            var storyboard = new Storyboard();
            storyboard.Completed += (s, e) => OverlayLayer.Visibility = Visibility.Collapsed;

            var panelOpacityAnimation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3));
            var backgroundOpacityAnimation = new DoubleAnimation(0.5, 0, TimeSpan.FromSeconds(0.3));

            Storyboard.SetTarget(panelOpacityAnimation, FloatingPanel);
            Storyboard.SetTarget(backgroundOpacityAnimation, OverlayBackground);
            Storyboard.SetTargetProperty(panelOpacityAnimation, new PropertyPath("Opacity"));
            Storyboard.SetTargetProperty(backgroundOpacityAnimation, new PropertyPath("Opacity"));

            storyboard.Children.Add(panelOpacityAnimation);
            storyboard.Children.Add(backgroundOpacityAnimation);
            storyboard.Begin();
        }
    }
}
