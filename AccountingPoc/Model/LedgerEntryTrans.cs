using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingPoc.Model
{
    public class LedgerEntryTrans
    {
        public int Id { get; set; }
        public int TransactionId { get; set; }
        public int LedgerEntryId { get; set; }
        public int LedgerEntryTransTypeId { get; set; }
        public DateTime AccountingDate { get; set; }
        public string TypeQualifier { get; set; }
        public decimal? Amount { get; set; }
        public decimal? Balance { get; set; }
        public int? RelatedLedgerEntryId { get; set; }
    }
}
