using System;
using System.Collections.Generic;

namespace VBank_ControlPanel.Models
{
    public partial class Credit
    {
        public Credit()
        {
            CreditPayments = new HashSet<CreditPayment>();
        }

        public int CreditId { get; set; }
        public int? CustomerId { get; set; }
        public int? EmployeeId { get; set; }
        public int? CurrencyId { get; set; }
        public string? ContractNumber { get; set; }
        public DateTime? StartDate { get; set; }
        public int? Term { get; set; }
        public decimal? InterestRate { get; set; }
        public decimal? Amount { get; set; }

        public virtual Currency? Currency { get; set; }
        public virtual Customer? Customer { get; set; }
        public virtual Employee? Employee { get; set; }
        public virtual ICollection<CreditPayment> CreditPayments { get; set; }
    }
}
