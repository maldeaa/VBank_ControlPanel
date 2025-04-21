using System;
using System.Collections.Generic;

namespace VBank_ControlPanel.Models
{
    public partial class DepositType
    {
        public DepositType()
        {
            Deposits = new HashSet<Deposit>();
        }

        public int DepositTypeId { get; set; }
        public string? Name { get; set; }
        public int? MinTerm { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? InterestRate { get; set; }

        public virtual ICollection<Deposit> Deposits { get; set; }
    }
}
