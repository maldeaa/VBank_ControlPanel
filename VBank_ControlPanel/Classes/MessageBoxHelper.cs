using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace VBank_ControlPanel.Classes
{
    public static class MessageBoxHelper
    {
        public static CustomMessageBox.MessageBoxResult Show(string message, string title = "Сообщение", string okText = "Ок", bool showCancel = false, string cancelText = "Отмена", bool showClose = true, bool canCopy = false, bool isDarkTheme = false)
        {
            CustomMessageBox msgBox = new CustomMessageBox(message, title, okText, showCancel, cancelText, showClose, canCopy, isDarkTheme);

            Window owner = null;

            if (Application.Current != null && Application.Current.MainWindow != null && Application.Current.MainWindow.IsVisible)
            {
                owner = Application.Current.MainWindow;
            }
            else if (Application.Current != null && Application.Current.Windows.Count > 0)
            {
                owner = Application.Current.Windows
                    .OfType<Window>()
                    .FirstOrDefault(w => w.IsActive) 
                    ?? Application.Current.Windows[0]; 
            }

            if (owner != null)
            {
                msgBox.Owner = owner;
            }

            msgBox.ShowDialog();
            return msgBox.Result;
        }
    }
}

