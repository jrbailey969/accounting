using AccountingPoc.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingPoc.Services
{
    public class LedgerEntryTransRepo
    {
        private List<LedgerEntryTrans> _entries = new List<LedgerEntryTrans>();

        public void Add(LedgerEntryTrans entry)
        {
            _entries.Add(entry);
        }

        public LedgerEntryTrans GetById(int id)
        {
            return _entries.Single(e => e.Id == id);
        }

        public List<LedgerEntryTrans> GetByLedgerEntryId(int entryId)
        {
            return _entries.Where(e => e.LedgerEntryId == entryId).ToList();
        }

        public List<LedgerEntryTrans> GetAll()
        {
            return _entries;
        }


    }
}
