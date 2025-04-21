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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace VBank_ControlPanel
{
    /// <summary>
    /// Логика взаимодействия для CustomMessageBox.xaml
    /// </summary>
    public partial class CustomMessageBox : Window
    {
        public enum MessageBoxResult
        {
            OK,
            Cancel
        }

        public MessageBoxResult Result { get; private set; }

        public CustomMessageBox(string message, string title = "Сообщение", string okText = "Ок", bool showCancel = false, string cancelText = "Отмена", bool showClose = true, bool canCopy = false, bool isDarkTheme = false)
        {
            InitializeComponent();

            if (canCopy)
            {
                MessageText.ToolTip = "Вы можете скопировать этот текст, нажав на ЛКМ!";
                MessageText.MouseLeftButtonDown += (s, e) => Clipboard.SetText(message);
            }

            if (isDarkTheme)
            {
                TitleText.Foreground = new SolidColorBrush(Colors.White);
                TitleBar.Background = new SolidColorBrush(Colors.Black);
            }

            MessageText.Text = message;
            TitleText.Text = title;
            OkButton.Content = okText;
            CancelButton.Content = cancelText;
            CancelButton.Visibility = showCancel ? Visibility.Visible : Visibility.Collapsed;
            CloseButton.Visibility = showClose ? Visibility.Visible : Visibility.Collapsed;
            TitleBar.Padding = new Thickness(10, showClose ? 0 : 3, 0, showClose ? 0 : 3);
            Result = MessageBoxResult.Cancel;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.OK;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Cancel;
            Close();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            this.WindowState = WindowState.Normal;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Cancel;
            Close();
        }
    }
}
