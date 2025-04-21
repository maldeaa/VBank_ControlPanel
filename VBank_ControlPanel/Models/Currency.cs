using System;
using System.Collections.Generic;

namespace VBank_ControlPanel.Models
{
    public partial class Currency
    {
        public Currency()
        {
            Credits = new HashSet<Credit>();
            Deposits = new HashSet<Deposit>();
        }

        public int CurrencyId { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }

        public virtual ICollection<Credit> Credits { get; set; }
        public virtual ICollection<Deposit> Deposits { get; set; }
    }
}
