using AccountingPoc.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingPoc.Services
{
    public class LedgerEntryTypeRepo
    {
        private List<LedgerEntryType> _entries;

        public LedgerEntryTypeRepo()
        {
            _entries = new List<LedgerEntryType>
            {
                new LedgerEntryType { Id = 1, Name = "Payment", IsCredit = true },
                new LedgerEntryType { Id = 2, Name = "Charge", IsCredit = false },
                new LedgerEntryType { Id = 3, Name = "Credit Memo", IsCredit = true },
                new LedgerEntryType { Id = 4, Name = "Adjustment", IsCredit = true }
            };
        }

        public LedgerEntryType GetById(int id)
        {
            return _entries.Single(e => e.Id == id);
        }

        
    }
}
