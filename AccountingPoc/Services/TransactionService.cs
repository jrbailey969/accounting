using AccountingPoc.Data;
using AccountingPoc.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingPoc.Services
{
    public class TransactionService
    {
        private LedgerEntryRepo _ledgerEntryRepo;
        private LedgerEntryTransRepo _ledgerEntryTransRepo;
        private LedgerEntryTypeRepo _ledgerEntryTypeRepo;

        public TransactionService(
            LedgerEntryRepo ledgerEntryRepo, 
            LedgerEntryTransRepo ledgerEntryTransRepo, 
            LedgerEntryTypeRepo ledgerEntryTypeRepo)
        {
            _ledgerEntryRepo = ledgerEntryRepo;
            _ledgerEntryTransRepo = ledgerEntryTransRepo;
            _ledgerEntryTypeRepo = ledgerEntryTypeRepo;
        }

        public void AddTransaction(List<TransactionAction> actions)
        {
            int transactionId = GetNextTransactionId();
            ValidateBalanceAdjustments(actions);
            foreach(TransactionAction action in actions)
            {
                ProcessAction(action, transactionId);
            }
        }

        public void VoidLedgerEntry(LedgerEntry entry)
        {
            var transactionActions = new List<TransactionAction>();

            // Void the main ledger entry and it's amount and balance transactions
            var entryTransactions = _ledgerEntryTransRepo.GetByLedgerEntryId(entry.Id);
            var transactionAction = new TransactionAction
            {
                LedgerEntry = entry,
                Actions = new List<TransactionAction.AdjustmentAction>()
            };

            decimal amountTotal = (from t in entryTransactions
                                where t.LedgerEntryTransTypeId == (int)Enumerations.LedgerEntryTransType.Amount
                                select t)
                                .DefaultIfEmpty()
                                .Sum(a => a == null ? 0 : a.Amount.HasValue ? a.Amount.Value : 0);

            if (amountTotal != 0)
            {
                transactionAction.Actions.Add(new TransactionAction.AdjustmentAction
                {
                    LedgerEntryTransType = Enumerations.LedgerEntryTransType.Amount,
                    Amount = amountTotal * -1
                });
            }

            //decimal balanceTotal = (from t in entryTransactions
            //                       where t.LedgerEntryTransTypeId == (int)Enumerations.LedgerEntryTransType.Balance
            //                       select t)
            //                    .DefaultIfEmpty()
            //                    .Sum(a => a == null ? 0 : a.Balance.HasValue ? a.Balance.Value : 0);

            //if (balanceTotal != 0)
            //{
            //    transactionAction.Actions.Add(new TransactionAction.AdjustmentAction
            //    {
            //        LedgerEntryTransType = Enumerations.LedgerEntryTransType.Balance,
            //        Amount = balanceTotal * -1
            //    });
            //}



            // Get the associated balance trans for the related entities and negate them
            entryTransactions
                .Where(t => t.LedgerEntryTransTypeId == (int)Enumerations.LedgerEntryTransType.Balance)
                .ToList()
                .ForEach(t =>
                {
                    if (!t.Balance.HasValue)
                        return;

                    var relatedEntry = _ledgerEntryRepo.GetById(t.RelatedLedgerEntryId.Value);

                    transactionAction.Actions.Add(new TransactionAction.AdjustmentAction
                    {
                        LedgerEntryTransType = Enumerations.LedgerEntryTransType.Balance,
                        Amount = t.Balance.Value * -1,
                        RelatedLedgerEntry = relatedEntry
                    });


                    var relatedTransactionAction = new TransactionAction
                    {
                        LedgerEntry = relatedEntry,
                        Actions = new List<TransactionAction.AdjustmentAction>
                        {
                            new TransactionAction.AdjustmentAction
                            {
                                LedgerEntryTransType = Enumerations.LedgerEntryTransType.Balance,
                                Amount = t.Balance.Value * -1,
                                RelatedLedgerEntry = entry
                            }
                        }
                    };

                    transactionActions.Add(relatedTransactionAction);
                });

            transactionActions.Add(transactionAction);
            this.AddTransaction(transactionActions);
        }

        #region Private Methods

        private void ProcessAction(TransactionAction action, int transactionId)
        {
            LedgerEntry entry;
            List<LedgerEntryTrans> entryTransactions;
            bool isNewEntry = false;
            if (action.LedgerEntry.Id == 0)
            {
                isNewEntry = true;
                entry = action.LedgerEntry;
                entry.Id = GetNextLedgerEntryId();
                entry.Balance = entry.Amount;
                entryTransactions = new List<LedgerEntryTrans>();
                action.Actions.Add(
                    new TransactionAction.AdjustmentAction
                    {
                        LedgerEntryTransType = Enumerations.LedgerEntryTransType.Amount,
                        Amount = entry.Amount,
                    }
                    );
            }
            else
            {
                entry = _ledgerEntryRepo.GetById(action.LedgerEntry.Id);
                entryTransactions = _ledgerEntryTransRepo.GetByLedgerEntryId(entry.Id);
            }

            if (isNewEntry)
            {
                _ledgerEntryRepo.Add(entry);
            }


            action.Actions.ForEach(a => {
                var ledgerEntryTrans = new LedgerEntryTrans
                {
                    Id = GetNextLedgerEntryTransId(),
                    TransactionId = transactionId,
                    AccountingDate = entry.AccountingDate,
                    LedgerEntryId = entry.Id,
                    LedgerEntryTransTypeId = (int)a.LedgerEntryTransType,
                    Amount = a.LedgerEntryTransType == Enumerations.LedgerEntryTransType.Amount ? (decimal?)a.Amount : null,
                    Balance = a.LedgerEntryTransType == Enumerations.LedgerEntryTransType.Balance ? (decimal?)a.Amount : null,
                    TypeQualifier = entry.TypeQualifier,
                    RelatedLedgerEntryId = a.LedgerEntryTransType == Enumerations.LedgerEntryTransType.Balance ? (int?)a.RelatedLedgerEntry.Id : null
                };
                entryTransactions.Add(ledgerEntryTrans);
                _ledgerEntryTransRepo.Add(ledgerEntryTrans);
            });

            // Recalculate entry amount based on entry transactions of type amount
            entry.Amount = entryTransactions
                .Where(t => t.LedgerEntryTransTypeId == (int)Enumerations.LedgerEntryTransType.Amount)
                .DefaultIfEmpty()
                .Sum(t => t == null ? 0 : t.Amount.HasValue ? t.Amount.Value : 0);

            // Recalculate entry balance based on entry transactions of type balance
            decimal balanceAdj = entryTransactions
                .Where(t => t.LedgerEntryTransTypeId == (int)Enumerations.LedgerEntryTransType.Balance)
                .DefaultIfEmpty()
                .Sum(t => t == null ? 0 : t.Balance.HasValue ? t.Balance.Value : 0);
            entry.Balance = entry.Amount + balanceAdj;
        }

        private void ValidateBalanceAdjustments(List<TransactionAction> actions)
        {
            decimal balanceAdjustmentTotal = 0;
            foreach(var action in actions)
            {
                foreach(var subAction in action.Actions)
                {
                    if (subAction.LedgerEntryTransType == Enumerations.LedgerEntryTransType.Balance && subAction.RelatedLedgerEntry == null)
                    {
                        throw new ApplicationException("Balance transactions must have a related entity.");
                    }
                }

                decimal subTotal = (from a in action.Actions
                                    where a.LedgerEntryTransType == Enumerations.LedgerEntryTransType.Balance
                                    select a)
                                    .DefaultIfEmpty()
                                    .Sum(a => a == null ? 0 : a.Amount);

                if (IsEntryCredit(action.LedgerEntry))
                {
                    subTotal = subTotal * -1;
                }
                balanceAdjustmentTotal += subTotal;
            }

            if (balanceAdjustmentTotal != 0)
            {
                throw new UnbalancedTransactionException("All balance adjustments within a transaction must balance to zero.");
            }
        }

        private bool IsEntryCredit(LedgerEntry entry)
        {
            var entryType = _ledgerEntryTypeRepo.GetById(entry.LedgerEntryTypeId);
            if (entryType == null)
            {
                throw new ApplicationException("Invalid ledger entry type: " + entry.LedgerEntryTypeId);
            }

            return entryType.IsCredit;
        }

        private int GetNextTransactionId()
        {
            return _ledgerEntryTransRepo.GetAll().DefaultIfEmpty().Max(t => t == null ? 0 : t.TransactionId) + 1;
        }

        private int GetNextLedgerEntryId()
        {
            return _ledgerEntryRepo.GetAll().DefaultIfEmpty().Max(t => t == null ? 0 : t.Id) + 1;
        }

        private int GetNextLedgerEntryTransId()
        {
            return _ledgerEntryTransRepo.GetAll().DefaultIfEmpty().Max(t => t == null ? 0 : t.Id) + 1;
        }

        #endregion

        #region Nested Classes
        public class UnbalancedTransactionException : ApplicationException
        {
            public UnbalancedTransactionException(string msg)
                : base(msg)
            {
            }
        }
        #endregion

    }
}
