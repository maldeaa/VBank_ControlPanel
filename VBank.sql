use master;
create database VBank;
use VBank;

-- Таблица ролей сотрудников
CREATE TABLE Role (
    RoleID INT PRIMARY KEY IDENTITY(1,1),
    RoleName NVARCHAR(50) UNIQUE,
    Description NVARCHAR(200)
);

-- Таблица сотрудников с привязкой к ролям
CREATE TABLE Employee (
    EmployeeID INT PRIMARY KEY IDENTITY(1,1),
    RoleID INT FOREIGN KEY REFERENCES Role(RoleID),
    FullName NVARCHAR(100),
    Age INT CHECK (Age >= 18),
    PhoneNumber NVARCHAR(20),
    Login NVARCHAR(50) UNIQUE,
    PasswordHash NVARCHAR(100),
    Salary DECIMAL(10,2)
);

-- Таблица клиентов
CREATE TABLE Customer (
    CustomerID INT PRIMARY KEY IDENTITY(1,1),
    FullName NVARCHAR(100),
    PhoneNumber NVARCHAR(20),
    Email NVARCHAR(50),
    PassportData NVARCHAR(50) UNIQUE
);

ALTER TABLE Customer
ADD isBanned BIT NOT NULL DEFAULT 0;

-- Таблица валют
CREATE TABLE Currency (
    CurrencyID INT PRIMARY KEY IDENTITY(1,1),
    Code NVARCHAR(10) UNIQUE, -- RUB, USD, GBP
    Name NVARCHAR(50)
);

-- Таблица типов вкладов
CREATE TABLE DepositType (
    DepositTypeID INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(50),
    MinTerm INT CHECK (MinTerm > 0),
    MinAmount DECIMAL(15,2) CHECK (MinAmount > 0),
    InterestRate DECIMAL(5,2) CHECK (InterestRate > 0)
);

-- Таблица вкладов
CREATE TABLE Deposit (
    DepositID INT PRIMARY KEY IDENTITY(1,1),
    CustomerID INT FOREIGN KEY REFERENCES Customer(CustomerID),
    EmployeeID INT FOREIGN KEY REFERENCES Employee(EmployeeID),
    DepositTypeID INT FOREIGN KEY REFERENCES DepositType(DepositTypeID),
    CurrencyID INT FOREIGN KEY REFERENCES Currency(CurrencyID),
    ContractNumber NVARCHAR(20) UNIQUE,
    StartDate DATE,
    EndDate DATE,
    Amount DECIMAL(15,2),
    ReturnAmount DECIMAL(15,2) DEFAULT 0
);

-- Таблица кредитов
CREATE TABLE Credit (
    CreditID INT PRIMARY KEY IDENTITY(1,1),
    CustomerID INT FOREIGN KEY REFERENCES Customer(CustomerID),
    EmployeeID INT FOREIGN KEY REFERENCES Employee(EmployeeID),
    CurrencyID INT FOREIGN KEY REFERENCES Currency(CurrencyID),
    ContractNumber NVARCHAR(20) UNIQUE,
    StartDate DATE,
    Term INT CHECK (Term > 0),
    InterestRate DECIMAL(5,2),
    Amount DECIMAL(15,2)
);

-- Таблица платежей по кредитам
CREATE TABLE CreditPayment (
    PaymentID INT PRIMARY KEY IDENTITY(1,1),
    CreditID INT FOREIGN KEY REFERENCES Credit(CreditID),
    PaymentDate DATE,
    Amount DECIMAL(15,2),
    IsPaid BIT DEFAULT 0
);

-- Таблица логов операций (для аудита)
CREATE TABLE OperationLog (
    LogID INT PRIMARY KEY IDENTITY(1,1),
    EmployeeID INT FOREIGN KEY REFERENCES Employee(EmployeeID),
    OperationType NVARCHAR(50), -- "Создание вклада", "Выдача кредита" и т.д.
    OperationDate DATETIME DEFAULT GETDATE(),
    Details NVARCHAR(200)
);

-- Триггер для проверки дат и минимальной суммы вклада
CREATE TRIGGER CheckDepositConstraints
ON Deposit
AFTER INSERT, UPDATE
AS
BEGIN
    IF EXISTS (SELECT 1 FROM Inserted i WHERE i.EndDate <= i.StartDate)
    BEGIN
        RAISERROR ('Дата окончания вклада должна быть больше даты начала', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    IF EXISTS (
        SELECT 1 
        FROM Inserted i
        JOIN DepositType dt ON i.DepositTypeID = dt.DepositTypeID
        WHERE i.Amount < dt.MinAmount
    )
    BEGIN
        RAISERROR ('Сумма вклада меньше минимальной', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;

-- Триггер для начисления процентов при создании вклада
CREATE TRIGGER UpdateReturnAmount
ON Deposit
AFTER INSERT
AS
BEGIN
    UPDATE d
    SET ReturnAmount = i.Amount + (i.Amount * dt.InterestRate / 100 / 12)
    FROM Deposit d
    JOIN Inserted i ON d.DepositID = i.DepositID
    JOIN DepositType dt ON d.DepositTypeID = dt.DepositTypeID;
END;

-- Процедура для ежемесячного начисления процентов
CREATE PROCEDURE UpdateMonthlyDepositInterest
AS
BEGIN
    UPDATE Deposit
    SET ReturnAmount = ReturnAmount + (Amount * dt.InterestRate / 100 / 12)
    FROM Deposit d
    JOIN DepositType dt ON d.DepositTypeID = dt.DepositTypeID
    WHERE GETDATE() BETWEEN d.StartDate AND d.EndDate;
END;

CREATE TRIGGER LogDepositOperations
ON Deposit
AFTER INSERT, UPDATE
AS
BEGIN
    INSERT INTO OperationLog (EmployeeID, OperationType, OperationDate, Details)
    SELECT 
        i.EmployeeID,
        CASE WHEN EXISTS (SELECT 1 FROM Deleted) THEN 'Обновление вклада' ELSE 'Создание вклада' END,
        GETDATE(),
        'Вклад #' + i.ContractNumber + ', Сумма: ' + CAST(i.Amount AS NVARCHAR(20))
    FROM Inserted i
    LEFT JOIN Deleted d ON i.DepositID = d.DepositID;
END;

-- Триггер для логирования добавления/обновления кредитов
CREATE TRIGGER LogCreditOperations
ON Credit
AFTER INSERT, UPDATE
AS
BEGIN
    INSERT INTO OperationLog (EmployeeID, OperationType, OperationDate, Details)
    SELECT 
        i.EmployeeID,
        CASE WHEN EXISTS (SELECT 1 FROM Deleted) THEN 'Обновление кредита' ELSE 'Выдача кредита' END,
        GETDATE(),
        'Кредит #' + i.ContractNumber + ', Сумма: ' + CAST(i.Amount AS NVARCHAR(20))
    FROM Inserted i
    LEFT JOIN Deleted d ON i.CreditID = d.CreditID;
END;

-- Триггер для логирования удаления вкладов
CREATE TRIGGER LogDepositDeletion
ON Deposit
AFTER DELETE
AS
BEGIN
    INSERT INTO OperationLog (EmployeeID, OperationType, OperationDate, Details)
    SELECT 
        d.EmployeeID,
        'Удаление вклада',
        GETDATE(),
        'Вклад #' + d.ContractNumber + ', Сумма: ' + CAST(d.Amount AS NVARCHAR(20))
    FROM Deleted d;
END;

-- Триггер для логирования удаления кредитов
CREATE TRIGGER LogCreditDeletion
ON Credit
AFTER DELETE
AS
BEGIN
    INSERT INTO OperationLog (EmployeeID, OperationType, OperationDate, Details)
    SELECT 
        d.EmployeeID,
        'Удаление кредита',
        GETDATE(),
        'Кредит #' + d.ContractNumber + ', Сумма: ' + CAST(d.Amount AS NVARCHAR(20))
    FROM Deleted d;
END;

CREATE TRIGGER CheckPhoneNumberFormat
ON Customer
AFTER INSERT, UPDATE
AS
BEGIN
    IF EXISTS (
        SELECT 1 
        FROM Inserted i
        WHERE i.PhoneNumber NOT LIKE '+7[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
        OR LEN(i.PhoneNumber) != 12
    )
    BEGIN
        RAISERROR ('Номер телефона должен быть в формате +7XXXXXXXXXX (12 символов, начинается с +7)', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;

CREATE TRIGGER CheckEmailFormat
ON Customer
AFTER INSERT, UPDATE
AS
BEGIN
    IF EXISTS (
        SELECT 1 
        FROM Inserted i
        WHERE i.Email NOT LIKE '%@%.%'
        OR i.Email LIKE '%[^a-zA-Z0-9@._-]%'
        OR LEN(i.Email) < 5
    )
    BEGIN
        RAISERROR ('Email должен содержать @ и домен (например, user@domain.com), допустимы только буквы, цифры, @, ., _, -', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;

-- Заполнение данными
INSERT INTO Role (RoleName, Description)
VALUES 
    ('Админ', 'Полный доступ ко всем операциям'),
    ('Менеджер', 'Работа с клиентами, вкладами и кредитами'),
    ('Кассир', 'Прием платежей и выдача средств');

INSERT INTO Employee (RoleID, FullName, Age, PhoneNumber, Login, PasswordHash, Salary)
VALUES 
    (1, 'Иванов Иван', 30, '+79991234567', 'admin', '$2y$10$LO4zOoYM5N9Q3wsfQnujqOCZLCfc4loSBoqCIxXkRiEZU0goiUmX.', 70000.00), -- Админ
    (2, 'Петров Петр', 35, '+79997654321', 'manager', '$2y$10$mSBNi68LJTLNWosAL5WJ8OurodT82GVkng.PDDq8gMSzo.0s7/WHm', 50000.00), -- Менеджер
    (3, 'Сидорова Анна', 28, '+79993456789', 'cashier', '$2y$10$T6kqVCh8UINYEUtHKZzKEOvHMvyMDuPT9p3nOo52/gm/KyiWZvL7K', 40000.00); -- Кассир

INSERT INTO Customer (FullName, PhoneNumber, Email, PassportData)
VALUES 
    ('Козлов Алексей', '+79991112233', 'kozlov@mail.ru', '1111 222222'),
    ('Смирнова Ольга', '+79993334455', 'smirnova@mail.ru', '3333 444444');

INSERT INTO Currency (Code, Name)
VALUES 
    ('RUB', 'Российский рубль'),
    ('USD', 'Доллар США');

INSERT INTO DepositType (Name, MinTerm, MinAmount, InterestRate)
VALUES 
    ('Срочный', 6, 10000.00, 5.5),
    ('Накопительный', 12, 5000.00, 6.0);

INSERT INTO Deposit (CustomerID, EmployeeID, DepositTypeID, CurrencyID, ContractNumber, StartDate, EndDate, Amount)
VALUES 
    (1, 2, 1, 1, 'D001', '2024-09-01', '2025-09-01', 50000.00),
    (2, 2, 2, 2, 'D002', '2024-06-01', '2025-06-01', 10000.00);

INSERT INTO Credit (CustomerID, EmployeeID, CurrencyID, ContractNumber, StartDate, Term, InterestRate, Amount)
VALUES 
    (1, 2, 1, 'C001', '2024-12-01', 12, 10.0, 100000.00);

INSERT INTO CreditPayment (CreditID, PaymentDate, Amount, IsPaid)
VALUES 
    (1, '2025-01-01', 9000.00, 1),
    (1, '2025-02-01', 9000.00, 0);

-- АПДЕЙТ ОТ 12 АПРЕЛЯ --

-- Триггер для валидации номера телефона сотрудников
CREATE TRIGGER CheckEmployeePhoneNumberFormat
ON Employee
AFTER INSERT, UPDATE
AS
BEGIN
    IF EXISTS (
        SELECT 1 
        FROM Inserted i
        WHERE i.PhoneNumber NOT LIKE '+7[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
        OR LEN(i.PhoneNumber) != 12
    )
    BEGIN
        RAISERROR ('Номер телефона сотрудника должен быть в формате +7XXXXXXXXXX (12 символов, начинается с +7)', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;

-- Триггер для валидации паспортных данных клиентов
CREATE TRIGGER CheckPassportDataFormat
ON Customer
AFTER INSERT, UPDATE
AS
BEGIN
    IF EXISTS (
        SELECT 1 
        FROM Inserted i
        WHERE i.PassportData NOT LIKE '[0-9][0-9][0-9][0-9] [0-9][0-9][0-9][0-9][0-9][0-9]'
        OR LEN(i.PassportData) != 11
    )
    BEGIN
        RAISERROR ('Паспортные данные должны быть в формате XXXX YYYYYY (4 цифры серии, пробел, 6 цифр номера)', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;

-- Триггер для проверки возраста сотрудников
CREATE TRIGGER CheckEmployeeAge
ON Employee
AFTER INSERT, UPDATE
AS
BEGIN
    IF EXISTS (
        SELECT 1 
        FROM Inserted i
        WHERE i.Age > 100
    )
    BEGIN
        RAISERROR ('Возраст сотрудника не может превышать 100 лет', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;

-- Триггер для проверки положительной суммы платежей по кредитам
CREATE TRIGGER CheckCreditPaymentAmount
ON CreditPayment
AFTER INSERT, UPDATE
AS
BEGIN
    IF EXISTS (
        SELECT 1 
        FROM Inserted i
        WHERE i.Amount <= 0
    )
    BEGIN
        RAISERROR ('Сумма платежа по кредиту должна быть больше 0', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;

-- Триггер для проверки дат платежей по кредитам
CREATE TRIGGER CheckCreditPaymentDate
ON CreditPayment
AFTER INSERT, UPDATE
AS
BEGIN
    IF EXISTS (
        SELECT 1 
        FROM Inserted i
        JOIN Credit c ON i.CreditID = c.CreditID
        WHERE i.PaymentDate < c.StartDate
    )
    BEGIN
        RAISERROR ('Дата платежа не может быть раньше даты начала кредита', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;

-- Триггер для проверки формата логина сотрудников
CREATE TRIGGER CheckEmployeeLoginFormat
ON Employee
AFTER INSERT, UPDATE
AS
BEGIN
    IF EXISTS (
        SELECT 1 
        FROM Inserted i
        WHERE i.Login LIKE '%[^a-zA-Z0-9_-]%'
        OR LEN(i.Login) < 4
    )
    BEGIN
        RAISERROR ('Логин должен содержать только буквы, цифры, _ или -, и быть не короче 4 символов', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;

-- Триггер для проверки, что клиент не забанен при создании вкладов
CREATE TRIGGER CheckCustomerBannedForDeposit
ON Deposit
AFTER INSERT
AS
BEGIN
    IF EXISTS (
        SELECT 1 
        FROM Inserted i
        JOIN Customer c ON i.CustomerID = c.CustomerID
        WHERE c.isBanned = 1
    )
    BEGIN
        RAISERROR ('Забаненный клиент не может открыть вклад', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;

-- Триггер для проверки, что клиент не забанен при создании кредитов
CREATE TRIGGER CheckCustomerBannedForCredit
ON Credit
AFTER INSERT
AS
BEGIN
    IF EXISTS (
        SELECT 1 
        FROM Inserted i
        JOIN Customer c ON i.CustomerID = c.CustomerID
        WHERE c.isBanned = 1
    )
    BEGIN
        RAISERROR ('Забаненный клиент не может взять кредит', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;

-- Триггер для предотвращения удаления клиентов с активными вкладами или кредитами
CREATE TRIGGER PreventCustomerDeletion
ON Customer
INSTEAD OF DELETE
AS
BEGIN
    IF EXISTS (
        SELECT 1 
        FROM Deleted d
        JOIN Deposit dep ON d.CustomerID = dep.CustomerID
        WHERE dep.EndDate > GETDATE()
    )
    OR EXISTS (
        SELECT 1 
        FROM Deleted d
        JOIN Credit c ON d.CustomerID = c.CustomerID
        WHERE DATEADD(MONTH, c.Term, c.StartDate) > GETDATE()
    )
    BEGIN
        RAISERROR ('Нельзя удалить клиента с активными вкладами или кредитами', 16, 1);
        RETURN;
    END

    -- Выполняем удаление, если нет активных вкладов или кредитов
    DELETE FROM Customer
    WHERE CustomerID IN (SELECT CustomerID FROM Deleted);
END;

-- Триггер для проверки уникальности номера контракта между вкладами и кредитами
CREATE TRIGGER CheckUniqueContractNumber
ON Deposit
AFTER INSERT, UPDATE
AS
BEGIN
    IF EXISTS (
        SELECT 1 
        FROM Inserted i
        JOIN Credit c ON i.ContractNumber = c.ContractNumber
    )
    BEGIN
        RAISERROR ('Номер контракта вклада совпадает с номером контракта кредита', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;

CREATE TRIGGER CheckUniqueContractNumberCredit
ON Credit
AFTER INSERT, UPDATE
AS
BEGIN
    IF EXISTS (
        SELECT 1 
        FROM Inserted i
        JOIN Deposit d ON i.ContractNumber = d.ContractNumber
    )
    BEGIN
        RAISERROR ('Номер контракта кредита совпадает с номером контракта вклада', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;

-- Триггер для проверки формата кода валюты
CREATE TRIGGER CheckCurrencyCodeFormat
ON Currency
AFTER INSERT, UPDATE
AS
BEGIN
    IF EXISTS (
        SELECT 1 
        FROM Inserted i
        WHERE i.Code NOT LIKE '[A-Z][A-Z][A-Z]'
        OR LEN(i.Code) != 3
    )
    BEGIN
        RAISERROR ('Код валюты должен состоять из 3 букв (например, RUB, USD)', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;

-- Процедура для ежемесячного обновления платежей по кредитам
CREATE OR ALTER PROCEDURE UpdateMonthlyCreditPayments
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CurrentDate DATE = GETDATE();
    DECLARE @NextMonth DATE = DATEADD(MONTH, 1, @CurrentDate);
    DECLARE @TwoMonthsAhead DATE = DATEADD(MONTH, 2, @CurrentDate);

    -- Курсор для обработки активных кредитов
    DECLARE @CreditID INT, @Amount DECIMAL(15,2), @InterestRate DECIMAL(5,2), @StartDate DATE, @Term INT;
    DECLARE credit_cursor CURSOR FOR
        SELECT 
            c.CreditID, 
            c.Amount, 
            c.InterestRate, 
            c.StartDate, 
            c.Term
        FROM Credit c
        WHERE 
            (SELECT SUM(cp.Amount) 
             FROM CreditPayment cp 
             WHERE cp.CreditID = c.CreditID AND cp.IsPaid = 1) < c.Amount
            AND DATEADD(MONTH, c.Term, c.StartDate) >= @CurrentDate;

    OPEN credit_cursor;
    FETCH NEXT FROM credit_cursor INTO @CreditID, @Amount, @InterestRate, @StartDate, @Term;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        DECLARE @MonthlyPayment DECIMAL(15,2);
        -- Рассчитываем аннуитетный платеж
        SET @MonthlyPayment = @Amount * (@InterestRate / 100 / 12) * POWER(1 + (@InterestRate / 100 / 12), @Term) /
                             (POWER(1 + (@InterestRate / 100 / 12), @Term) - 1);

        -- Проверяем, есть ли неоплаченный платеж на следующий месяц
        DECLARE @ExistingPaymentID INT, @ExistingPaymentAmount DECIMAL(15,2), @ExistingPaymentDate DATE;
        SELECT TOP 1 
            @ExistingPaymentID = cp.PaymentID,
            @ExistingPaymentAmount = cp.Amount,
            @ExistingPaymentDate = cp.PaymentDate
        FROM CreditPayment cp
        WHERE 
            cp.CreditID = @CreditID 
            AND cp.IsPaid = 0 
            AND YEAR(cp.PaymentDate) = YEAR(@NextMonth)
            AND MONTH(cp.PaymentDate) = MONTH(@NextMonth);

        -- Если платежа нет или сумма недостаточна, добавляем новый
        IF @ExistingPaymentID IS NULL OR @ExistingPaymentAmount < @MonthlyPayment
        BEGIN
            IF @ExistingPaymentID IS NOT NULL
            BEGIN
                -- Обновляем существующий платеж
                UPDATE CreditPayment
                SET Amount = @MonthlyPayment
                WHERE PaymentID = @ExistingPaymentID;
            END
            ELSE
            BEGIN
                -- Проверяем, остались ли платежи
                DECLARE @PaidPayments INT;
                SELECT @PaidPayments = COUNT(*) 
                FROM CreditPayment 
                WHERE CreditID = @CreditID AND IsPaid = 1;

                IF @PaidPayments < @Term
                BEGIN
                    INSERT INTO CreditPayment (CreditID, PaymentDate, Amount, IsPaid)
                    VALUES (@CreditID, @NextMonth, @MonthlyPayment, 0);
                END
            END
        END

        -- Проверяем раннюю оплату
        DECLARE @CurrentPaymentID INT, @CurrentPaymentDate DATE, @DaysUntilDue INT;
        SELECT TOP 1 
            @CurrentPaymentID = cp.PaymentID,
            @CurrentPaymentDate = cp.PaymentDate
        FROM CreditPayment cp
        WHERE 
            cp.CreditID = @CreditID 
            AND cp.IsPaid = 1 
            AND YEAR(cp.PaymentDate) = YEAR(@CurrentDate)
            AND MONTH(cp.PaymentDate) = MONTH(@CurrentDate)
        ORDER BY cp.PaymentDate DESC;

        IF @CurrentPaymentID IS NOT NULL
        BEGIN
            SET @DaysUntilDue = DATEDIFF(DAY, @CurrentDate, @CurrentPaymentDate);
            IF @DaysUntilDue > 3
            BEGIN
                -- Переиспользуем @PaidPayments вместо повторного объявления
                IF @PaidPayments < @Term - 1
                BEGIN
                    DECLARE @TwoMonthsPaymentID INT;
                    SELECT TOP 1 
                        @TwoMonthsPaymentID = cp.PaymentID
                    FROM CreditPayment cp
                    WHERE 
                        cp.CreditID = @CreditID 
                        AND cp.IsPaid = 0 
                        AND YEAR(cp.PaymentDate) = YEAR(@TwoMonthsAhead)
                        AND MONTH(cp.PaymentDate) = MONTH(@TwoMonthsAhead);

                    IF @TwoMonthsPaymentID IS NULL
                    BEGIN
                        INSERT INTO CreditPayment (CreditID, PaymentDate, Amount, IsPaid)
                        VALUES (@CreditID, @TwoMonthsAhead, @MonthlyPayment, 0);
                    END
                END
            END
        END

        FETCH NEXT FROM credit_cursor INTO @CreditID, @Amount, @InterestRate, @StartDate, @Term;
    END

    CLOSE credit_cursor;
    DEALLOCATE credit_cursor;
END;

-- Триггер для автоматического добавления первого платежа при создании кредита
CREATE OR ALTER TRIGGER AddFirstCreditPayment
ON Credit
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO CreditPayment (CreditID, PaymentDate, Amount, IsPaid)
    SELECT 
        i.CreditID,
        DATEADD(MONTH, 1, i.StartDate),
        i.Amount * (i.InterestRate / 100 / 12) * POWER(1 + (i.InterestRate / 100 / 12), i.Term) /
        (POWER(1 + (i.InterestRate / 100 / 12), i.Term) - 1),
        0
    FROM Inserted i;
END;

-- Триггер для логирования оплаты кредитов
CREATE TRIGGER LogCreditPayment
ON CreditPayment
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO OperationLog (EmployeeID, OperationType, OperationDate, Details)
    SELECT 
        c.EmployeeID,
        'Оплата кредита',
        GETDATE(),
        'Кредит #' + c.ContractNumber + ', Платеж: ' + CAST(i.Amount AS NVARCHAR(20))
    FROM Inserted i
    JOIN Credit c ON i.CreditID = c.CreditID
    WHERE i.IsPaid = 1 AND EXISTS (
        SELECT 1 FROM Deleted d 
        WHERE d.PaymentID = i.PaymentID AND d.IsPaid = 0
    );
END;

CREATE TRIGGER CheckUniqueCustomerPhonePassport
ON Customer
AFTER INSERT, UPDATE
AS
BEGIN
    -- Проверка на уникальность номера телефона среди клиентов
    IF EXISTS (
        SELECT i.PhoneNumber 
        FROM Inserted i
        JOIN Customer c ON i.PhoneNumber = c.PhoneNumber AND i.CustomerID <> c.CustomerID
    )
    BEGIN
        RAISERROR('Номер телефона уже используется другим клиентом', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    -- Проверка на уникальность паспортных данных
    IF EXISTS (
        SELECT i.PassportData 
        FROM Inserted i
        JOIN Customer c ON i.PassportData = c.PassportData AND i.CustomerID <> c.CustomerID
    )
    BEGIN
        RAISERROR('Паспортные данные уже зарегистрированы у другого клиента', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    -- Проверка на уникальность Email
    IF EXISTS (
        SELECT i.Email 
        FROM Inserted i
        JOIN Customer c ON i.Email = c.Email AND i.CustomerID <> c.CustomerID
    )
    BEGIN
        RAISERROR('Email уже используется другим клиентом', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;


CREATE TRIGGER CheckUniqueEmployeePhoneNumber
ON Employee
AFTER INSERT, UPDATE
AS
BEGIN
    -- Проверка на совпадение номеров сотрудников
    IF EXISTS (
        SELECT i.PhoneNumber
        FROM Inserted i
        JOIN Employee e ON i.PhoneNumber = e.PhoneNumber AND i.EmployeeID <> e.EmployeeID
    )
    BEGIN
        RAISERROR('Номер телефона уже используется другим сотрудником', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    -- Проверка на совпадение с номером клиента
    IF EXISTS (
        SELECT i.PhoneNumber
        FROM Inserted i
        JOIN Customer c ON i.PhoneNumber = c.PhoneNumber
    )
    BEGIN
        RAISERROR('Номер телефона сотрудника совпадает с номером клиента', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;

CREATE TRIGGER CheckCustomerPhoneNotEmployee
ON Customer
AFTER INSERT, UPDATE
AS
BEGIN
    IF EXISTS (
        SELECT i.PhoneNumber
        FROM Inserted i
        JOIN Employee e ON i.PhoneNumber = e.PhoneNumber
    )
    BEGIN
        RAISERROR('Недопустимый номер для клиента. Пожалуйста, свяжитесь с банком', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;


