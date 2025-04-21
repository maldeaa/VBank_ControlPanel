using System;
using System.Collections.Generic;

namespace VBank_ControlPanel.Models
{
    public partial class CreditPayment
    {
        public int PaymentId { get; set; }
        public int? CreditId { get; set; }
        public DateTime? PaymentDate { get; set; }
        public decimal? Amount { get; set; }
        public bool? IsPaid { get; set; }

        public virtual Credit? Credit { get; set; }
    }
}
