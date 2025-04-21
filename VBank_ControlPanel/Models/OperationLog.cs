using System;
using System.Collections.Generic;

namespace VBank_ControlPanel.Models
{
    public partial class OperationLog
    {
        public int LogId { get; set; }
        public int? EmployeeId { get; set; }
        public string? OperationType { get; set; }
        public DateTime? OperationDate { get; set; }
        public string? Details { get; set; }

        public virtual Employee? Employee { get; set; }
    }
}
