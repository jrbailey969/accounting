using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingPoc.Model
{
    public class LedgerEntryType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsCredit { get; set; }
    }
}
