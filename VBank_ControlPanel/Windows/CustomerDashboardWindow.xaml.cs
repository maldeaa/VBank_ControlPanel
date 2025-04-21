using MahApps.Metro.Controls;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VBank_ControlPanel.Classes;
using VBank_ControlPanel.Models;
using System.Timers;

namespace VBank_ControlPanel.Windows
{
    public partial class CustomerDashboardWindow : MetroWindow
    {
        private readonly VBankContent context;
        private Customer customer;
        private bool isPanelOpen = false;
        private readonly System.Timers.Timer _timer;
        private readonly System.Timers.Timer _banCheckTimer;
        private bool _isClosing = false;

        public CustomerDashboardWindow(Customer customer)
        {
            InitializeComponent();
            this.customer = customer;
            context = new VBankContent();
            LoadCustomerData();

            _timer = new System.Timers.Timer(10000);
            _timer.Elapsed += (s, e) => Dispatcher.Invoke(LoadCustomerData);
            _timer.AutoReset = true;
            _timer.Start();

            _banCheckTimer = new System.Timers.Timer(1000);
            _banCheckTimer.Elapsed += (s, e) => Dispatcher.Invoke(CheckBanStatus);
            _banCheckTimer.AutoReset = true;
            _banCheckTimer.Start();
        }

        private void CheckBanStatus()
        {
            using (var context = new VBankContent())
            {
                var freshCustomer = context.Customers
                    .AsNoTracking()
                    .FirstOrDefault(c => c.CustomerId == customer.CustomerId);

                if (freshCustomer != null && freshCustomer.IsBanned != customer.IsBanned)
                {
                    customer = freshCustomer;
                    if (customer.IsBanned)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            MainContent.Visibility = Visibility.Collapsed;
                            BannedOverlayLayer.Visibility = Visibility.Visible;
                            DepositsItemsControl.ItemsSource = null;
                            CreditsItemsControl.ItemsSource = null;
                        });
                    }
                    else
                    {
                        Dispatcher.Invoke(() =>
                        {
                            MainContent.Visibility = Visibility.Visible;
                            BannedOverlayLayer.Visibility = Visibility.Collapsed;
                            LoadCustomerData();
                        });
                    }
                }
            }
        }

        private void LoadCustomerData()
        {
            if (_isClosing || context == null || context.Database == null)
            {
                return;
            }

            Dispatcher.Invoke(() =>
            {
                context.ChangeTracker.Clear();

                var freshCustomer = context.Customers
                    .AsNoTracking()
                    .FirstOrDefault(c => c.CustomerId == customer.CustomerId);

                if (freshCustomer != null)
                {
                    customer = freshCustomer;
                    FullNameTextBlock.Text = $"ФИО: {customer.FullName}";
                    PhoneTextBlock.Text = $"Телефон: {customer.PhoneNumber}";
                    EmailTextBlock.Text = $"Email: {customer.Email}";
                    PassportTextBlock.Text = $"Паспорт: {customer.PassportData}";

                    if (customer.IsBanned)
                    {
                        MainContent.Visibility = Visibility.Collapsed;
                        BannedOverlayLayer.Visibility = Visibility.Visible;
                        DepositsItemsControl.ItemsSource = null;
                        CreditsItemsControl.ItemsSource = null;
                        return;
                    }
                    else
                    {
                        MainContent.Visibility = Visibility.Visible;
                        BannedOverlayLayer.Visibility = Visibility.Collapsed;
                    }

                    var deposits = context.Deposits
                        .Include(d => d.Currency)
                        .Include(d => d.DepositType)
                        .AsNoTracking()
                        .Where(d => d.CustomerId == customer.CustomerId)
                        .ToList();

                    foreach (var deposit in deposits)
                    {
                        Console.WriteLine($"Вклад {deposit.ContractNumber}: Процент = {deposit.DepositType?.InterestRate}");
                    }

                    DepositsItemsControl.ItemsSource = null;
                    DepositsItemsControl.ItemsSource = deposits;

                    var credits = context.Credits
                        .Include(c => c.Currency)
                        .Include(c => c.CreditPayments)
                        .AsNoTracking()
                        .Where(c => c.CustomerId == customer.CustomerId)
                        .ToList();

                    CreditsItemsControl.ItemsSource = null;
                    CreditsItemsControl.ItemsSource = credits.Select(c => new
                    {
                        Credit = c,
                        PayButtonVisibility = c.CreditPayments.Any(p => !p.IsPaid == true) ? Visibility.Visible : Visibility.Collapsed
                    });
                }
                else
                {
                    MessageBoxHelper.Show("Не удалось загрузить данные клиента!", "Ошибка", "ОК");
                }
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            _isClosing = true;

            if (_timer != null)
            {
                _timer.Stop();
                _timer.Elapsed -= (s, e) => Dispatcher.Invoke(LoadCustomerData);
                _timer.Dispose();
            }

            if (_banCheckTimer != null)
            {
                _banCheckTimer.Stop();
                _banCheckTimer.Elapsed -= (s, e) => Dispatcher.Invoke(CheckBanStatus);
                _banCheckTimer.Dispose();
            }

            if (context != null)
            {
                context.Dispose();
            }

            base.OnClosed(e);
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow(true).Show();
            Close();
        }

        private void OpenNewDeposit_Click(object sender, RoutedEventArgs e)
        {
            if (customer.IsBanned)
            {
                MessageBoxHelper.Show("Ваш аккаунт заблокирован. Вы не можете совершать операции.", "Ошибка", "ОК");
                return;
            }
            ShowFloatingPanel(CreateNewDepositPanel());
        }

        private void TakeNewCredit_Click(object sender, RoutedEventArgs e)
        {
            if (customer.IsBanned)
            {
                MessageBoxHelper.Show("Ваш аккаунт заблокирован. Вы не можете совершать операции.", "Ошибка", "ОК");
                return;
            }
            ShowFloatingPanel(CreateNewCreditPanel());
        }

        private void ReplenishDeposit_Click(object sender, RoutedEventArgs e)
        {
            if (customer.IsBanned)
            {
                MessageBoxHelper.Show("Ваш аккаунт заблокирован. Вы не можете совершать операции.", "Ошибка", "ОК");
                return;
            }
            var button = (Button)sender;
            int depositId = (int)button.Tag;
            ShowFloatingPanel(CreateReplenishDepositPanel(depositId));
        }

        private void PayCredit_Click(object sender, RoutedEventArgs e)
        {
            if (customer.IsBanned)
            {
                MessageBoxHelper.Show("Ваш аккаунт заблокирован. Вы не можете совершать операции.", "Ошибка", "ОК");
                return;
            }
            var button = (Button)sender;
            int creditId = (int)button.Tag;
            ShowFloatingPanel(CreatePayCreditPanel(creditId));
        }

        private StackPanel CreateNewDepositPanel()
        {
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = "Открыть новый вклад", Style = FindResource("BentoTextStyle") as Style, FontSize = 18, FontWeight = FontWeights.Bold });

            var typesCombo = new ComboBox
            {
                Style = FindResource("ZBankComboBoxStyle") as Style,
                ItemsSource = context.DepositTypes.ToList(),
                DisplayMemberPath = "Name",
                Margin = new Thickness(0, 10, 0, 0),
                SelectedIndex = -1
            };
            panel.Children.Add(new TextBlock { Text = "Тип вклада:", Style = FindResource("BentoTextStyle") as Style });
            panel.Children.Add(typesCombo);

            var currenciesCombo = new ComboBox
            {
                Style = FindResource("ZBankComboBoxStyle") as Style,
                ItemsSource = context.Currencies.ToList(),
                DisplayMemberPath = "Code",
                Margin = new Thickness(0, 10, 0, 0),
                SelectedIndex = -1
            };
            panel.Children.Add(new TextBlock { Text = "Валюта:", Style = FindResource("BentoTextStyle") as Style });
            panel.Children.Add(currenciesCombo);

            var amountBox = new TextBox
            {
                Style = FindResource("ZBankTextBoxStyle") as Style,
                Height = 40,
                Margin = new Thickness(0, 10, 0, 0),
                Text = ""
            };
            panel.Children.Add(new TextBlock { Text = "Сумма:", Style = FindResource("BentoTextStyle") as Style });
            panel.Children.Add(amountBox);

            var submitButton = new Button { Content = "Открыть", Style = FindResource("BentoButtonStyle") as Style };
            submitButton.Click += (s, e) => SubmitNewDeposit(typesCombo, currenciesCombo, amountBox);
            panel.Children.Add(submitButton);

            return panel;
        }

        private void SubmitNewDeposit(ComboBox typesCombo, ComboBox currenciesCombo, TextBox amountBox)
        {
            if (!decimal.TryParse(amountBox.Text, out decimal amount) || typesCombo.SelectedItem == null || currenciesCombo.SelectedItem == null)
            {
                MessageBoxHelper.Show("Заполните все поля корректно!", "Ошибка", "ОК");
                return;
            }

            var depositType = (DepositType)typesCombo.SelectedItem;
            var currency = (Currency)currenciesCombo.SelectedItem;

            var employees = context.Employees.ToList();
            if (!employees.Any())
            {
                MessageBoxHelper.Show("Нет доступных сотрудников для назначения!", "Ошибка", "ОК");
                return;
            }
            var random = new Random();
            var randomEmployee = employees[random.Next(employees.Count)];

            var deposit = new Deposit
            {
                CustomerId = customer.CustomerId,
                EmployeeId = randomEmployee.EmployeeId,
                DepositTypeId = depositType.DepositTypeId,
                CurrencyId = currency.CurrencyId,
                ContractNumber = $"D{DateTime.Now.Ticks}",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(depositType.MinTerm ?? 6),
                Amount = amount
            };

            try
            {
                context.Deposits.Add(deposit);
                context.SaveChanges();
                LoadCustomerData();
                CloseFloatingPanel();
                MessageBoxHelper.Show("Вклад успешно открыт!", "Успех", "ОК");
            }
            catch (DbUpdateException ex)
            {
                context.ChangeTracker.Clear();
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                if (innerMessage.Contains("Сумма вклада меньше минимальной"))
                {
                    MessageBoxHelper.Show($"Сумма вклада ({amount}) меньше минимальной ({depositType.MinAmount}) для типа '{depositType.Name}'!", "Ошибка", "ОК");
                }
                else if (innerMessage.Contains("Дата окончания вклада должна быть больше даты начала"))
                {
                    MessageBoxHelper.Show("Дата окончания вклада должна быть больше даты начала!", "Ошибка", "ОК");
                }
                else
                {
                    MessageBoxHelper.Show($"Ошибка при сохранении в базе данных: {innerMessage}", "Ошибка", "ОК", canCopy: true);
                }
            }
            catch (Exception ex)
            {
                context.ChangeTracker.Clear();
                MessageBoxHelper.Show($"Неизвестная ошибка: {ex.Message}", "Ошибка", "ОК", canCopy: true);
            }
        }

        private StackPanel CreateNewCreditPanel()
        {
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = "Взять новый кредит", Style = FindResource("BentoTextStyle") as Style, FontSize = 18, FontWeight = FontWeights.Bold });

            var currenciesCombo = new ComboBox
            {
                Style = FindResource("ZBankComboBoxStyle") as Style,
                ItemsSource = context.Currencies.ToList(),
                DisplayMemberPath = "Code",
                Margin = new Thickness(0, 10, 0, 0),
                SelectedIndex = -1
            };
            panel.Children.Add(new TextBlock { Text = "Валюта:", Style = FindResource("BentoTextStyle") as Style });
            panel.Children.Add(currenciesCombo);

            var amountBox = new TextBox
            {
                Style = FindResource("ZBankTextBoxStyle") as Style,
                Height = 40,
                Margin = new Thickness(0, 10, 0, 0),
                Text = ""
            };
            panel.Children.Add(new TextBlock { Text = "Сумма:", Style = FindResource("BentoTextStyle") as Style });
            panel.Children.Add(amountBox);

            var termBox = new TextBox
            {
                Style = FindResource("ZBankTextBoxStyle") as Style,
                Height = 40,
                Margin = new Thickness(0, 10, 0, 0),
                Text = ""
            };
            panel.Children.Add(new TextBlock { Text = "Срок (месяцы):", Style = FindResource("BentoTextStyle") as Style });
            panel.Children.Add(termBox);

            var submitButton = new Button { Content = "Взять", Style = FindResource("BentoButtonStyle") as Style };
            submitButton.Click += (s, e) => SubmitNewCredit(currenciesCombo, amountBox, termBox);
            panel.Children.Add(submitButton);

            return panel;
        }

        private void SubmitNewCredit(ComboBox currenciesCombo, TextBox amountBox, TextBox termBox)
        {
            if (!decimal.TryParse(amountBox.Text, out decimal amount) || !int.TryParse(termBox.Text, out int term) || currenciesCombo.SelectedItem == null)
            {
                MessageBoxHelper.Show("Заполните все поля корректно!", "Ошибка", "ОК");
                return;
            }

            var currency = (Currency)currenciesCombo.SelectedItem;
            var employees = context.Employees.ToList();
            if (!employees.Any())
            {
                MessageBoxHelper.Show("Нет доступных сотрудников для назначения!", "Ошибка", "ОК");
                return;
            }
            var random = new Random();
            var randomEmployee = employees[random.Next(employees.Count)];

            var credit = new Credit
            {
                CustomerId = customer.CustomerId,
                EmployeeId = randomEmployee.EmployeeId,
                CurrencyId = currency.CurrencyId,
                ContractNumber = $"C{DateTime.Now.Ticks}",
                StartDate = DateTime.Today,
                Term = term,
                InterestRate = 10.0m,
                Amount = amount
            };

            try
            {
                context.Credits.Add(credit);
                context.SaveChanges();
                LoadCustomerData();
                CloseFloatingPanel();
                MessageBoxHelper.Show("Кредит успешно взят!", "Успех", "ОК");
            }
            catch (DbUpdateException ex)
            {
                context.ChangeTracker.Clear();
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                if (innerMessage.Contains("Сумма вклада меньше минимальной"))
                {
                    MessageBoxHelper.Show("Ошибка: почему-то сработала проверка вкладов. Проверьте базу данных!", "Ошибка", "ОК", canCopy: true);
                }
                else if (innerMessage.Contains("CHECK constraint"))
                {
                    MessageBoxHelper.Show("Срок кредита должен быть больше 0!", "Ошибка", "ОК");
                }
                else if (innerMessage.Contains("Забаненный клиент не может взять кредит"))
                {
                    MessageBoxHelper.Show("Забаненный клиент не может взять кредит!", "Ошибка", "ОК");
                }
                else if (innerMessage.Contains("Номер контракта кредита совпадает с номером контракта вклада"))
                {
                    MessageBoxHelper.Show("Номер контракта кредита совпадает с номером контракта вклада!", "Ошибка", "ОК");
                }
                else
                {
                    MessageBoxHelper.Show($"Ошибка при сохранении кредита: {innerMessage}", "Ошибка", "ОК", canCopy: true);
                }
            }
            catch (Exception ex)
            {
                context.ChangeTracker.Clear();
                MessageBoxHelper.Show($"Неизвестная ошибка: {ex.Message}", "Ошибка", "ОК", canCopy: true);
            }
        }

        private StackPanel CreateReplenishDepositPanel(int depositId)
        {
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = "Пополнить вклад", Style = FindResource("BentoTextStyle") as Style, FontSize = 18, FontWeight = FontWeights.Bold });

            var amountBox = new TextBox
            {
                Style = FindResource("ZBankTextBoxStyle") as Style,
                Height = 40,
                Margin = new Thickness(0, 10, 0, 0),
                Text = ""
            };
            panel.Children.Add(new TextBlock { Text = "Сумма пополнения:", Style = FindResource("BentoTextStyle") as Style });
            panel.Children.Add(amountBox);

            var submitButton = new Button { Content = "Пополнить", Style = FindResource("BentoButtonStyle") as Style };
            submitButton.Click += (s, e) => SubmitReplenishDeposit(depositId, amountBox);
            panel.Children.Add(submitButton);

            return panel;
        }

        private void SubmitReplenishDeposit(int depositId, TextBox amountBox)
        {
            if (!decimal.TryParse(amountBox.Text, out decimal amount) || amount <= 0)
            {
                MessageBoxHelper.Show("Введите корректную сумму!", "Ошибка", "ОК");
                return;
            }

            var deposit = context.Deposits.FirstOrDefault(d => d.DepositId == depositId);
            if (deposit != null)
            {
                deposit.Amount += amount;
                context.SaveChanges();
                LoadCustomerData();
                CloseFloatingPanel();
                MessageBoxHelper.Show("Вклад успешно пополнен!", "Успех", "ОК");
            }
        }

        private StackPanel CreatePayCreditPanel(int creditId)
        {
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = "Оплатить кредит",
                Style = FindResource("BentoTextStyle") as Style,
                FontSize = 18,
                FontWeight = FontWeights.Bold
            });

            var credit = context.Credits
                .Include(c => c.CreditPayments)
                .FirstOrDefault(c => c.CreditId == creditId);
            if (credit == null) return panel;

            var paidAmount = credit.CreditPayments
                .Where(p => p.IsPaid == true)
                .Sum(p => p.Amount);
            panel.Children.Add(new TextBlock
            {
                Text = $"Уплачено: {paidAmount:N2}",
                Style = FindResource("BentoTextStyle") as Style
            });

            var nextPayment = credit.CreditPayments
                .Where(p => !p.IsPaid == true)
                .OrderBy(p => p.PaymentDate)
                .FirstOrDefault();
            var paymentStatus = new TextBlock
            {
                Style = FindResource("BentoTextStyle") as Style
            };
            if (nextPayment != null)
            {
                var daysUntilDue = (nextPayment.PaymentDate.Value - DateTime.Today).Days;
                paymentStatus.Text = $"Следующий платёж: {nextPayment.Amount:N2} ({nextPayment.PaymentDate:dd.MM.yyyy})";
                paymentStatus.Foreground = daysUntilDue switch
                {
                    < 0 => new SolidColorBrush(Colors.Red),
                    <= 3 => new SolidColorBrush(Colors.Orange),
                    _ => new SolidColorBrush(Colors.White)
                };
            }
            else
            {
                paymentStatus.Text = "Нет предстоящих платежей";
            }
            panel.Children.Add(paymentStatus);

            if (nextPayment != null)
            {
                var submitButton = new Button
                {
                    Content = "Оплатить",
                    Style = FindResource("BentoButtonStyle") as Style
                };
                submitButton.Click += (s, e) => SubmitPayCredit(creditId);
                panel.Children.Add(submitButton);
            }
            else
            {
                panel.Children.Add(new TextBlock
                {
                    Text = "Кредит полностью оплачен",
                    Style = FindResource("BentoTextStyle") as Style,
                    Foreground = new SolidColorBrush(Colors.Green)
                });
            }

            return panel;
        }

        private decimal CalculateAnnuityPayment(decimal amount, decimal interestRate, int term)
        {
            decimal monthlyRate = interestRate / 100 / 12;
            double factor = Math.Pow(1 + (double)monthlyRate, term);
            return amount * monthlyRate * (decimal)factor / ((decimal)factor - 1);
        }

        private void SubmitPayCredit(int creditId)
        {
            try
            {
                var credit = context.Credits
                    .Include(c => c.CreditPayments)
                    .FirstOrDefault(c => c.CreditId == creditId);
                if (credit == null)
                {
                    MessageBoxHelper.Show("Кредит не найден!", "Ошибка", "ОК");
                    return;
                }

                var nextPayment = credit.CreditPayments
                    .Where(p => !p.IsPaid == true)
                    .OrderBy(p => p.PaymentDate)
                    .FirstOrDefault();
                if (nextPayment == null)
                {
                    MessageBoxHelper.Show("Нет предстоящих платежей для оплаты!", "Ошибка", "ОК");
                    return;
                }

                nextPayment.IsPaid = true;
                context.SaveChanges();

                var totalPaid = credit.CreditPayments
                    .Where(p => p.IsPaid == true)
                    .Sum(p => p.Amount);
                var monthlyPayment = CalculateAnnuityPayment((decimal)credit.Amount, (decimal)credit.InterestRate, (int)credit.Term);

                if (totalPaid < credit.Amount + (monthlyPayment * credit.Term))
                {
                    var nextPaymentDate = nextPayment.PaymentDate.Value.AddMonths(1);
                    var remainingPayments = credit.Term - credit.CreditPayments.Count(p => p.IsPaid == true);
                    if (remainingPayments > 0)
                    {
                        var existingNextPayment = credit.CreditPayments
                            .FirstOrDefault(p => !p.IsPaid == true && p.PaymentDate == nextPaymentDate);
                        if (existingNextPayment == null)
                        {
                            context.CreditPayments.Add(new CreditPayment
                            {
                                CreditId = creditId,
                                PaymentDate = nextPaymentDate,
                                Amount = monthlyPayment,
                                IsPaid = false
                            });
                            context.SaveChanges();
                        }
                    }

                    var daysUntilDue = (nextPayment.PaymentDate.Value - DateTime.Today).Days;
                    if (daysUntilDue > 3 && remainingPayments > 1)
                    {
                        var twoMonthsAhead = nextPayment.PaymentDate.Value.AddMonths(2);
                        var existingTwoMonthsPayment = credit.CreditPayments
                            .FirstOrDefault(p => !p.IsPaid == true && p.PaymentDate == twoMonthsAhead);
                        if (existingTwoMonthsPayment == null)
                        {
                            context.CreditPayments.Add(new CreditPayment
                            {
                                CreditId = creditId,
                                PaymentDate = twoMonthsAhead,
                                Amount = monthlyPayment,
                                IsPaid = false
                            });
                            context.SaveChanges();
                        }
                    }
                }

                LoadCustomerData();
                CloseFloatingPanel();
                MessageBoxHelper.Show("Платёж успешно проведён!", "Успех", "ОК");
            }
            catch (Exception ex)
            {
                context.ChangeTracker.Clear();
                MessageBoxHelper.Show($"Ошибка при оплате: {ex.Message}", "Ошибка", "ОК", canCopy: true);
            }
        }

        private void ShowFloatingPanel(StackPanel content)
        {
            if (!isPanelOpen)
            {
                FloatingContent.Children.Clear();
                FloatingContent.Children.Add(content);
                isPanelOpen = true;
                OverlayLayer.Visibility = Visibility.Visible;
                OverlayLayer.IsEnabled = true;
            }
        }

        private void CloseFloatingPanel()
        {
            if (isPanelOpen)
            {
                isPanelOpen = false;
                OverlayLayer.Visibility = Visibility.Collapsed;
                OverlayLayer.IsEnabled = false;
                FloatingContent.Children.Clear();
            }
        }

        private void CloseFloatingPanel_Click(object sender, RoutedEventArgs e)
        {
            CloseFloatingPanel();
        }

        private void DepositItem_Click(object sender, MouseButtonEventArgs e)
        {
            var stackPanel = sender as StackPanel;
            if (stackPanel != null && stackPanel.DataContext is Deposit deposit)
            {
                ShowDetailsPanel(CreateDepositDetailsPanel(deposit));
            }
        }

        private void CreditItem_Click(object sender, MouseButtonEventArgs e)
        {
            var stackPanel = sender as StackPanel;
            if (stackPanel != null && stackPanel.DataContext is object data && data.GetType().GetProperty("Credit")?.GetValue(data) is Credit credit)
            {
                ShowDetailsPanel(CreateCreditDetailsPanel(credit));
            }
        }

        private StackPanel CreateDepositDetailsPanel(Deposit deposit)
        {
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = "Подробности вклада", Style = FindResource("BentoTextStyle") as Style, FontSize = 18, FontWeight = FontWeights.Bold });
            panel.Children.Add(new TextBlock { Text = $"Номер договора: {deposit.ContractNumber}", Style = FindResource("BentoTextStyle") as Style });
            panel.Children.Add(new TextBlock { Text = $"Сумма: {deposit.Amount:N2}", Style = FindResource("BentoTextStyle") as Style });
            panel.Children.Add(new TextBlock { Text = $"Валюта: {deposit.Currency?.Code}", Style = FindResource("BentoTextStyle") as Style });
            panel.Children.Add(new TextBlock { Text = $"Дата окончания: {deposit.EndDate:dd.MM.yyyy}", Style = FindResource("BentoTextStyle") as Style });
            panel.Children.Add(new TextBlock { Text = $"Доход: {deposit.ReturnAmount:N2}", Style = FindResource("BentoTextStyle") as Style });
            return panel;
        }

        private StackPanel CreateCreditDetailsPanel(Credit credit)
        {
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = "Подробности кредита", Style = FindResource("BentoTextStyle") as Style, FontSize = 18, FontWeight = FontWeights.Bold });
            panel.Children.Add(new TextBlock { Text = $"Номер договора: {credit.ContractNumber}", Style = FindResource("BentoTextStyle") as Style });
            panel.Children.Add(new TextBlock { Text = $"Сумма: {credit.Amount:N2}", Style = FindResource("BentoTextStyle") as Style });
            panel.Children.Add(new TextBlock { Text = $"Валюта: {credit.Currency?.Code}", Style = FindResource("BentoTextStyle") as Style });
            panel.Children.Add(new TextBlock { Text = $"Срок: {credit.Term} мес.", Style = FindResource("BentoTextStyle") as Style });
            panel.Children.Add(new TextBlock { Text = $"Процентная ставка: {credit.InterestRate:F2}%", Style = FindResource("BentoTextStyle") as Style });

            var paidAmount = context.CreditPayments
                .Where(p => p.CreditId == credit.CreditId && p.IsPaid == true)
                .Sum(p => p.Amount);
            var paidText = new TextBlock { Text = $"Уплачено: {paidAmount:N2}", Style = FindResource("BentoTextStyle") as Style };
            panel.Children.Add(paidText);

            var nextPayment = context.CreditPayments
                .Where(p => p.CreditId == credit.CreditId && !p.IsPaid == true)
                .OrderBy(p => p.PaymentDate)
                .FirstOrDefault();
            var paymentStatus = new TextBlock { Style = FindResource("BentoTextStyle") as Style };
            if (nextPayment != null)
            {
                var daysUntilDue = (nextPayment.PaymentDate.Value - DateTime.Today).Days;
                paymentStatus.Text = $"Следующий платёж: {nextPayment.Amount:N2} ({nextPayment.PaymentDate:dd.MM.yyyy})";
                paymentStatus.Foreground = daysUntilDue switch
                {
                    < 0 => new SolidColorBrush(Colors.Red),
                    <= 3 => new SolidColorBrush(Colors.Orange),
                    _ => new SolidColorBrush(Colors.White)
                };
            }
            else
            {
                paymentStatus.Text = "Кредит полностью оплачен";
                paymentStatus.Foreground = new SolidColorBrush(Colors.Green);
            }
            panel.Children.Add(paymentStatus);

            return panel;
        }

        private void ShowDetailsPanel(StackPanel content)
        {
            DetailsContent.Children.Clear();
            DetailsContent.Children.Add(content);
            DetailsOverlayLayer.Visibility = Visibility.Visible;
            DetailsOverlayLayer.IsEnabled = true;
        }

        private void CloseDetailsPanel_Click(object sender, RoutedEventArgs e)
        {
            DetailsOverlayLayer.Visibility = Visibility.Collapsed;
            DetailsOverlayLayer.IsEnabled = false;
            DetailsContent.Children.Clear();
        }

        private void EditPhone_Click(object sender, RoutedEventArgs e)
        {
            if (customer.IsBanned)
            {
                MessageBoxHelper.Show("Ваш аккаунт заблокирован. Вы не можете редактировать данные.", "Ошибка", "ОК");
                return;
            }
            ShowEditPanel(CreateEditPhonePanel());
        }

        private void EditEmail_Click(object sender, RoutedEventArgs e)
        {
            if (customer.IsBanned)
            {
                MessageBoxHelper.Show("Ваш аккаунт заблокирован. Вы не можете редактировать данные.", "Ошибка", "ОК");
                return;
            }
            ShowEditPanel(CreateEditEmailPanel());
        }

        private StackPanel CreateEditPhonePanel()
        {
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = "Редактировать телефон", Style = FindResource("BentoTextStyle") as Style, FontSize = 18, FontWeight = FontWeights.Bold });

            var phoneBox = new TextBox
            {
                Style = FindResource("ZBankTextBoxStyle") as Style,
                Height = 40,
                Margin = new Thickness(0, 10, 0, 0),
                Text = PhoneTextBlock.Text.Replace("Телефон: ", "")
            };
            panel.Children.Add(new TextBlock { Text = "Новый номер телефона:", Style = FindResource("BentoTextStyle") as Style });
            panel.Children.Add(phoneBox);

            var submitButton = new Button { Content = "Сохранить", Style = FindResource("BentoButtonStyle") as Style };
            submitButton.Click += (s, e) => SubmitEditPhone(phoneBox);
            panel.Children.Add(submitButton);

            return panel;
        }

        private StackPanel CreateEditEmailPanel()
        {
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = "Редактировать email", Style = FindResource("BentoTextStyle") as Style, FontSize = 18, FontWeight = FontWeights.Bold });

            var emailBox = new TextBox
            {
                Style = FindResource("ZBankTextBoxStyle") as Style,
                Height = 40,
                Margin = new Thickness(0, 10, 0, 0),
                Text = EmailTextBlock.Text.Replace("Email: ", "")
            };
            panel.Children.Add(new TextBlock { Text = "Новый email:", Style = FindResource("BentoTextStyle") as Style });
            panel.Children.Add(emailBox);

            var submitButton = new Button { Content = "Сохранить", Style = FindResource("BentoButtonStyle") as Style };
            submitButton.Click += (s, e) => SubmitEditEmail(emailBox);
            panel.Children.Add(submitButton);

            return panel;
        }

        private void SubmitEditPhone(TextBox phoneBox)
        {
            if (string.IsNullOrWhiteSpace(phoneBox.Text))
            {
                MessageBoxHelper.Show("Введите корректный номер телефона!", "Ошибка", "ОК");
                phoneBox.Text = PhoneTextBlock.Text.Replace("Телефон: ", "");
                return;
            }

            try
            {
                var freshCustomer = context.Customers.Find(customer.CustomerId);
                if (freshCustomer == null)
                {
                    MessageBoxHelper.Show("Клиент не найден в базе данных!", "Ошибка", "ОК");
                    return;
                }

                freshCustomer.PhoneNumber = phoneBox.Text;
                context.Customers.Update(freshCustomer);
                context.SaveChanges();

                customer = freshCustomer;
                LoadCustomerData();
                CloseEditPanel();
                MessageBoxHelper.Show("Телефон успешно обновлён!", "Успех", "ОК");
            }
            catch (DbUpdateException ex)
            {
                context.ChangeTracker.Clear();
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                if (innerMessage.Contains("Номер телефона должен быть в формате"))
                {
                    MessageBoxHelper.Show("Номер телефона должен быть в формате +7XXXXXXXXXX (например, +79991234567)!", "Ошибка", "ОК");
                }
                else
                {
                    MessageBoxHelper.Show($"Ошибка при обновлении телефона: {innerMessage}", "Ошибка", "ОК", canCopy: true);
                }
                phoneBox.Text = PhoneTextBlock.Text.Replace("Телефон: ", "");
            }
            catch (Exception ex)
            {
                context.ChangeTracker.Clear();
                MessageBoxHelper.Show($"Неизвестная ошибка: {ex.Message}", "Ошибка", "ОК", canCopy: true);
                phoneBox.Text = PhoneTextBlock.Text.Replace("Телефон: ", "");
            }
        }

        private void SubmitEditEmail(TextBox emailBox)
        {
            if (string.IsNullOrWhiteSpace(emailBox.Text))
            {
                MessageBoxHelper.Show("Введите корректный email!", "Ошибка", "ОК");
                emailBox.Text = EmailTextBlock.Text.Replace("Email: ", "");
                return;
            }

            try
            {
                var freshCustomer = context.Customers.Find(customer.CustomerId);
                if (freshCustomer == null)
                {
                    MessageBoxHelper.Show("Клиент не найден в базе данных!", "Ошибка", "ОК");
                    return;
                }

                freshCustomer.Email = emailBox.Text;
                context.Customers.Update(freshCustomer);
                context.SaveChanges();

                customer = freshCustomer;
                LoadCustomerData();
                CloseEditPanel();
                MessageBoxHelper.Show("Email успешно обновлён!", "Успех", "ОК");
            }
            catch (DbUpdateException ex)
            {
                context.ChangeTracker.Clear();
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                if (innerMessage.Contains("Email должен содержать"))
                {
                    MessageBoxHelper.Show("Email должен содержать @ и домен (например, user@domain.com)!", "Ошибка", "ОК");
                }
                else
                {
                    MessageBoxHelper.Show($"Ошибка при обновлении email: {innerMessage}", "Ошибка", "ОК", canCopy: true);
                }
                emailBox.Text = EmailTextBlock.Text.Replace("Email: ", "");
            }
            catch (Exception ex)
            {
                context.ChangeTracker.Clear();
                MessageBoxHelper.Show($"Неизвестная ошибка: {ex.Message}", "Ошибка", "ОК", canCopy: true);
                emailBox.Text = EmailTextBlock.Text.Replace("Email: ", "");
            }
        }

        private void ShowEditPanel(StackPanel content)
        {
            EditContent.Children.Clear();
            EditContent.Children.Add(content);
            EditOverlayLayer.Visibility = Visibility.Visible;
            EditOverlayLayer.IsEnabled = true;
        }

        private void CloseEditPanel_Click(object sender, RoutedEventArgs e)
        {
            CloseEditPanel();
        }

        private void ReturnToMain_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow(true).Show();
            Close();
        }

        private void CloseEditPanel()
        {
            EditOverlayLayer.Visibility = Visibility.Collapsed;
            EditOverlayLayer.IsEnabled = false;
            EditContent.Children.Clear();
        }
    }
}