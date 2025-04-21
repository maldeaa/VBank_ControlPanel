using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using VBank_ControlPanel.Classes;
using VBank_ControlPanel.Models;
using DateTimeConverter = VBank_ControlPanel.Classes.DateTimeConverter;

namespace VBank_ControlPanel.Windows
{
    /// <summary>
    /// Логика взаимодействия для DashboardWindow.xaml
    /// </summary>
    public partial class DashboardWindow : MetroWindow, INotifyPropertyChanged
    {
        private readonly VBankContent _context;
        private readonly Employee _currentEmployee;
        private bool _isReadOnly;
        private string _fullName;
        private string _currentRoleName;
        private Visibility _canAddRecordVisibility;
        private Visibility _bcryptVisibility;

        public bool IsReadOnly
        {
            get => _isReadOnly;
            set { _isReadOnly = value; OnPropertyChanged(nameof(IsReadOnly)); }
        }

        public string FullName
        {
            get => _fullName;
            set { _fullName = value; OnPropertyChanged(nameof(FullName)); }
        }

        public string CurrentRoleName
        {
            get => _currentRoleName;
            set { _currentRoleName = value; OnPropertyChanged(nameof(CurrentRoleName)); }
        }

        public Visibility CanAddRecordVisibility
        {
            get => _canAddRecordVisibility;
            set { _canAddRecordVisibility = value; OnPropertyChanged(nameof(CanAddRecordVisibility)); }
        }

        public Visibility BCryptVisibility
        {
            get => _bcryptVisibility;
            set { _bcryptVisibility = value; OnPropertyChanged(nameof(BCryptVisibility)); }
        }

        public Visibility EmployeeVisibility => _currentEmployee.RoleId == 1 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility RoleVisibility => _currentEmployee.RoleId == 1 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility CustomerVisibility => _currentEmployee.RoleId <= 2 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility CurrencyVisibility => _currentEmployee.RoleId == 1 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility DepositTypeVisibility => _currentEmployee.RoleId == 1 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility DepositVisibility => _currentEmployee.RoleId <= 2 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility CreditVisibility => _currentEmployee.RoleId <= 2 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility CreditPaymentVisibility => Visibility.Visible;
        public Visibility OperationLogVisibility => Visibility.Visible;
        public Visibility AdminVisibility => _currentEmployee.RoleId == 1 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EditVisibility => _currentEmployee.RoleId <= 2 ? Visibility.Visible : Visibility.Collapsed;

        public DashboardWindow(Employee employee)
        {
            InitializeComponent();
            _context = new VBankContent();
            _currentEmployee = employee;

            FullName = _currentEmployee.FullName;
            CurrentRoleName = _context.Roles.FirstOrDefault(r => r.RoleId == _currentEmployee.RoleId)?.RoleName ?? "Неизвестно";
            IsReadOnly = _currentEmployee.RoleId == 3;
            CanAddRecordVisibility = Visibility.Collapsed;
            BCryptVisibility = Visibility.Collapsed;

            SearchTextBox.Text = string.Empty;

            SearchColumnComboBox.Items.Add("Все поля");
            SearchColumnComboBox.SelectedIndex = 0;

            DataContext = this;
        }

        private void UpdateCanAddRecordVisibility(string tableName)
        {
            bool canAdd = _currentEmployee.RoleId <= 2 && tableName switch
            {
                "Сотрудники" => true,
                "Роли" => _currentEmployee.RoleId == 1,
                "Клиенты" => true,
                "Валюты" => _currentEmployee.RoleId == 1,
                "Типы вкладов" => _currentEmployee.RoleId == 1,
                "Вклады" => true,
                "Кредиты" => true,
                "Логи операций" => false,
                _ => false
            };
            CanAddRecordVisibility = canAdd && _currentEmployee.RoleId == 1 && tableName == "Логи операций" ? Visibility.Visible : canAdd ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateBCryptVisibility(string tableName)
        {
            BCryptVisibility = _currentEmployee.RoleId <= 2 && tableName == "Сотрудники" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ShowEmployees_Click(object sender, RoutedEventArgs e)
        {
            TableTitle.Text = "Сотрудники";
            DataGrid.Columns.Clear();
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new Binding("EmployeeId"), IsReadOnly = true });
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "Полное имя", Binding = new Binding("FullName"), IsReadOnly = _currentEmployee.RoleId == 2 });
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "Возраст", Binding = new Binding("Age"), IsReadOnly = _currentEmployee.RoleId == 2 });
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "Телефон", Binding = new Binding("PhoneNumber"), IsReadOnly = _currentEmployee.RoleId == 2 });
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "Логин", Binding = new Binding("Login"), IsReadOnly = _currentEmployee.RoleId == 2 });
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "Хэш пароля", Binding = new Binding("PasswordHash"), IsReadOnly = _currentEmployee.RoleId == 2 });
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "Зарплата", Binding = new Binding("Salary"), IsReadOnly = _currentEmployee.RoleId == 2 });

            var roleComboColumn = new DataGridComboBoxColumn
            {
                Header = "Роль",
                SelectedValueBinding = new Binding("RoleId") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged },
                DisplayMemberPath = "RoleName",
                SelectedValuePath = "RoleId",
                IsReadOnly = _currentEmployee.RoleId == 2
            };
            roleComboColumn.ItemsSource = _context.Roles.ToList();
            DataGrid.Columns.Add(roleComboColumn);

            UpdateCanAddRecordVisibility("Сотрудники");
            UpdateBCryptVisibility("Сотрудники");
            UpdateSearchColumns();
            SearchTextBox_TextChanged(null, null);
        }

        private void ShowRoles_Click(object sender, RoutedEventArgs e)
        {
            TableTitle.Text = "Роли";
            DataGrid.Columns.Clear();
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new Binding("RoleId"), IsReadOnly = true });
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "Название", Binding = new Binding("RoleName") });
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "Описание", Binding = new Binding("Description") });
            UpdateCanAddRecordVisibility("Роли");
            UpdateBCryptVisibility("Роли");
            UpdateSearchColumns();
            SearchTextBox_TextChanged(null, null);
        }

        private void ShowCustomers_Click(object sender, RoutedEventArgs e)
        {
            TableTitle.Text = "Клиенты";
            DataGrid.Columns.Clear();
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new Binding("CustomerId"), IsReadOnly = true });
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "Полное имя", Binding = new Binding("FullName") });
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "Телефон", Binding = new Binding("PhoneNumber") });
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "Email", Binding = new Binding("Email") });
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "Паспорт", Binding = new Binding("PassportData") });
            DataGrid.Columns.Add(new DataGridCheckBoxColumn { Header = "Заблокирован", Binding = new Binding("IsBanned"), IsReadOnly = _currentEmployee.RoleId != 1 });
            UpdateCanAddRecordVisibility("Клиенты");
            UpdateBCryptVisibility("Клиенты");
            UpdateSearchColumns();
            SearchTextBox_TextChanged(null, null);
        }

        private void ShowCurrencies_Click(object sender, RoutedEventArgs e)
        {
            TableTitle.Text = "Валюты";
            DataGrid.Columns.Clear();
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new Binding("CurrencyId"), IsReadOnly = true });
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "Код", Binding = new Binding("Code") });
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "Название", Binding = new Binding("Name") });
            UpdateCanAddRecordVisibility("Валюты");
            UpdateBCryptVisibility("Валюты");
            UpdateSearchColumns();
            SearchTextBox_TextChanged(null, null);
        }

        private void ShowDepositTypes_Click(object sender, RoutedEventArgs e)
        {
            TableTitle.Text = "Типы вкладов";
            DataGrid.Columns.Clear();
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new Binding("DepositTypeId"), IsReadOnly = true });
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "Название", Binding = new Binding("Name") });
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "Мин. срок", Binding = new Binding("MinTerm") });
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "Мин. сумма", Binding = new Binding("MinAmount") });
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "Ставка", Binding = new Binding("InterestRate") });
            UpdateCanAddRecordVisibility("Типы вкладов");
            UpdateBCryptVisibility("Типы вкладов");
            UpdateSearchColumns();
            SearchTextBox_TextChanged(null, null);
        }

        private void ShowDeposits_Click(object sender, RoutedEventArgs e)
        {
            TableTitle.Text = "Вклады";
            DataGrid.Columns.Clear();
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new Binding("DepositId"), IsReadOnly = true });

            var customerComboColumn = new DataGridComboBoxColumn
            {
                Header = "Клиент",
                SelectedValueBinding = new Binding("CustomerId") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged },
                DisplayMemberPath = "FullName",
                SelectedValuePath = "CustomerId",
                IsReadOnly = true
            };
            customerComboColumn.ItemsSource = _context.Customers.ToList();
            DataGrid.Columns.Add(customerComboColumn);

            var employeeComboColumn = new DataGridComboBoxColumn
            {
                Header = "Сотрудник",
                SelectedValueBinding = new Binding("EmployeeId") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged },
                DisplayMemberPath = "FullName",
                SelectedValuePath = "EmployeeId",
                IsReadOnly = false
            };
            employeeComboColumn.ItemsSource = _context.Employees.ToList();
            DataGrid.Columns.Add(employeeComboColumn);

            var depositTypeComboColumn = new DataGridComboBoxColumn
            {
                Header = "Тип вклада",
                SelectedValueBinding = new Binding("DepositTypeId") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged },
                DisplayMemberPath = "Name",
                SelectedValuePath = "DepositTypeId",
                IsReadOnly = true
            };
            depositTypeComboColumn.ItemsSource = _context.DepositTypes.ToList();
            DataGrid.Columns.Add(depositTypeComboColumn);

            var currencyComboColumn = new DataGridComboBoxColumn
            {
                Header = "Валюта",
                SelectedValueBinding = new Binding("CurrencyId") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged },
                DisplayMemberPath = "Code",
                SelectedValuePath = "CurrencyId",
                IsReadOnly = true
            };
            currencyComboColumn.ItemsSource = _context.Currencies.ToList();
            DataGrid.Columns.Add(currencyComboColumn);

            DataGrid.Columns.Add(new DataGridTextColumn { Header = "Номер договора", Binding = new Binding("ContractNumber") });
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "Дата начала", Binding = new Binding("StartDate") { StringFormat = "yyyy-MM-dd", Converter = new DateTimeConverter() } });
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "Дата окончания", Binding = new Binding("EndDate") { StringFormat = "yyyy-MM-dd", Converter = new DateTimeConverter() } });
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "Сумма", Binding = new Binding("Amount") });
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "Итоговая сумма", Binding = new Binding("ReturnAmount") });

            UpdateCanAddRecordVisibility("Вклады");
            UpdateBCryptVisibility("Вклады");
            UpdateSearchColumns();
            SearchTextBox_TextChanged(null, null);
        }

        private void ShowCredits_Click(object sender, RoutedEventArgs e)
        {
            TableTitle.Text = "Кредиты";
            DataGrid.Columns.Clear();
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new Binding("CreditId"), IsReadOnly = true });

            var customerComboColumn = new DataGridComboBoxColumn
            {
                Header = "Клиент",
                SelectedValueBinding = new Binding("CustomerId") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged },
                DisplayMemberPath = "FullName",
                SelectedValuePath = "CustomerId",
                IsReadOnly = true
            };
            customerComboColumn.ItemsSource = _context.Customers.ToList();
            DataGrid.Columns.Add(customerComboColumn);

            var employeeComboColumn = new DataGridComboBoxColumn
            {
                Header = "Сотрудник",
                SelectedValueBinding = new Binding("EmployeeId") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged },
                DisplayMemberPath = "FullName",
                SelectedValuePath = "EmployeeId",
                IsReadOnly = false
            };
            employeeComboColumn.ItemsSource = _context.Employees.ToList();
            DataGrid.Columns.Add(employeeComboColumn);

            var currencyComboColumn = new DataGridComboBoxColumn
            {
                Header = "Валюта",
                SelectedValueBinding = new Binding("CurrencyId") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged },
                DisplayMemberPath = "Code",
                SelectedValuePath = "CurrencyId",
                IsReadOnly = true
            };
            currencyComboColumn.ItemsSource = _context.Currencies.ToList();
            DataGrid.Columns.Add(currencyComboColumn);

            DataGrid.Columns.Add(new DataGridTextColumn { Header = "Номер договора", Binding = new Binding("ContractNumber") });
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "Дата начала", Binding = new Binding("StartDate") { StringFormat = "yyyy-MM-dd HH:mm:ss", Converter = new DateTimeConverter(), ConverterParameter = "withTime" } });
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "Срок", Binding = new Binding("Term") });
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "Ставка", Binding = new Binding("InterestRate") });
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "Сумма", Binding = new Binding("Amount") });

            UpdateCanAddRecordVisibility("Кредиты");
            UpdateBCryptVisibility("Кредиты");
            UpdateSearchColumns();
            SearchTextBox_TextChanged(null, null);
        }

        private void ShowCreditPayments_Click(object sender, RoutedEventArgs e)
        {
            TableTitle.Text = "Платежи по кредитам";
            DataGrid.Columns.Clear();
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new Binding("PaymentId"), IsReadOnly = true });

            var creditComboColumn = new DataGridComboBoxColumn
            {
                Header = "Кредит",
                SelectedValueBinding = new Binding("CreditId") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged },
                DisplayMemberPath = "ContractNumber",
                SelectedValuePath = "CreditId",
                IsReadOnly = true
            };
            creditComboColumn.ItemsSource = _context.Credits.ToList();
            DataGrid.Columns.Add(creditComboColumn);

            DataGrid.Columns.Add(new DataGridTextColumn { Header = "Дата платежа", Binding = new Binding("PaymentDate") });
            DataGrid.Columns.Add(new DataGridTextColumn { Header = "Сумма", Binding = new Binding("Amount") });
            DataGrid.Columns.Add(new DataGridCheckBoxColumn { Header = "Оплачено", Binding = new Binding("IsPaid") });

            UpdateCanAddRecordVisibility("Платежи по кредитам");
            UpdateBCryptVisibility("Платежи по кредитам");
            UpdateSearchColumns();
            SearchTextBox_TextChanged(null, null);
        }

        private void ShowOperationLogs_Click(object sender, RoutedEventArgs e)
        {
            if (_currentEmployee.RoleId != 1)
            {
                TableTitle.Text = "Логи операций";
                DataGrid.Columns.Clear();
                DataGrid.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new Binding("LogId"), IsReadOnly = true });
                DataGrid.Columns.Add(new DataGridTextColumn { Header = "Сотрудник", Binding = new Binding("Employee.FullName"), IsReadOnly = true });
                DataGrid.Columns.Add(new DataGridTextColumn { Header = "Тип операции", Binding = new Binding("OperationType"), IsReadOnly = true });
                DataGrid.Columns.Add(new DataGridTextColumn { Header = "Дата", Binding = new Binding("OperationDate"), IsReadOnly = true });
                DataGrid.Columns.Add(new DataGridTextColumn { Header = "Детали", Binding = new Binding("Details"), IsReadOnly = true });
                DataGrid.IsReadOnly = true;
                UpdateCanAddRecordVisibility("Логи операций");
                UpdateBCryptVisibility("Логи операций");
                UpdateSearchColumns();
                SearchTextBox_TextChanged(null, null);
            }
            else
            {
                TableTitle.Text = "Логи операций";
                DataGrid.Columns.Clear();
                DataGrid.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new Binding("LogId"), IsReadOnly = true });
                DataGrid.Columns.Add(new DataGridTextColumn { Header = "Сотрудник", Binding = new Binding("Employee.FullName"), IsReadOnly = true });
                DataGrid.Columns.Add(new DataGridTextColumn { Header = "Тип операции", Binding = new Binding("OperationType") });
                DataGrid.Columns.Add(new DataGridTextColumn { Header = "Дата", Binding = new Binding("OperationDate") });
                DataGrid.Columns.Add(new DataGridTextColumn { Header = "Детали", Binding = new Binding("Details") });
                UpdateCanAddRecordVisibility("Логи операций");
                UpdateBCryptVisibility("Логи операций");
                UpdateSearchColumns();
                SearchTextBox_TextChanged(null, null);
            }
        }

        private void AddRecord_Click(object sender, RoutedEventArgs e)
        {
            string tableName = TableTitle.Text;
            if (string.IsNullOrEmpty(tableName) || tableName == "Выберите таблицу" ||
                !new[] { "Сотрудники", "Роли", "Клиенты", "Валюты", "Типы вкладов", "Вклады", "Кредиты" }.Contains(tableName))
            {
                return;
            }

            var addWindow = new AddRecordWindow(_context, tableName);
            if (addWindow.ShowDialog() == true)
            {
                switch (tableName)
                {
                    case "Сотрудники": ShowEmployees_Click(null, null); break;
                    case "Роли": ShowRoles_Click(null, null); break;
                    case "Клиенты": ShowCustomers_Click(null, null); break;
                    case "Валюты": ShowCurrencies_Click(null, null); break;
                    case "Типы вкладов": ShowDepositTypes_Click(null, null); break;
                    case "Вклады": ShowDeposits_Click(null, null); break;
                    case "Кредиты": ShowCredits_Click(null, null); break;
                }
            }
        }

        private void ConvertToBCrypt_Click(object sender, RoutedEventArgs e)
        {
            if (TableTitle.Text != "Сотрудники")
                return;

            var selectedEmployee = DataGrid.SelectedItem as Employee;
            if (selectedEmployee == null)
            {
                MessageBoxHelper.Show("Выберите сотрудника для преобразования пароля", "Предупреждение", "ОК");
                return;
            }

            var dialog = new MetroDialogSettings { AffirmativeButtonText = "Преобразовать", NegativeButtonText = "Отмена" };
            string result = MahApps.Metro.Controls.Dialogs.DialogManager.ShowModalInputExternal(this, "Введите новый пароль", "Введите пароль для преобразования в BCrypt", dialog);

            if (string.IsNullOrWhiteSpace(result))
            {
                MessageBoxHelper.Show("Пароль не введён", "Ошибка", "ОК");
                return;
            }

            try
            {
                string bcryptHash = BCrypt.Net.BCrypt.HashPassword(result);
                selectedEmployee.PasswordHash = bcryptHash;
                _context.SaveChanges();
                MessageBoxHelper.Show("Пароль успешно преобразован в BCrypt", "Успех", "ОК");
                ShowEmployees_Click(null, null);
            }
            catch (Exception ex)
            {
                MessageBoxHelper.Show($"Ошибка при преобразовании пароля: {ex.Message}", "Ошибка", "ОК");
            }
        }

        private void ResetChanges_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _context.ChangeTracker.Entries().ToList().ForEach(entry => entry.Reload());
                switch (TableTitle.Text)
                {
                    case "Сотрудники": ShowEmployees_Click(null, null); break;
                    case "Роли": ShowRoles_Click(null, null); break;
                    case "Клиенты": ShowCustomers_Click(null, null); break;
                    case "Валюты": ShowCurrencies_Click(null, null); break;
                    case "Типы вкладов": ShowDepositTypes_Click(null, null); break;
                    case "Вклады": ShowDeposits_Click(null, null); break;
                    case "Кредиты": ShowCredits_Click(null, null); break;
                    case "Платежи по кредитам": ShowCreditPayments_Click(null, null); break;
                    case "Логи операций": ShowOperationLogs_Click(null, null); break;
                }
                MessageBoxHelper.Show("Изменения сброшены", "Успех", "ОК");
            }
            catch (Exception ex)
            {
                MessageBoxHelper.Show($"Ошибка при сбросе изменений: {ex.Message}", "Ошибка", "ОК");
            }
        }

        private void UpdateMonthlyInterest_Click(object sender, RoutedEventArgs e)
        {
            if (_currentEmployee.RoleId != 1)
            {
                MessageBoxHelper.Show("У вас нет прав для начисления процентов", "Ошибка", "ОК");
                return;
            }

            try
            {
                _context.Database.ExecuteSqlRaw("EXEC UpdateMonthlyDepositInterest");

                foreach (var entry in _context.ChangeTracker.Entries<Deposit>().ToList())
                {
                    entry.State = EntityState.Detached;
                }

                if (TableTitle.Text == "Вклады")
                {
                    DataGrid.ItemsSource = _context.Deposits
                        .Include(d => d.Customer)
                        .Include(d => d.Employee)
                        .Include(d => d.DepositType)
                        .Include(d => d.Currency)
                        .ToList();
                }

                MessageBoxHelper.Show("Проценты успешно начислены", "Успех", "ОК");
            }
            catch (Exception ex)
            {
                MessageBoxHelper.Show($"Ошибка при начислении процентов: {ex.Message}", "Ошибка", "ОК");
            }
        }

        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (TableTitle.Text == "Вклады")
                {
                    var deposits = _context.Deposits.Local.ToList();
                    foreach (var deposit in deposits)
                    {
                        if (deposit.EndDate <= deposit.StartDate)
                        {
                            MessageBoxHelper.Show("Дата окончания вклада должна быть больше даты начала", "Ошибка", "ОК");
                            return;
                        }
                    }
                }
                else if (TableTitle.Text == "Кредиты")
                {
                    var credits = _context.Credits.Local.ToList();
                    foreach (var credit in credits)
                    {
                        if (credit.StartDate == DateTime.MinValue)
                        {
                            MessageBoxHelper.Show("Неверная дата начала кредита", "Ошибка", "ОК");
                            return;
                        }
                    }
                }

                _context.SaveChanges();
                MessageBoxHelper.Show("Изменения успешно сохранены", "Успех", "ОК");
                RefreshCurrentTable();
            }
            catch (DbUpdateException ex)
            {
                _context.ChangeTracker.Clear();
                var innerMessage = ex.InnerException?.Message ?? ex.Message;

                if (innerMessage.Contains("Номер телефона должен быть в формате"))
                {
                    MessageBoxHelper.Show("Номер телефона должен быть в формате +7XXXXXXXXXX (например, +79991234567)!", "Ошибка", "ОК");
                }
                else if (innerMessage.Contains("Email должен содержать"))
                {
                    MessageBoxHelper.Show("Email должен содержать @ и домен (например, user@domain.com)!", "Ошибка", "ОК");
                }
                else if (innerMessage.Contains("Дата окончания вклада должна быть больше даты начала"))
                {
                    MessageBoxHelper.Show("Дата окончания вклада должна быть больше даты начала!", "Ошибка", "ОК");
                }
                else if (innerMessage.Contains("Сумма вклада меньше минимальной"))
                {
                    MessageBoxHelper.Show("Сумма вклада меньше минимальной для выбранного типа вклада!", "Ошибка", "ОК");
                }
                else if (innerMessage.Contains("Паспортные данные должны быть в формате"))
                {
                    MessageBoxHelper.Show("Паспортные данные должны быть в формате XXXX YYYYYY (4 цифры серии, пробел, 6 цифр номера)!", "Ошибка", "ОК");
                }
                else if (innerMessage.Contains("Возраст сотрудника не может превышать 100 лет"))
                {
                    MessageBoxHelper.Show("Возраст сотрудника не может превышать 100 лет!", "Ошибка", "ОК");
                }
                else if (innerMessage.Contains("Сумма платежа по кредиту должна быть больше 0"))
                {
                    MessageBoxHelper.Show("Сумма платежа по кредиту должна быть больше 0!", "Ошибка", "ОК");
                }
                else if (innerMessage.Contains("Дата платежа не может быть раньше даты начала кредита"))
                {
                    MessageBoxHelper.Show("Дата платежа не может быть раньше даты начала кредита!", "Ошибка", "ОК");
                }
                else if (innerMessage.Contains("Логин должен содержать только буквы"))
                {
                    MessageBoxHelper.Show("Логин должен содержать только буквы, цифры, _ или -, и быть не короче 4 символов!", "Ошибка", "ОК");
                }
                else if (innerMessage.Contains("Забаненный клиент не может открыть вклад"))
                {
                    MessageBoxHelper.Show("Забаненный клиент не может открыть вклад!", "Ошибка", "ОК");
                }
                else if (innerMessage.Contains("Забаненный клиент не может взять кредит"))
                {
                    MessageBoxHelper.Show("Забаненный клиент не может взять кредит!", "Ошибка", "ОК");
                }
                else if (innerMessage.Contains("Нельзя удалить клиента с активными вкладами или кредитами"))
                {
                    MessageBoxHelper.Show("Нельзя удалить клиента с активными вкладами или кредитами!", "Ошибка", "ОК");
                }
                else if (innerMessage.Contains("Номер контракта вклада совпадает с номером контракта кредита"))
                {
                    MessageBoxHelper.Show("Номер контракта вклада совпадает с номером контракта кредита!", "Ошибка", "ОК");
                }
                else if (innerMessage.Contains("Номер контракта кредита совпадает с номером контракта вклада"))
                {
                    MessageBoxHelper.Show("Номер контракта кредита совпадает с номером контракта вклада!", "Ошибка", "ОК");
                }
                else if (innerMessage.Contains("Код валюты должен состоять из 3 букв"))
                {
                    MessageBoxHelper.Show("Код валюты должен состоять из 3 букв (например, RUB, USD)!", "Ошибка", "ОК");
                }
                else
                {
                    MessageBoxHelper.Show($"Ошибка при сохранении: {innerMessage}", "Ошибка", "ОК", canCopy: true);
                }
            }
            catch (Exception ex)
            {
                _context.ChangeTracker.Clear();
                MessageBoxHelper.Show($"Неизвестная ошибка: {ex.Message}", "Ошибка", "ОК", canCopy: true);
            }
        }

        private void DeleteRecord_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = DataGrid.SelectedItem;
            if (selectedItem == null)
            {
                MessageBoxHelper.Show("Выберите запись для удаления", "Предупреждение", "ОК");
                return;
            }

            if ((TableTitle.Text == "Логи операций" || TableTitle.Text == "Платежи по кредитам") && _currentEmployee.RoleId != 1)
            {
                MessageBoxHelper.Show("У вас нет прав для удаления логов операций", "Ошибка", "ОК");
                return;
            }

            var result = MessageBoxHelper.Show("Вы уверены, что хотите удалить эту запись?", "Подтверждение", "Да", true, "Отменить");
            if (result != CustomMessageBox.MessageBoxResult.OK)
                return;

            try
            {
                switch (TableTitle.Text)
                {
                    case "Сотрудники":
                        _context.Employees.Remove((Employee)selectedItem);
                        break;
                    case "Роли":
                        _context.Roles.Remove((Role)selectedItem);
                        break;
                    case "Клиенты":
                        _context.Customers.Remove((Customer)selectedItem);
                        break;
                    case "Валюты":
                        _context.Currencies.Remove((Currency)selectedItem);
                        break;
                    case "Типы вкладов":
                        _context.DepositTypes.Remove((DepositType)selectedItem);
                        break;
                    case "Вклады":
                        _context.Deposits.Remove((Deposit)selectedItem);
                        break;
                    case "Кредиты":
                        _context.Credits.Remove((Credit)selectedItem);
                        break;
                    case "Платежи по кредитам":
                        _context.CreditPayments.Remove((CreditPayment)selectedItem);
                        break;
                    case "Логи операций":
                        _context.OperationLogs.Remove((OperationLog)selectedItem);
                        break;
                    default:
                        MessageBoxHelper.Show("Удаление для этой таблицы не поддерживается", "Ошибка", "ОК");
                        return;
                }

                _context.SaveChanges();
                RefreshCurrentTable();
                MessageBoxHelper.Show("Запись успешно удалена", "Успех", "ОК");
            }
            catch (Exception ex)
            {
                MessageBoxHelper.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", "ОК");
            }
        }

        private void RefreshCurrentTable()
        {
            switch (TableTitle.Text)
            {
                case "Сотрудники": ShowEmployees_Click(null, null); break;
                case "Роли": ShowRoles_Click(null, null); break;
                case "Клиенты": ShowCustomers_Click(null, null); break;
                case "Валюты": ShowCurrencies_Click(null, null); break;
                case "Типы вкладов": ShowDepositTypes_Click(null, null); break;
                case "Вклады": ShowDeposits_Click(null, null); break;
                case "Кредиты": ShowCredits_Click(null, null); break;
                case "Платежи по кредитам": ShowCreditPayments_Click(null, null); break;
                case "Логи операций": ShowOperationLogs_Click(null, null); break;
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new MainWindow();
            loginWindow.Show();
            Close();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchTextBox.Text?.ToLower() ?? string.Empty;
            string selectedColumn = SearchColumnComboBox.SelectedItem?.ToString();

            switch (TableTitle.Text)
            {
                case "Сотрудники":
                    var employees = _context.Employees.Include(e => e.Role).AsQueryable();
                    if (selectedColumn != "Все поля")
                    {
                        employees = selectedColumn switch
                        {
                            "ID" => employees.Where(emp => emp.EmployeeId.ToString().Contains(searchText)),
                            "Полное имя" => employees.Where(emp => emp.FullName.ToLower().Contains(searchText)),
                            "Возраст" => employees.Where(emp => emp.Age.ToString().Contains(searchText)),
                            "Телефон" => employees.Where(emp => emp.PhoneNumber.ToLower().Contains(searchText)),
                            "Логин" => employees.Where(emp => emp.Login.ToLower().Contains(searchText)),
                            "Хэш пароля" => employees.Where(emp => emp.PasswordHash.ToLower().Contains(searchText)),
                            "Зарплата" => employees.Where(emp => emp.Salary.ToString().Contains(searchText)),
                            "Роль" => employees.Where(emp => emp.Role.RoleName.ToLower().Contains(searchText)),
                            _ => employees
                        };
                    }
                    else
                    {
                        employees = employees.Where(emp => searchText == string.Empty ||
                            emp.EmployeeId.ToString().Contains(searchText) ||
                            emp.FullName.ToLower().Contains(searchText) ||
                            emp.Age.ToString().Contains(searchText) ||
                            emp.PhoneNumber.ToLower().Contains(searchText) ||
                            emp.Login.ToLower().Contains(searchText) ||
                            emp.PasswordHash.ToLower().Contains(searchText) ||
                            emp.Salary.ToString().Contains(searchText) ||
                            emp.Role.RoleName.ToLower().Contains(searchText));
                    }
                    DataGrid.ItemsSource = employees.ToList();
                    break;

                case "Роли":
                    var roles = _context.Roles.AsQueryable();
                    if (selectedColumn != "Все поля")
                    {
                        roles = selectedColumn switch
                        {
                            "ID" => roles.Where(r => r.RoleId.ToString().Contains(searchText)),
                            "Название" => roles.Where(r => r.RoleName.ToLower().Contains(searchText)),
                            "Описание" => roles.Where(r => r.Description.ToLower().Contains(searchText)),
                            _ => roles
                        };
                    }
                    else
                    {
                        roles = roles.Where(r => searchText == string.Empty ||
                            r.RoleId.ToString().Contains(searchText) ||
                            r.RoleName.ToLower().Contains(searchText) ||
                            r.Description.ToLower().Contains(searchText));
                    }
                    DataGrid.ItemsSource = roles.ToList();
                    break;

                case "Клиенты":
                    var customers = _context.Customers.AsQueryable();
                    if (selectedColumn != "Все поля")
                    {
                        customers = selectedColumn switch
                        {
                            "ID" => customers.Where(c => c.CustomerId.ToString().Contains(searchText)),
                            "Полное имя" => customers.Where(c => c.FullName.ToLower().Contains(searchText)),
                            "Телефон" => customers.Where(c => c.PhoneNumber.ToLower().Contains(searchText)),
                            "Email" => customers.Where(c => c.Email.ToLower().Contains(searchText)),
                            "Паспорт" => customers.Where(c => c.PassportData.ToLower().Contains(searchText)),
                            _ => customers
                        };
                    }
                    else
                    {
                        customers = customers.Where(c => searchText == string.Empty ||
                            c.CustomerId.ToString().Contains(searchText) ||
                            c.FullName.ToLower().Contains(searchText) ||
                            c.PhoneNumber.ToLower().Contains(searchText) ||
                            c.Email.ToLower().Contains(searchText) ||
                            c.PassportData.ToLower().Contains(searchText));
                    }
                    DataGrid.ItemsSource = customers.ToList();
                    break;

                case "Валюты":
                    var currencies = _context.Currencies.AsQueryable();
                    if (selectedColumn != "Все поля")
                    {
                        currencies = selectedColumn switch
                        {
                            "ID" => currencies.Where(c => c.CurrencyId.ToString().Contains(searchText)),
                            "Код" => currencies.Where(c => c.Code.ToLower().Contains(searchText)),
                            "Название" => currencies.Where(c => c.Name.ToLower().Contains(searchText)),
                            _ => currencies
                        };
                    }
                    else
                    {
                        currencies = currencies.Where(c => searchText == string.Empty ||
                            c.CurrencyId.ToString().Contains(searchText) ||
                            c.Code.ToLower().Contains(searchText) ||
                            c.Name.ToLower().Contains(searchText));
                    }
                    DataGrid.ItemsSource = currencies.ToList();
                    break;

                case "Типы вкладов":
                    var depositTypes = _context.DepositTypes.AsQueryable();
                    if (selectedColumn != "Все поля")
                    {
                        depositTypes = selectedColumn switch
                        {
                            "ID" => depositTypes.Where(dt => dt.DepositTypeId.ToString().Contains(searchText)),
                            "Название" => depositTypes.Where(dt => dt.Name.ToLower().Contains(searchText)),
                            "Мин. срок" => depositTypes.Where(dt => dt.MinTerm.ToString().Contains(searchText)),
                            "Мин. сумма" => depositTypes.Where(dt => dt.MinAmount.ToString().Contains(searchText)),
                            "Ставка" => depositTypes.Where(dt => dt.InterestRate.ToString().Contains(searchText)),
                            _ => depositTypes
                        };
                    }
                    else
                    {
                        depositTypes = depositTypes.Where(dt => searchText == string.Empty ||
                            dt.DepositTypeId.ToString().Contains(searchText) ||
                            dt.Name.ToLower().Contains(searchText) ||
                            dt.MinTerm.ToString().Contains(searchText) ||
                            dt.MinAmount.ToString().Contains(searchText) ||
                            dt.InterestRate.ToString().Contains(searchText));
                    }
                    DataGrid.ItemsSource = depositTypes.ToList();
                    break;

                case "Вклады":
                    var deposits = _context.Deposits.Include(d => d.Customer).Include(d => d.Employee).Include(d => d.DepositType).Include(d => d.Currency).AsQueryable();
                    if (selectedColumn != "Все поля")
                    {
                        deposits = selectedColumn switch
                        {
                            "ID" => deposits.Where(d => d.DepositId.ToString().Contains(searchText)),
                            "Клиент" => deposits.Where(d => d.Customer.FullName.ToLower().Contains(searchText)),
                            "Сотрудник" => deposits.Where(d => d.Employee.FullName.ToLower().Contains(searchText)),
                            "Тип вклада" => deposits.Where(d => d.DepositType.Name.ToLower().Contains(searchText)),
                            "Валюта" => deposits.Where(d => d.Currency.Code.ToLower().Contains(searchText)),
                            "Номер договора" => deposits.Where(d => d.ContractNumber.ToLower().Contains(searchText)),
                            "Дата начала" => deposits.Where(d => d.StartDate.ToString().Contains(searchText)),
                            "Дата окончания" => deposits.Where(d => d.EndDate.ToString().Contains(searchText)),
                            "Сумма" => deposits.Where(d => d.Amount.ToString().Contains(searchText)),
                            "Итоговая сумма" => deposits.Where(d => d.ReturnAmount.ToString().Contains(searchText)),
                            _ => deposits
                        };
                    }
                    else
                    {
                        deposits = deposits.Where(d => searchText == string.Empty ||
                            d.DepositId.ToString().Contains(searchText) ||
                            d.Customer.FullName.ToLower().Contains(searchText) ||
                            d.Employee.FullName.ToLower().Contains(searchText) ||
                            d.DepositType.Name.ToLower().Contains(searchText) ||
                            d.Currency.Code.ToLower().Contains(searchText) ||
                            d.ContractNumber.ToLower().Contains(searchText) ||
                            d.StartDate.ToString().Contains(searchText) ||
                            d.EndDate.ToString().Contains(searchText) ||
                            d.Amount.ToString().Contains(searchText) ||
                            d.ReturnAmount.ToString().Contains(searchText));
                    }
                    DataGrid.ItemsSource = deposits.ToList();
                    break;

                case "Кредиты":
                    var credits = _context.Credits.Include(c => c.Customer).Include(c => c.Employee).Include(c => c.Currency).AsQueryable();
                    if (selectedColumn != "Все поля")
                    {
                        credits = selectedColumn switch
                        {
                            "ID" => credits.Where(c => c.CreditId.ToString().Contains(searchText)),
                            "Клиент" => credits.Where(c => c.Customer.FullName.ToLower().Contains(searchText)),
                            "Сотрудник" => credits.Where(c => c.Employee.FullName.ToLower().Contains(searchText)),
                            "Валюта" => credits.Where(c => c.Currency.Code.ToLower().Contains(searchText)),
                            "Номер договора" => credits.Where(c => c.ContractNumber.ToLower().Contains(searchText)),
                            "Дата начала" => credits.Where(c => c.StartDate.ToString().Contains(searchText)),
                            "Срок" => credits.Where(c => c.Term.ToString().Contains(searchText)),
                            "Ставка" => credits.Where(c => c.InterestRate.ToString().Contains(searchText)),
                            "Сумма" => credits.Where(c => c.Amount.ToString().Contains(searchText)),
                            _ => credits
                        };
                    }
                    else
                    {
                        credits = credits.Where(c => searchText == string.Empty ||
                            c.CreditId.ToString().Contains(searchText) ||
                            c.Customer.FullName.ToLower().Contains(searchText) ||
                            c.Employee.FullName.ToLower().Contains(searchText) ||
                            c.Currency.Code.ToLower().Contains(searchText) ||
                            c.ContractNumber.ToLower().Contains(searchText) ||
                            c.StartDate.ToString().Contains(searchText) ||
                            c.Term.ToString().Contains(searchText) ||
                            c.InterestRate.ToString().Contains(searchText) ||
                            c.Amount.ToString().Contains(searchText));
                    }
                    DataGrid.ItemsSource = credits.ToList();
                    break;

                case "Платежи по кредитам":
                    var payments = _context.CreditPayments.Include(p => p.Credit).AsQueryable();
                    if (selectedColumn != "Все поля")
                    {
                        payments = selectedColumn switch
                        {
                            "ID" => payments.Where(p => p.PaymentId.ToString().Contains(searchText)),
                            "Кредит" => payments.Where(p => p.Credit.ContractNumber.ToLower().Contains(searchText)),
                            "Дата платежа" => payments.Where(p => p.PaymentDate.ToString().Contains(searchText)),
                            "Сумма" => payments.Where(p => p.Amount.ToString().Contains(searchText)),
                            "Оплачено" => payments.Where(p => p.IsPaid == true.ToString().ToLower().Contains(searchText)),
                            _ => payments
                        };
                    }
                    else
                    {
                        payments = payments.Where(p => searchText == string.Empty ||
                            p.PaymentId.ToString().Contains(searchText) ||
                            p.Credit.ContractNumber.ToLower().Contains(searchText) ||
                            p.PaymentDate.ToString().Contains(searchText) ||
                            p.Amount.ToString().Contains(searchText) ||
                            p.IsPaid == true.ToString().ToLower().Contains(searchText));
                    }
                    DataGrid.ItemsSource = payments.ToList();
                    break;

                case "Логи операций":
                    var logs = _context.OperationLogs.Include(l => l.Employee).AsQueryable();
                    if (selectedColumn != "Все поля")
                    {
                        logs = selectedColumn switch
                        {
                            "ID" => logs.Where(l => l.LogId.ToString().Contains(searchText)),
                            "Сотрудник" => logs.Where(l => l.Employee.FullName.ToLower().Contains(searchText)),
                            "Тип операции" => logs.Where(l => l.OperationType.ToLower().Contains(searchText)),
                            "Дата" => logs.Where(l => l.OperationDate.ToString().Contains(searchText)),
                            "Детали" => logs.Where(l => l.Details.ToLower().Contains(searchText)),
                            _ => logs
                        };
                    }
                    else
                    {
                        logs = logs.Where(l => searchText == string.Empty ||
                            l.LogId.ToString().Contains(searchText) ||
                            l.Employee.FullName.ToLower().Contains(searchText) ||
                            l.OperationType.ToLower().Contains(searchText) ||
                            l.OperationDate.ToString().Contains(searchText) ||
                            l.Details.ToLower().Contains(searchText));
                    }
                    DataGrid.ItemsSource = logs.ToList();
                    break;
            }
        }

        private void UpdateSearchColumns()
        {
            SearchColumnComboBox.Items.Clear();
            SearchColumnComboBox.Items.Add("Все поля");

            foreach (var column in DataGrid.Columns.OfType<DataGridTextColumn>())
            {
                if (column.Header != null)
                    SearchColumnComboBox.Items.Add(column.Header.ToString());
            }
            SearchColumnComboBox.SelectedIndex = 0;
        }

        private void SearchColumnComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SearchTextBox_TextChanged(null, null);
        }

        private void ReloadTable_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _context.ChangeTracker.Clear();
                RefreshCurrentTable();
                MessageBoxHelper.Show("Таблица успешно перезагружена", "Успех", "ОК");
            }
            catch (Exception ex)
            {
                MessageBoxHelper.Show($"Ошибка при перезагрузке таблицы: {ex.Message}", "Ошибка", "ОК");
            }
        }

        private void UpdateMonthlyCreditPayments_Click(object sender, RoutedEventArgs e)
        {
            if (_currentEmployee.RoleId != 1)
            {
                MessageBoxHelper.Show("У вас нет прав для обновления платежей по кредитам", "Ошибка", "ОК");
                return;
            }

            try
            {
                _context.Database.ExecuteSqlRaw("EXEC UpdateMonthlyCreditPayments");

                foreach (var entry in _context.ChangeTracker.Entries<CreditPayment>().ToList())
                {
                    entry.State = EntityState.Detached;
                }

                if (TableTitle.Text == "Платежи по кредитам")
                {
                    DataGrid.ItemsSource = _context.CreditPayments
                        .Include(p => p.Credit)
                        .ToList();
                }

                MessageBoxHelper.Show("Платежи по кредитам успешно обновлены", "Успех", "ОК");
            }
            catch (Exception ex)
            {
                MessageBoxHelper.Show($"Ошибка при обновлении платежей: {ex.Message}", "Ошибка", "ОК");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

