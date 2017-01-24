using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingPoc.Model
{
    public class LedgerEntry
    {
        public int Id { get; set; }
        public int LedgerEntryTypeId { get; set; }
        public DateTime AccountingDate { get; set; }
        public string Description { get; set; }
        public string TypeQualifier { get; set; }
        public decimal Amount { get; set; }
        public decimal Balance { get; set; }
    }
}
