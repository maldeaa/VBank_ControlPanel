use master;
create database VBank;
use VBank;

-- ������� ����� �����������
CREATE TABLE Role (
    RoleID INT PRIMARY KEY IDENTITY(1,1),
    RoleName NVARCHAR(50) UNIQUE,
    Description NVARCHAR(200)
);

-- ������� ����������� � ��������� � �����
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

-- ������� ��������
CREATE TABLE Customer (
    CustomerID INT PRIMARY KEY IDENTITY(1,1),
    FullName NVARCHAR(100),
    PhoneNumber NVARCHAR(20),
    Email NVARCHAR(50),
    PassportData NVARCHAR(50) UNIQUE
);

ALTER TABLE Customer
ADD isBanned BIT NOT NULL DEFAULT 0;

-- ������� �����
CREATE TABLE Currency (
    CurrencyID INT PRIMARY KEY IDENTITY(1,1),
    Code NVARCHAR(10) UNIQUE, -- RUB, USD, GBP
    Name NVARCHAR(50)
);

-- ������� ����� �������
CREATE TABLE DepositType (
    DepositTypeID INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(50),
    MinTerm INT CHECK (MinTerm > 0),
    MinAmount DECIMAL(15,2) CHECK (MinAmount > 0),
    InterestRate DECIMAL(5,2) CHECK (InterestRate > 0)
);

-- ������� �������
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

-- ������� ��������
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

-- ������� �������� �� ��������
CREATE TABLE CreditPayment (
    PaymentID INT PRIMARY KEY IDENTITY(1,1),
    CreditID INT FOREIGN KEY REFERENCES Credit(CreditID),
    PaymentDate DATE,
    Amount DECIMAL(15,2),
    IsPaid BIT DEFAULT 0
);

-- ������� ����� �������� (��� ������)
CREATE TABLE OperationLog (
    LogID INT PRIMARY KEY IDENTITY(1,1),
    EmployeeID INT FOREIGN KEY REFERENCES Employee(EmployeeID),
    OperationType NVARCHAR(50), -- "�������� ������", "������ �������" � �.�.
    OperationDate DATETIME DEFAULT GETDATE(),
    Details NVARCHAR(200)
);

-- ������� ��� �������� ��� � ����������� ����� ������
CREATE TRIGGER CheckDepositConstraints
ON Deposit
AFTER INSERT, UPDATE
AS
BEGIN
    IF EXISTS (SELECT 1 FROM Inserted i WHERE i.EndDate <= i.StartDate)
    BEGIN
        RAISERROR ('���� ��������� ������ ������ ���� ������ ���� ������', 16, 1);
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
        RAISERROR ('����� ������ ������ �����������', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;

-- ������� ��� ���������� ��������� ��� �������� ������
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

-- ��������� ��� ������������ ���������� ���������
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
        CASE WHEN EXISTS (SELECT 1 FROM Deleted) THEN '���������� ������' ELSE '�������� ������' END,
        GETDATE(),
        '����� #' + i.ContractNumber + ', �����: ' + CAST(i.Amount AS NVARCHAR(20))
    FROM Inserted i
    LEFT JOIN Deleted d ON i.DepositID = d.DepositID;
END;

-- ������� ��� ����������� ����������/���������� ��������
CREATE TRIGGER LogCreditOperations
ON Credit
AFTER INSERT, UPDATE
AS
BEGIN
    INSERT INTO OperationLog (EmployeeID, OperationType, OperationDate, Details)
    SELECT 
        i.EmployeeID,
        CASE WHEN EXISTS (SELECT 1 FROM Deleted) THEN '���������� �������' ELSE '������ �������' END,
        GETDATE(),
        '������ #' + i.ContractNumber + ', �����: ' + CAST(i.Amount AS NVARCHAR(20))
    FROM Inserted i
    LEFT JOIN Deleted d ON i.CreditID = d.CreditID;
END;

-- ������� ��� ����������� �������� �������
CREATE TRIGGER LogDepositDeletion
ON Deposit
AFTER DELETE
AS
BEGIN
    INSERT INTO OperationLog (EmployeeID, OperationType, OperationDate, Details)
    SELECT 
        d.EmployeeID,
        '�������� ������',
        GETDATE(),
        '����� #' + d.ContractNumber + ', �����: ' + CAST(d.Amount AS NVARCHAR(20))
    FROM Deleted d;
END;

-- ������� ��� ����������� �������� ��������
CREATE TRIGGER LogCreditDeletion
ON Credit
AFTER DELETE
AS
BEGIN
    INSERT INTO OperationLog (EmployeeID, OperationType, OperationDate, Details)
    SELECT 
        d.EmployeeID,
        '�������� �������',
        GETDATE(),
        '������ #' + d.ContractNumber + ', �����: ' + CAST(d.Amount AS NVARCHAR(20))
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
        RAISERROR ('����� �������� ������ ���� � ������� +7XXXXXXXXXX (12 ��������, ���������� � +7)', 16, 1);
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
        RAISERROR ('Email ������ ��������� @ � ����� (��������, user@domain.com), ��������� ������ �����, �����, @, ., _, -', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;

-- ���������� �������
INSERT INTO Role (RoleName, Description)
VALUES 
    ('�����', '������ ������ �� ���� ���������'),
    ('��������', '������ � ���������, �������� � ���������'),
    ('������', '����� �������� � ������ �������');

INSERT INTO Employee (RoleID, FullName, Age, PhoneNumber, Login, PasswordHash, Salary)
VALUES 
    (1, '������ ����', 30, '+79991234567', 'admin', '$2y$10$LO4zOoYM5N9Q3wsfQnujqOCZLCfc4loSBoqCIxXkRiEZU0goiUmX.', 70000.00), -- �����
    (2, '������ ����', 35, '+79997654321', 'manager', '$2y$10$mSBNi68LJTLNWosAL5WJ8OurodT82GVkng.PDDq8gMSzo.0s7/WHm', 50000.00), -- ��������
    (3, '�������� ����', 28, '+79993456789', 'cashier', '$2y$10$T6kqVCh8UINYEUtHKZzKEOvHMvyMDuPT9p3nOo52/gm/KyiWZvL7K', 40000.00); -- ������

INSERT INTO Customer (FullName, PhoneNumber, Email, PassportData)
VALUES 
    ('������ �������', '+79991112233', 'kozlov@mail.ru', '1111 222222'),
    ('�������� �����', '+79993334455', 'smirnova@mail.ru', '3333 444444');

INSERT INTO Currency (Code, Name)
VALUES 
    ('RUB', '���������� �����'),
    ('USD', '������ ���');

INSERT INTO DepositType (Name, MinTerm, MinAmount, InterestRate)
VALUES 
    ('�������', 6, 10000.00, 5.5),
    ('�������������', 12, 5000.00, 6.0);

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

-- ������ �� 12 ������ --

-- ������� ��� ��������� ������ �������� �����������
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
        RAISERROR ('����� �������� ���������� ������ ���� � ������� +7XXXXXXXXXX (12 ��������, ���������� � +7)', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;

-- ������� ��� ��������� ���������� ������ ��������
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
        RAISERROR ('���������� ������ ������ ���� � ������� XXXX YYYYYY (4 ����� �����, ������, 6 ���� ������)', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;

-- ������� ��� �������� �������� �����������
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
        RAISERROR ('������� ���������� �� ����� ��������� 100 ���', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;

-- ������� ��� �������� ������������� ����� �������� �� ��������
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
        RAISERROR ('����� ������� �� ������� ������ ���� ������ 0', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;

-- ������� ��� �������� ��� �������� �� ��������
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
        RAISERROR ('���� ������� �� ����� ���� ������ ���� ������ �������', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;

-- ������� ��� �������� ������� ������ �����������
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
        RAISERROR ('����� ������ ��������� ������ �����, �����, _ ��� -, � ���� �� ������ 4 ��������', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;

-- ������� ��� ��������, ��� ������ �� ������� ��� �������� �������
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
        RAISERROR ('���������� ������ �� ����� ������� �����', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;

-- ������� ��� ��������, ��� ������ �� ������� ��� �������� ��������
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
        RAISERROR ('���������� ������ �� ����� ����� ������', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;

-- ������� ��� �������������� �������� �������� � ��������� �������� ��� ���������
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
        RAISERROR ('������ ������� ������� � ��������� �������� ��� ���������', 16, 1);
        RETURN;
    END

    -- ��������� ��������, ���� ��� �������� ������� ��� ��������
    DELETE FROM Customer
    WHERE CustomerID IN (SELECT CustomerID FROM Deleted);
END;

-- ������� ��� �������� ������������ ������ ��������� ����� �������� � ���������
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
        RAISERROR ('����� ��������� ������ ��������� � ������� ��������� �������', 16, 1);
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
        RAISERROR ('����� ��������� ������� ��������� � ������� ��������� ������', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;

-- ������� ��� �������� ������� ���� ������
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
        RAISERROR ('��� ������ ������ �������� �� 3 ���� (��������, RUB, USD)', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;

-- ��������� ��� ������������ ���������� �������� �� ��������
CREATE OR ALTER PROCEDURE UpdateMonthlyCreditPayments
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CurrentDate DATE = GETDATE();
    DECLARE @NextMonth DATE = DATEADD(MONTH, 1, @CurrentDate);
    DECLARE @TwoMonthsAhead DATE = DATEADD(MONTH, 2, @CurrentDate);

    -- ������ ��� ��������� �������� ��������
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
        -- ������������ ����������� ������
        SET @MonthlyPayment = @Amount * (@InterestRate / 100 / 12) * POWER(1 + (@InterestRate / 100 / 12), @Term) /
                             (POWER(1 + (@InterestRate / 100 / 12), @Term) - 1);

        -- ���������, ���� �� ������������ ������ �� ��������� �����
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

        -- ���� ������� ��� ��� ����� ������������, ��������� �����
        IF @ExistingPaymentID IS NULL OR @ExistingPaymentAmount < @MonthlyPayment
        BEGIN
            IF @ExistingPaymentID IS NOT NULL
            BEGIN
                -- ��������� ������������ ������
                UPDATE CreditPayment
                SET Amount = @MonthlyPayment
                WHERE PaymentID = @ExistingPaymentID;
            END
            ELSE
            BEGIN
                -- ���������, �������� �� �������
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

        -- ��������� ������ ������
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
                -- �������������� @PaidPayments ������ ���������� ����������
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

-- ������� ��� ��������������� ���������� ������� ������� ��� �������� �������
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

-- ������� ��� ����������� ������ ��������
CREATE TRIGGER LogCreditPayment
ON CreditPayment
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO OperationLog (EmployeeID, OperationType, OperationDate, Details)
    SELECT 
        c.EmployeeID,
        '������ �������',
        GETDATE(),
        '������ #' + c.ContractNumber + ', ������: ' + CAST(i.Amount AS NVARCHAR(20))
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
    -- �������� �� ������������ ������ �������� ����� ��������
    IF EXISTS (
        SELECT i.PhoneNumber 
        FROM Inserted i
        JOIN Customer c ON i.PhoneNumber = c.PhoneNumber AND i.CustomerID <> c.CustomerID
    )
    BEGIN
        RAISERROR('����� �������� ��� ������������ ������ ��������', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    -- �������� �� ������������ ���������� ������
    IF EXISTS (
        SELECT i.PassportData 
        FROM Inserted i
        JOIN Customer c ON i.PassportData = c.PassportData AND i.CustomerID <> c.CustomerID
    )
    BEGIN
        RAISERROR('���������� ������ ��� ���������������� � ������� �������', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    -- �������� �� ������������ Email
    IF EXISTS (
        SELECT i.Email 
        FROM Inserted i
        JOIN Customer c ON i.Email = c.Email AND i.CustomerID <> c.CustomerID
    )
    BEGIN
        RAISERROR('Email ��� ������������ ������ ��������', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;


CREATE TRIGGER CheckUniqueEmployeePhoneNumber
ON Employee
AFTER INSERT, UPDATE
AS
BEGIN
    -- �������� �� ���������� ������� �����������
    IF EXISTS (
        SELECT i.PhoneNumber
        FROM Inserted i
        JOIN Employee e ON i.PhoneNumber = e.PhoneNumber AND i.EmployeeID <> e.EmployeeID
    )
    BEGIN
        RAISERROR('����� �������� ��� ������������ ������ �����������', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    -- �������� �� ���������� � ������� �������
    IF EXISTS (
        SELECT i.PhoneNumber
        FROM Inserted i
        JOIN Customer c ON i.PhoneNumber = c.PhoneNumber
    )
    BEGIN
        RAISERROR('����� �������� ���������� ��������� � ������� �������', 16, 1);
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
        RAISERROR('������������ ����� ��� �������. ����������, ��������� � ������', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;


