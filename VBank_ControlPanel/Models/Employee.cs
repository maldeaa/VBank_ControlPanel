using System;
using System.Collections.Generic;

namespace VBank_ControlPanel.Models
{
    public partial class Employee
    {
        public Employee()
        {
            Credits = new HashSet<Credit>();
            Deposits = new HashSet<Deposit>();
            OperationLogs = new HashSet<OperationLog>();
        }

        public int EmployeeId { get; set; }
        public int? RoleId { get; set; }
        public string? FullName { get; set; }
        public int? Age { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Login { get; set; }
        public string? PasswordHash { get; set; }
        public decimal? Salary { get; set; }

        public virtual Role? Role { get; set; }
        public virtual ICollection<Credit> Credits { get; set; }
        public virtual ICollection<Deposit> Deposits { get; set; }
        public virtual ICollection<OperationLog> OperationLogs { get; set; }
    }
}
