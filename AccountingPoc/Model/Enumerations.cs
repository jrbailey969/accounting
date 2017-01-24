using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingPoc.Model
{
    public static class Enumerations
    {
        public enum LedgerEntryType
        {
            Payment = 1,
            Charge = 2,
            CreditMemo = 3,
            Adjustment = 4
        }

        public enum LedgerEntryTransType
        {
            Amount = 1,
            Balance = 2
        }
    }
}
