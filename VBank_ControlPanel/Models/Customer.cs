using System;
using System.Collections.Generic;

namespace VBank_ControlPanel.Models
{
    public partial class Customer
    {
        public Customer()
        {
            Credits = new HashSet<Credit>();
            Deposits = new HashSet<Deposit>();
        }

        public int CustomerId { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? PassportData { get; set; }
        public bool IsBanned { get; set; }

        public virtual ICollection<Credit> Credits { get; set; }
        public virtual ICollection<Deposit> Deposits { get; set; }
    }
}
