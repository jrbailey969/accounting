using AccountingPoc.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingPoc.Data
{
    public class TransactionAction
    {
        public TransactionAction()
        {
            Actions = new List<AdjustmentAction>();
        }

        public LedgerEntry LedgerEntry { get; set; }
        public List<AdjustmentAction> Actions { get; set; }

        #region Nested Classes
        public class AdjustmentAction
        {
            public Enumerations.LedgerEntryTransType LedgerEntryTransType { get; set; }
            public decimal Amount { get; set; }
            public LedgerEntry RelatedLedgerEntry { get; set; }
        }
        #endregion
    }
}
