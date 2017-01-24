using AccountingPoc.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingPoc.Services
{
    public class LedgerEntryRepo
    {
        private List<LedgerEntry> _entries = new List<LedgerEntry>();

        public void Add(LedgerEntry entry)
        {
            _entries.Add(entry);
        }

        public LedgerEntry GetById(int id)
        {
            return _entries.Single(e => e.Id == id);
        }

        public List<LedgerEntry> GetAll()
        {
            return _entries;
        }

        
    }
}
