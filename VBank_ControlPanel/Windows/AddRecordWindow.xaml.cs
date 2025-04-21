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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VBank_ControlPanel.Classes;
using VBank_ControlPanel.Models;

namespace VBank_ControlPanel.Windows
{
    /// <summary>
    /// Логика взаимодействия для AddRecordWindow.xaml
    /// </summary>
    public partial class AddRecordWindow : MetroWindow
    {
        private readonly VBankContent _context;
        private readonly string _tableName;
        private readonly Dictionary<string, Control> _inputFields = new Dictionary<string, Control>();
        private readonly HashSet<string> _requiredFields;
        private readonly Button _addButton;

        public AddRecordWindow(VBankContent context, string tableName)
        {
            InitializeComponent();
            _context = context;
            _tableName = tableName;
            _requiredFields = GetRequiredFields(tableName);
            TitleText.Text = $"Добавление в {_tableName}";
            GenerateInputFields();

            _addButton = (Button)FindName("AddButton");
            UpdateAddButtonState();
        }

        private void GenerateInputFields()
        {
            Type entityType = GetEntityType(_tableName);
            if (entityType == null)
            {
                MessageBoxHelper.Show("Неизвестная таблица", "Ошибка", "ОК");
                Close();
                return;
            }

            var entityMetadata = _context.Model.FindEntityType(entityType);
            if (entityMetadata == null)
            {
                MessageBoxHelper.Show("Не удалось найти метаданные сущности", "Ошибка", "ОК");
                Close();
                return;
            }

            foreach (var property in entityType.GetProperties())
            {
                if (property.GetSetMethod() == null)
                    continue;

                if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
                    continue;
                if (typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType) && property.PropertyType != typeof(string))
                    continue;

                var propertyMetadata = entityMetadata.FindProperty(property.Name);
                bool isIdentity = propertyMetadata?.ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd;
                if (isIdentity)
                    continue;

                var label = new TextBlock
                {
                    Text = property.Name + (_requiredFields.Contains(property.Name) ? " *" : ""),
                    Foreground = Brushes.White,
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                InputFields.Children.Add(label);

                Control inputControl;

                var foreignKey = entityMetadata.GetForeignKeys().FirstOrDefault(fk => fk.Properties.Any(p => p.Name == property.Name));
                if (foreignKey != null)
                {
                    inputControl = new ComboBox
                    {
                        Height = 40,
                        Margin = new Thickness(0, 0, 0, 15),
                        Style = (Style)FindResource("ZBankComboBoxStyle")
                    };

                    var principalType = foreignKey.PrincipalEntityType.ClrType;
                    if (principalType == typeof(Role))
                    {
                        ((ComboBox)inputControl).ItemsSource = _context.Roles.ToList();
                        ((ComboBox)inputControl).DisplayMemberPath = "RoleName";
                        ((ComboBox)inputControl).SelectedValuePath = "RoleId";
                    }
                    else if (principalType == typeof(Customer))
                    {
                        ((ComboBox)inputControl).ItemsSource = _context.Customers.ToList();
                        ((ComboBox)inputControl).DisplayMemberPath = "FullName";
                        ((ComboBox)inputControl).SelectedValuePath = "CustomerId";
                    }
                    else if (principalType == typeof(Employee))
                    {
                        ((ComboBox)inputControl).ItemsSource = _context.Employees.ToList();
                        ((ComboBox)inputControl).DisplayMemberPath = "FullName";
                        ((ComboBox)inputControl).SelectedValuePath = "EmployeeId";
                    }
                    else if (principalType == typeof(DepositType))
                    {
                        ((ComboBox)inputControl).ItemsSource = _context.DepositTypes.ToList();
                        ((ComboBox)inputControl).DisplayMemberPath = "Name";
                        ((ComboBox)inputControl).SelectedValuePath = "DepositTypeId";
                    }
                    else if (principalType == typeof(Currency))
                    {
                        ((ComboBox)inputControl).ItemsSource = _context.Currencies.ToList();
                        ((ComboBox)inputControl).DisplayMemberPath = "Name";
                        ((ComboBox)inputControl).SelectedValuePath = "CurrencyId";
                    }

                    ((ComboBox)inputControl).SelectionChanged += (s, e) => UpdateAddButtonState();
                }
                else if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
                {
                    inputControl = new DateTimePicker
                    {
                        Height = 40,
                        Margin = new Thickness(0, 0, 0, 15),
                        Style = (Style)FindResource("DarkDateTimePickerStyle"),
                        SelectedDateTime = DateTime.Now
                    };
                    ((DateTimePicker)inputControl).SelectedDateTimeChanged += (s, e) => UpdateAddButtonState();
                }
                else
                {
                    inputControl = new TextBox
                    {
                        Style = (Style)FindResource("ZBankTextBoxStyle"),
                        Height = 40,
                        Margin = new Thickness(0, 0, 0, 15)
                    };
                    ((TextBox)inputControl).TextChanged += (s, e) => UpdateAddButtonState();
                }

                InputFields.Children.Add(inputControl);
                _inputFields[property.Name] = inputControl;
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Type entityType = GetEntityType(_tableName);
                var entity = Activator.CreateInstance(entityType);
                var entityMetadata = _context.Model.FindEntityType(entityType);

                foreach (var property in entityType.GetProperties())
                {
                    var propertyMetadata = entityMetadata?.FindProperty(property.Name);
                    bool isIdentity = propertyMetadata?.ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd;
                    if (isIdentity)
                        continue;

                    if (_inputFields.ContainsKey(property.Name))
                    {
                        var control = _inputFields[property.Name];
                        object value = null;

                        if (control is ComboBox comboBox)
                        {
                            value = comboBox.SelectedValue;
                        }
                        else if (control is TextBox textBox)
                        {
                            string text = textBox.Text;
                            if (string.IsNullOrEmpty(text) && !_requiredFields.Contains(property.Name))
                                continue;

                            Type underlyingType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                            value = string.IsNullOrEmpty(text) && property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)
                                ? null
                                : Convert.ChangeType(text, underlyingType);
                        }
                        else if (control is DateTimePicker dateTimePicker)
                        {
                            value = dateTimePicker.SelectedDateTime;
                        }

                        property.SetValue(entity, value);
                    }
                }

                Console.WriteLine($"Adding entity to {_tableName}: {System.Text.Json.JsonSerializer.Serialize(entity)}");

                if (_tableName == "Вклады")
                {
                    var deposit = (Deposit)entity;
                    if (!ValidateDeposit(deposit))
                        return;
                }

                switch (_tableName)
                {
                    case "Сотрудники":
                        if (_context.Employees.Any(e => e.Login == ((Employee)entity).Login))
                            throw new Exception("Логин уже существует");
                        _context.Employees.Add((Employee)entity);
                        break;
                    case "Роли":
                        if (_context.Roles.Any(r => r.RoleName == ((Role)entity).RoleName))
                            throw new Exception("Роль с таким именем уже существует");
                        _context.Roles.Add((Role)entity);
                        break;
                    case "Клиенты":
                        if (_context.Customers.Any(c => c.PassportData == ((Customer)entity).PassportData))
                            throw new Exception("Паспортные данные уже существуют");
                        _context.Customers.Add((Customer)entity);
                        break;
                    case "Валюты":
                        if (_context.Currencies.Any(c => c.Code == ((Currency)entity).Code))
                            throw new Exception("Код валюты уже существует");
                        _context.Currencies.Add((Currency)entity);
                        break;
                    case "Типы вкладов": _context.DepositTypes.Add((DepositType)entity); break;
                    case "Вклады":
                        if (_context.Deposits.Any(d => d.ContractNumber == ((Deposit)entity).ContractNumber))
                            throw new Exception("Номер контракта уже существует");
                        _context.Deposits.Add((Deposit)entity);
                        break;
                    case "Кредиты":
                        if (_context.Credits.Any(c => c.ContractNumber == ((Credit)entity).ContractNumber))
                            throw new Exception("Номер контракта уже существует");
                        _context.Credits.Add((Credit)entity);
                        break;
                }

                int rowsAffected = _context.SaveChanges();
                Console.WriteLine($"Rows affected: {rowsAffected}");

                MessageBoxHelper.Show("Запись успешно добавлена", "Успех", "ОК");
                DialogResult = true;
                Close();
            }
            catch (DbUpdateException ex)
            {
                _context.ChangeTracker.Clear();
                MessageBoxHelper.Show($"Ошибка базы данных: {ex.InnerException?.Message ?? ex.Message}", "Ошибка", "ОК");
            }
            catch (Exception ex)
            {
                _context.ChangeTracker.Clear();
                MessageBoxHelper.Show($"Ошибка при добавлении: {ex.Message}", "Ошибка", "ОК");
            }
        }

        private HashSet<string> GetRequiredFields(string tableName)
        {
            return tableName switch
            {
                "Сотрудники" => new HashSet<string> { "RoleId", "FullName", "Login", "PasswordHash" },
                "Роли" => new HashSet<string> { "RoleName" },
                "Клиенты" => new HashSet<string> { "FullName", "PassportData" },
                "Валюты" => new HashSet<string> { "Code", "Name" },
                "Типы вкладов" => new HashSet<string> { "Name", "MinTerm", "MinAmount", "InterestRate" },
                "Вклады" => new HashSet<string> { "CustomerId", "EmployeeId", "DepositTypeId", "CurrencyId", "ContractNumber", "StartDate", "EndDate", "Amount" },
                "Кредиты" => new HashSet<string> { "CustomerId", "EmployeeId", "CurrencyId", "ContractNumber", "StartDate", "Term", "InterestRate", "Amount" },
                _ => new HashSet<string>()
            };
        }

        private Type GetEntityType(string tableName)
        {
            return tableName switch
            {
                "Сотрудники" => typeof(Employee),
                "Роли" => typeof(Role),
                "Клиенты" => typeof(Customer),
                "Валюты" => typeof(Currency),
                "Типы вкладов" => typeof(DepositType),
                "Вклады" => typeof(Deposit),
                "Кредиты" => typeof(Credit),
                _ => null
            };
        }

        private void UpdateAddButtonState()
        {
            _addButton.IsEnabled = _requiredFields.All(field =>
            {
                if (!_inputFields.ContainsKey(field)) return true;
                var control = _inputFields[field];
                return control switch
                {
                    TextBox textBox => !string.IsNullOrWhiteSpace(textBox.Text),
                    DateTimePicker dateTimePicker => dateTimePicker.SelectedDateTime.HasValue,
                    ComboBox comboBox => comboBox.SelectedItem != null,
                    _ => false
                };
            });
        }

        private bool ValidateDeposit(Deposit deposit)
        {
            if (deposit.EndDate <= deposit.StartDate)
            {
                MessageBoxHelper.Show("Дата окончания вклада должна быть больше даты начала", "Ошибка", "ОК");
                return false;
            }

            var depositType = _context.DepositTypes.Find(deposit.DepositTypeId);
            if (depositType != null && deposit.Amount < depositType.MinAmount)
            {
                MessageBoxHelper.Show("Сумма вклада меньше минимальной", "Ошибка", "ОК");
                return false;
            }

            return true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
