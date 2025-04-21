using System;
using System.Collections.Generic;

namespace VBank_ControlPanel.Models
{
    public partial class Deposit
    {
        public int DepositId { get; set; }
        public int? CustomerId { get; set; }
        public int? EmployeeId { get; set; }
        public int? DepositTypeId { get; set; }
        public int? CurrencyId { get; set; }
        public string? ContractNumber { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? Amount { get; set; }
        public decimal? ReturnAmount { get; set; }

        public virtual Currency? Currency { get; set; }
        public virtual Customer? Customer { get; set; }
        public virtual DepositType? DepositType { get; set; }
        public virtual Employee? Employee { get; set; }
    }
}
