using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace VBank_ControlPanel.Models
{
    public partial class VBankContent : DbContext
    {
        public VBankContent()
        {
        }

        public VBankContent(DbContextOptions<VBankContent> options)
            : base(options)
        {
        }

        public virtual DbSet<Credit> Credits { get; set; } = null!;
        public virtual DbSet<CreditPayment> CreditPayments { get; set; } = null!;
        public virtual DbSet<Currency> Currencies { get; set; } = null!;
        public virtual DbSet<Customer> Customers { get; set; } = null!;
        public virtual DbSet<Deposit> Deposits { get; set; } = null!;
        public virtual DbSet<DepositType> DepositTypes { get; set; } = null!;
        public virtual DbSet<Employee> Employees { get; set; } = null!;
        public virtual DbSet<OperationLog> OperationLogs { get; set; } = null!;
        public virtual DbSet<Role> Roles { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS;Database=VBank;Trusted_Connection=True;TrustServerCertificate=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Credit>(entity =>
            {
                entity.ToTable("Credit");

                entity.HasIndex(e => e.ContractNumber, "UQ__Credit__C51D43DAE61BE6CC")
                    .IsUnique();

                entity.Property(e => e.CreditId).HasColumnName("CreditID");

                entity.Property(e => e.Amount).HasColumnType("decimal(15, 2)");

                entity.Property(e => e.ContractNumber).HasMaxLength(20);

                entity.Property(e => e.CurrencyId).HasColumnName("CurrencyID");

                entity.Property(e => e.CustomerId).HasColumnName("CustomerID");

                entity.Property(e => e.EmployeeId).HasColumnName("EmployeeID");

                entity.Property(e => e.InterestRate).HasColumnType("decimal(5, 2)");

                entity.Property(e => e.StartDate).HasColumnType("date");

                entity.HasOne(d => d.Currency)
                    .WithMany(p => p.Credits)
                    .HasForeignKey(d => d.CurrencyId)
                    .HasConstraintName("FK__Credit__Currency__66603565");

                entity.HasOne(d => d.Customer)
                    .WithMany(p => p.Credits)
                    .HasForeignKey(d => d.CustomerId)
                    .HasConstraintName("FK__Credit__Customer__6477ECF3");

                entity.HasOne(d => d.Employee)
                    .WithMany(p => p.Credits)
                    .HasForeignKey(d => d.EmployeeId)
                    .HasConstraintName("FK__Credit__Employee__656C112C");
            });

            modelBuilder.Entity<CreditPayment>(entity =>
            {
                entity.HasKey(e => e.PaymentId)
                    .HasName("PK__CreditPa__9B556A58C67B04FE");

                entity.ToTable("CreditPayment");

                entity.Property(e => e.PaymentId).HasColumnName("PaymentID");

                entity.Property(e => e.Amount).HasColumnType("decimal(15, 2)");

                entity.Property(e => e.CreditId).HasColumnName("CreditID");

                entity.Property(e => e.IsPaid).HasDefaultValueSql("((0))");

                entity.Property(e => e.PaymentDate).HasColumnType("date");

                entity.HasOne(d => d.Credit)
                    .WithMany(p => p.CreditPayments)
                    .HasForeignKey(d => d.CreditId)
                    .HasConstraintName("FK__CreditPay__Credi__6A30C649");
            });

            modelBuilder.Entity<Currency>(entity =>
            {
                entity.ToTable("Currency");

                entity.HasIndex(e => e.Code, "UQ__Currency__A25C5AA7ACD46E0F")
                    .IsUnique();

                entity.Property(e => e.CurrencyId).HasColumnName("CurrencyID");

                entity.Property(e => e.Code).HasMaxLength(10);

                entity.Property(e => e.Name).HasMaxLength(50);
            });

            modelBuilder.Entity<Customer>(entity =>
            {
                entity.ToTable("Customer");

                entity.HasIndex(e => e.PassportData, "UQ__Customer__E8994A81CFF3F4AC")
                    .IsUnique();

                entity.Property(e => e.CustomerId).HasColumnName("CustomerID");

                entity.Property(e => e.Email).HasMaxLength(50);

                entity.Property(e => e.FullName).HasMaxLength(100);

                entity.Property(e => e.IsBanned).HasColumnName("isBanned");

                entity.Property(e => e.PassportData).HasMaxLength(50);

                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            });

            modelBuilder.Entity<Deposit>(entity =>
            {
                entity.ToTable("Deposit");

                entity.HasIndex(e => e.ContractNumber, "UQ__Deposit__C51D43DACCE29A49")
                    .IsUnique();

                entity.Property(e => e.DepositId).HasColumnName("DepositID");

                entity.Property(e => e.Amount).HasColumnType("decimal(15, 2)");

                entity.Property(e => e.ContractNumber).HasMaxLength(20);

                entity.Property(e => e.CurrencyId).HasColumnName("CurrencyID");

                entity.Property(e => e.CustomerId).HasColumnName("CustomerID");

                entity.Property(e => e.DepositTypeId).HasColumnName("DepositTypeID");

                entity.Property(e => e.EmployeeId).HasColumnName("EmployeeID");

                entity.Property(e => e.EndDate).HasColumnType("date");

                entity.Property(e => e.ReturnAmount)
                    .HasColumnType("decimal(15, 2)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.StartDate).HasColumnType("date");

                entity.HasOne(d => d.Currency)
                    .WithMany(p => p.Deposits)
                    .HasForeignKey(d => d.CurrencyId)
                    .HasConstraintName("FK__Deposit__Currenc__5FB337D6");

                entity.HasOne(d => d.Customer)
                    .WithMany(p => p.Deposits)
                    .HasForeignKey(d => d.CustomerId)
                    .HasConstraintName("FK__Deposit__Custome__5CD6CB2B");

                entity.HasOne(d => d.DepositType)
                    .WithMany(p => p.Deposits)
                    .HasForeignKey(d => d.DepositTypeId)
                    .HasConstraintName("FK__Deposit__Deposit__5EBF139D");

                entity.HasOne(d => d.Employee)
                    .WithMany(p => p.Deposits)
                    .HasForeignKey(d => d.EmployeeId)
                    .HasConstraintName("FK__Deposit__Employe__5DCAEF64");
            });

            modelBuilder.Entity<DepositType>(entity =>
            {
                entity.ToTable("DepositType");

                entity.Property(e => e.DepositTypeId).HasColumnName("DepositTypeID");

                entity.Property(e => e.InterestRate).HasColumnType("decimal(5, 2)");

                entity.Property(e => e.MinAmount).HasColumnType("decimal(15, 2)");

                entity.Property(e => e.Name).HasMaxLength(50);
            });

            modelBuilder.Entity<Employee>(entity =>
            {
                entity.ToTable("Employee");

                entity.HasIndex(e => e.Login, "UQ__Employee__5E55825B96A33DF9")
                    .IsUnique();

                entity.Property(e => e.EmployeeId).HasColumnName("EmployeeID");

                entity.Property(e => e.FullName).HasMaxLength(100);

                entity.Property(e => e.Login).HasMaxLength(50);

                entity.Property(e => e.PasswordHash).HasMaxLength(100);

                entity.Property(e => e.PhoneNumber).HasMaxLength(20);

                entity.Property(e => e.RoleId).HasColumnName("RoleID");

                entity.Property(e => e.Salary).HasColumnType("decimal(10, 2)");

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.Employees)
                    .HasForeignKey(d => d.RoleId)
                    .HasConstraintName("FK__Employee__RoleID__4D94879B");
            });

            modelBuilder.Entity<OperationLog>(entity =>
            {
                entity.HasKey(e => e.LogId)
                    .HasName("PK__Operatio__5E5499A876093926");

                entity.ToTable("OperationLog");

                entity.Property(e => e.LogId).HasColumnName("LogID");

                entity.Property(e => e.Details).HasMaxLength(200);

                entity.Property(e => e.EmployeeId).HasColumnName("EmployeeID");

                entity.Property(e => e.OperationDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.OperationType).HasMaxLength(50);

                entity.HasOne(d => d.Employee)
                    .WithMany(p => p.OperationLogs)
                    .HasForeignKey(d => d.EmployeeId)
                    .HasConstraintName("FK__Operation__Emplo__6E01572D");
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Role");

                entity.HasIndex(e => e.RoleName, "UQ__Role__8A2B6160382AD2A0")
                    .IsUnique();

                entity.Property(e => e.RoleId).HasColumnName("RoleID");

                entity.Property(e => e.Description).HasMaxLength(200);

                entity.Property(e => e.RoleName).HasMaxLength(50);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
