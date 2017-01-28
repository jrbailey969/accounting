using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AccountingPoc.Data;
using AccountingPoc.Model;
using AccountingPoc.Services;
using System.Collections.Generic;

namespace AccountPocTests
{
    [TestClass]
    public class TransactionServiceTests
    {
        private LedgerEntryRepo _ledgerEntryRepo;
        private LedgerEntryTransRepo _ledgerEntryTransRepo;
        private TransactionService _transactionService;

        [TestInitialize]
        public void Setup()
        {
            _ledgerEntryRepo = new LedgerEntryRepo();
            _ledgerEntryTransRepo = new LedgerEntryTransRepo();
            _transactionService = new TransactionService(_ledgerEntryRepo, _ledgerEntryTransRepo, new LedgerEntryTypeRepo());
        }

        [TestMethod]
        public void AddTransaction_NewPayment()
        {
            // Arrange
            DateTime accountingDate = new DateTime(2017, 1, 1);
            var actions = new List<TransactionAction>
            {
                new TransactionAction
                {
                    LedgerEntry = new LedgerEntry
                    {
                        AccountingDate = accountingDate,
                        LedgerEntryTypeId = (int)Enumerations.LedgerEntryType.Payment,
                        Description = "Test",
                        Amount = 100,
                        TypeQualifier = "10"
                    }
                }
            };

            // Act
            _transactionService.AddTransaction(actions);

            // Assert
            var entries = _ledgerEntryRepo.GetAll();
            Assert.AreEqual(1, entries.Count);
            var entry = entries[0];
            Assert.AreEqual(1, entry.Id);
            Assert.AreEqual(accountingDate, entry.AccountingDate);
            Assert.AreEqual((int)Enumerations.LedgerEntryType.Payment, entry.LedgerEntryTypeId);
            Assert.AreEqual("Test", entry.Description);
            Assert.AreEqual(100, entry.Amount);
            Assert.AreEqual(100, entry.Balance);
            Assert.AreEqual("10", entry.TypeQualifier);

            var transactions = _ledgerEntryTransRepo.GetAll();
            Assert.AreEqual(1, transactions.Count);
            var trans = transactions[0];
            Assert.AreEqual(1, trans.Id);
            Assert.AreEqual(1, trans.TransactionId);
            Assert.AreEqual(accountingDate, trans.AccountingDate);
            Assert.AreEqual((int)Enumerations.LedgerEntryTransType.Amount, trans.LedgerEntryTransTypeId);
            Assert.AreEqual(100, trans.Amount);
            Assert.IsNull(trans.Balance);
            Assert.AreEqual("10", trans.TypeQualifier);

        }

        [TestMethod]
        public void AddTransaction_ApplyPaymentToCharge()
        {
            // Arrange
            DateTime accountingDate = new DateTime(2017, 1, 1);
            DateTime applyAccountingDate = new DateTime(2017, 1, 2);
            LedgerEntry payment = new LedgerEntry
            {
                AccountingDate = accountingDate,
                LedgerEntryTypeId = (int)Enumerations.LedgerEntryType.Payment,
                Description = "Test Payment",
                Amount = 100,
                TypeQualifier = "10"
            };
            LedgerEntry charge = new LedgerEntry
            {
                AccountingDate = accountingDate,
                LedgerEntryTypeId = (int)Enumerations.LedgerEntryType.Charge,
                Description = "Test Charge",
                Amount = 200,
                TypeQualifier = "11"
            };

            var paymentActions = new List<TransactionAction>
            {
                new TransactionAction
                {
                    LedgerEntry = payment
                }
            };

            var chargeActions = new List<TransactionAction>
            {
                new TransactionAction
                {
                    LedgerEntry = charge
                }
            };

            var applyPaymentActions = new List<TransactionAction>
            {
                new TransactionAction
                {
                    LedgerEntry = payment,
                    Actions = new List<TransactionAction.AdjustmentAction>
                    {
                        new TransactionAction.AdjustmentAction
                        {
                            LedgerEntryTransType = Enumerations.LedgerEntryTransType.Balance,
                            Amount = -100,
                            RelatedLedgerEntry = charge,
                            AccountingDate = applyAccountingDate
                        }
                    }
                },
                new TransactionAction
                {
                    LedgerEntry = charge,
                    Actions = new List<TransactionAction.AdjustmentAction>
                    {
                        new TransactionAction.AdjustmentAction
                        {
                            LedgerEntryTransType = Enumerations.LedgerEntryTransType.Balance,
                            Amount = -100,
                            RelatedLedgerEntry = payment,
                            AccountingDate = applyAccountingDate
                        }
                    }
                }
            };

            // Act
            _transactionService.AddTransaction(paymentActions);
            _transactionService.AddTransaction(chargeActions);
            _transactionService.AddTransaction(applyPaymentActions);

            // Assert
            payment = _ledgerEntryRepo.GetById(1);
            Assert.AreEqual(accountingDate, payment.AccountingDate);
            Assert.AreEqual((int)Enumerations.LedgerEntryType.Payment, payment.LedgerEntryTypeId);
            Assert.AreEqual("Test Payment", payment.Description);
            Assert.AreEqual(100, payment.Amount);
            Assert.AreEqual(0, payment.Balance);
            Assert.AreEqual("10", payment.TypeQualifier);

            var transactions = _ledgerEntryTransRepo.GetByLedgerEntryId(payment.Id);
            Assert.AreEqual(2, transactions.Count);
            var trans = transactions[0];
            Assert.AreEqual(1, trans.TransactionId);
            Assert.AreEqual(accountingDate, trans.AccountingDate);
            Assert.AreEqual((int)Enumerations.LedgerEntryTransType.Amount, trans.LedgerEntryTransTypeId);
            Assert.AreEqual(100, trans.Amount);
            Assert.IsNull(trans.Balance);
            Assert.AreEqual("10", trans.TypeQualifier);
            Assert.IsNull(trans.RelatedLedgerEntryId);

            trans = transactions[1];
            Assert.AreEqual(3, trans.TransactionId);
            Assert.AreEqual(applyAccountingDate, trans.AccountingDate);
            Assert.AreEqual((int)Enumerations.LedgerEntryTransType.Balance, trans.LedgerEntryTransTypeId);
            Assert.AreEqual(-100, trans.Balance);
            Assert.IsNull(trans.Amount);
            Assert.AreEqual("10", trans.TypeQualifier);
            Assert.AreEqual(2, trans.RelatedLedgerEntryId);

            charge = _ledgerEntryRepo.GetById(2);
            Assert.AreEqual(accountingDate, charge.AccountingDate);
            Assert.AreEqual((int)Enumerations.LedgerEntryType.Charge, charge.LedgerEntryTypeId);
            Assert.AreEqual("Test Charge", charge.Description);
            Assert.AreEqual(200, charge.Amount);
            Assert.AreEqual(100, charge.Balance);
            Assert.AreEqual("11", charge.TypeQualifier);

            transactions = _ledgerEntryTransRepo.GetByLedgerEntryId(charge.Id);
            Assert.AreEqual(2, transactions.Count);
            trans = transactions[0];
            Assert.AreEqual(2, trans.TransactionId);
            Assert.AreEqual(accountingDate, trans.AccountingDate);
            Assert.AreEqual((int)Enumerations.LedgerEntryTransType.Amount, trans.LedgerEntryTransTypeId);
            Assert.AreEqual(200, trans.Amount);
            Assert.IsNull(trans.Balance);
            Assert.AreEqual("11", trans.TypeQualifier);
            Assert.IsNull(trans.RelatedLedgerEntryId);

            trans = transactions[1];
            Assert.AreEqual(3, trans.TransactionId);
            Assert.AreEqual(applyAccountingDate, trans.AccountingDate);
            Assert.AreEqual((int)Enumerations.LedgerEntryTransType.Balance, trans.LedgerEntryTransTypeId);
            Assert.AreEqual(-100, trans.Balance);
            Assert.IsNull(trans.Amount);
            Assert.AreEqual("11", trans.TypeQualifier);
            Assert.AreEqual(1, trans.RelatedLedgerEntryId);

        }

        [TestMethod]
        public void AddTransaction_ModifyChargeArmount_InPlace()
        {
            // Arrange
            DateTime accountingDate = new DateTime(2017, 1, 1);
            DateTime modificationDate = new DateTime(2017, 1, 1, 12, 0, 0);
            var charge = new LedgerEntry
            {
                AccountingDate = accountingDate,
                LedgerEntryTypeId = (int)Enumerations.LedgerEntryType.Charge,
                Description = "Test Charge",
                Amount = 200,
                TypeQualifier = "11"
            };

            var chargeActions = new List<TransactionAction>
            {
                new TransactionAction
                {
                    LedgerEntry = charge
                }
            };

            var modifyChargeActions = new List<TransactionAction>
            {
                new TransactionAction
                {
                    LedgerEntry = charge,
                    Actions = new List<TransactionAction.AdjustmentAction>
                    {
                        new TransactionAction.AdjustmentAction
                        {
                            LedgerEntryTransType = Enumerations.LedgerEntryTransType.Amount,
                            Amount = -20,
                            AccountingDate = modificationDate
                        }
                    }
                }
            };

            // Act
            _transactionService.AddTransaction(chargeActions);
            _transactionService.AddTransaction(modifyChargeActions);

            // Assert
            charge = _ledgerEntryRepo.GetById(1);
            Assert.AreEqual(accountingDate, charge.AccountingDate);
            Assert.AreEqual((int)Enumerations.LedgerEntryType.Charge, charge.LedgerEntryTypeId);
            Assert.AreEqual("Test Charge", charge.Description);
            Assert.AreEqual(180, charge.Amount);
            Assert.AreEqual(180, charge.Balance);
            Assert.AreEqual("11", charge.TypeQualifier);

            var transactions = _ledgerEntryTransRepo.GetByLedgerEntryId(charge.Id);
            Assert.AreEqual(1, transactions.Count);
            var trans = transactions[0];
            Assert.AreEqual(1, trans.TransactionId);
            Assert.AreEqual(accountingDate, trans.AccountingDate);
            Assert.AreEqual((int)Enumerations.LedgerEntryTransType.Amount, trans.LedgerEntryTransTypeId);
            Assert.AreEqual(180, trans.Amount);
            Assert.IsNull(trans.Balance);
            Assert.AreEqual("11", trans.TypeQualifier);
        }

        [TestMethod]
        public void AddTransaction_ModifyChargeArmount_NewEntryTrans()
        {
            // Arrange
            DateTime accountingDate = new DateTime(2017, 1, 1);
            DateTime modificationAccountingDate = new DateTime(2017, 1, 2);

            var charge = new LedgerEntry
            {
                AccountingDate = accountingDate,
                LedgerEntryTypeId = (int)Enumerations.LedgerEntryType.Charge,
                Description = "Test Charge",
                Amount = 200,
                TypeQualifier = "11"
            };

            var chargeActions = new List<TransactionAction>
            {
                new TransactionAction
                {
                    LedgerEntry = charge
                }
            };

            var modifyChargeActions = new List<TransactionAction>
            {
                new TransactionAction
                {
                    LedgerEntry = charge,
                    Actions = new List<TransactionAction.AdjustmentAction>
                    {
                        new TransactionAction.AdjustmentAction
                        {
                            LedgerEntryTransType = Enumerations.LedgerEntryTransType.Amount,
                            Amount = -20,
                            AccountingDate = modificationAccountingDate
                        }
                    }
                }
            };

            // Act
            _transactionService.AddTransaction(chargeActions);
            _transactionService.AddTransaction(modifyChargeActions);

            // Assert
            charge = _ledgerEntryRepo.GetById(1);
            Assert.AreEqual(accountingDate, charge.AccountingDate);
            Assert.AreEqual((int)Enumerations.LedgerEntryType.Charge, charge.LedgerEntryTypeId);
            Assert.AreEqual("Test Charge", charge.Description);
            Assert.AreEqual(180, charge.Amount);
            Assert.AreEqual(180, charge.Balance);
            Assert.AreEqual("11", charge.TypeQualifier);

            var transactions = _ledgerEntryTransRepo.GetByLedgerEntryId(charge.Id);
            Assert.AreEqual(2, transactions.Count);
            var trans = transactions[0];
            Assert.AreEqual(1, trans.TransactionId);
            Assert.AreEqual(accountingDate, trans.AccountingDate);
            Assert.AreEqual((int)Enumerations.LedgerEntryTransType.Amount, trans.LedgerEntryTransTypeId);
            Assert.AreEqual(200, trans.Amount);
            Assert.IsNull(trans.Balance);
            Assert.AreEqual("11", trans.TypeQualifier);

            trans = transactions[1];
            Assert.AreEqual(2, trans.TransactionId);
            Assert.AreEqual(modificationAccountingDate, trans.AccountingDate);
            Assert.AreEqual((int)Enumerations.LedgerEntryTransType.Amount, trans.LedgerEntryTransTypeId);
            Assert.AreEqual(-20, trans.Amount);
            Assert.IsNull(trans.Balance);
            Assert.AreEqual("11", trans.TypeQualifier);
        }

        [TestMethod]
        public void AddTransaction_ComplexTransaction()
        {
            // Arrange
            DateTime accountingDate = new DateTime(2017, 1, 1);
            LedgerEntry payment = new LedgerEntry
            {
                AccountingDate = accountingDate,
                LedgerEntryTypeId = (int)Enumerations.LedgerEntryType.Payment,
                Description = "Test Payment",
                Amount = 100,
                TypeQualifier = "10"
            };
            LedgerEntry charge = new LedgerEntry
            {
                AccountingDate = accountingDate,
                LedgerEntryTypeId = (int)Enumerations.LedgerEntryType.Charge,
                Description = "Test Charge",
                Amount = 200,
                TypeQualifier = "11"
            };

            var transActions = new List<TransactionAction>
            {
                new TransactionAction
                {
                    LedgerEntry = payment,
                    Actions = new List<TransactionAction.AdjustmentAction>
                    {
                        new TransactionAction.AdjustmentAction
                        {
                            LedgerEntryTransType = Enumerations.LedgerEntryTransType.Balance,
                            Amount = -100,
                            RelatedLedgerEntry = charge,
                            AccountingDate = accountingDate
                        }
                    }
                },
                new TransactionAction
                {
                    LedgerEntry = charge,
                    Actions = new List<TransactionAction.AdjustmentAction>
                    {
                        new TransactionAction.AdjustmentAction
                        {
                            LedgerEntryTransType = Enumerations.LedgerEntryTransType.Balance,
                            Amount = -100,
                            RelatedLedgerEntry = payment,
                            AccountingDate =accountingDate
                        }
                    }
                }
            };

            // Act
            _transactionService.AddTransaction(transActions);

            // Assert
            payment = _ledgerEntryRepo.GetById(1);
            Assert.AreEqual(accountingDate, payment.AccountingDate);
            Assert.AreEqual((int)Enumerations.LedgerEntryType.Payment, payment.LedgerEntryTypeId);
            Assert.AreEqual("Test Payment", payment.Description);
            Assert.AreEqual(100, payment.Amount);
            Assert.AreEqual(0, payment.Balance);
            Assert.AreEqual("10", payment.TypeQualifier);

            var transactions = _ledgerEntryTransRepo.GetByLedgerEntryId(payment.Id);
            Assert.AreEqual(2, transactions.Count);
            var trans = transactions[0];
            Assert.AreEqual(1, trans.TransactionId);
            Assert.AreEqual(accountingDate, trans.AccountingDate);
            Assert.AreEqual((int)Enumerations.LedgerEntryTransType.Amount, trans.LedgerEntryTransTypeId);
            Assert.AreEqual(100, trans.Amount);
            Assert.IsNull(trans.Balance);
            Assert.AreEqual("10", trans.TypeQualifier);
            Assert.IsNull(trans.RelatedLedgerEntryId);

            trans = transactions[1];
            Assert.AreEqual(1, trans.TransactionId);
            Assert.AreEqual(accountingDate, trans.AccountingDate);
            Assert.AreEqual((int)Enumerations.LedgerEntryTransType.Balance, trans.LedgerEntryTransTypeId);
            Assert.AreEqual(-100, trans.Balance);
            Assert.IsNull(trans.Amount);
            Assert.AreEqual("10", trans.TypeQualifier);
            Assert.AreEqual(2, trans.RelatedLedgerEntryId);

            charge = _ledgerEntryRepo.GetById(2);
            Assert.AreEqual(accountingDate, charge.AccountingDate);
            Assert.AreEqual((int)Enumerations.LedgerEntryType.Charge, charge.LedgerEntryTypeId);
            Assert.AreEqual("Test Charge", charge.Description);
            Assert.AreEqual(200, charge.Amount);
            Assert.AreEqual(100, charge.Balance);
            Assert.AreEqual("11", charge.TypeQualifier);

            transactions = _ledgerEntryTransRepo.GetByLedgerEntryId(charge.Id);
            Assert.AreEqual(2, transactions.Count);
            trans = transactions[0];
            Assert.AreEqual(1, trans.TransactionId);
            Assert.AreEqual(accountingDate, trans.AccountingDate);
            Assert.AreEqual((int)Enumerations.LedgerEntryTransType.Amount, trans.LedgerEntryTransTypeId);
            Assert.AreEqual(200, trans.Amount);
            Assert.IsNull(trans.Balance);
            Assert.AreEqual("11", trans.TypeQualifier);
            Assert.IsNull(trans.RelatedLedgerEntryId);

            trans = transactions[1];
            Assert.AreEqual(1, trans.TransactionId);
            Assert.AreEqual(accountingDate, trans.AccountingDate);
            Assert.AreEqual((int)Enumerations.LedgerEntryTransType.Balance, trans.LedgerEntryTransTypeId);
            Assert.AreEqual(-100, trans.Balance);
            Assert.IsNull(trans.Amount);
            Assert.AreEqual("11", trans.TypeQualifier);
            Assert.AreEqual(1, trans.RelatedLedgerEntryId);

        }

        [TestMethod]
        public void AddTransaction_VoidCharge()
        {
            // Arrange
            DateTime accountingDate = new DateTime(2017, 1, 1);
            DateTime applyAccountingDate = new DateTime(2017, 1, 2);
            DateTime voidAccountingDate = new DateTime(2017, 1, 3);
            LedgerEntry payment = new LedgerEntry
            {
                AccountingDate = accountingDate,
                LedgerEntryTypeId = (int)Enumerations.LedgerEntryType.Payment,
                Description = "Test Payment",
                Amount = 100,
                TypeQualifier = "10"
            };
            LedgerEntry charge = new LedgerEntry
            {
                AccountingDate = accountingDate,
                LedgerEntryTypeId = (int)Enumerations.LedgerEntryType.Charge,
                Description = "Test Charge",
                Amount = 200,
                TypeQualifier = "11"
            };

            var paymentActions = new List<TransactionAction>
            {
                new TransactionAction
                {
                    LedgerEntry = payment
                }
            };

            var chargeActions = new List<TransactionAction>
            {
                new TransactionAction
                {
                    LedgerEntry = charge
                }
            };

            var applyPaymentActions = new List<TransactionAction>
            {
                new TransactionAction
                {
                    LedgerEntry = payment,
                    Actions = new List<TransactionAction.AdjustmentAction>
                    {
                        new TransactionAction.AdjustmentAction
                        {
                            LedgerEntryTransType = Enumerations.LedgerEntryTransType.Balance,
                            Amount = -100,
                            RelatedLedgerEntry = charge,
                            AccountingDate = applyAccountingDate
                        }
                    }
                },
                new TransactionAction
                {
                    LedgerEntry = charge,
                    Actions = new List<TransactionAction.AdjustmentAction>
                    {
                        new TransactionAction.AdjustmentAction
                        {
                            LedgerEntryTransType = Enumerations.LedgerEntryTransType.Balance,
                            Amount = -100,
                            RelatedLedgerEntry = payment,
                            AccountingDate = applyAccountingDate
                        }
                    }
                }
            };

            // Act
            _transactionService.AddTransaction(paymentActions);
            _transactionService.AddTransaction(chargeActions);
            _transactionService.AddTransaction(applyPaymentActions);
            _transactionService.VoidLedgerEntry(charge, voidAccountingDate);

            // Assert
            payment = _ledgerEntryRepo.GetById(1);
            Assert.AreEqual(accountingDate, payment.AccountingDate);
            Assert.AreEqual((int)Enumerations.LedgerEntryType.Payment, payment.LedgerEntryTypeId);
            Assert.AreEqual("Test Payment", payment.Description);
            Assert.AreEqual(100, payment.Amount);
            Assert.AreEqual(100, payment.Balance);
            Assert.AreEqual("10", payment.TypeQualifier);

            var transactions = _ledgerEntryTransRepo.GetByLedgerEntryId(payment.Id);
            Assert.AreEqual(3, transactions.Count);
            var trans = transactions[0];
            Assert.AreEqual(1, trans.TransactionId);
            Assert.AreEqual(accountingDate, trans.AccountingDate);
            Assert.AreEqual((int)Enumerations.LedgerEntryTransType.Amount, trans.LedgerEntryTransTypeId);
            Assert.AreEqual(100, trans.Amount);
            Assert.IsNull(trans.Balance);
            Assert.AreEqual("10", trans.TypeQualifier);
            Assert.IsNull(trans.RelatedLedgerEntryId);

            trans = transactions[1];
            Assert.AreEqual(3, trans.TransactionId);
            Assert.AreEqual(applyAccountingDate, trans.AccountingDate);
            Assert.AreEqual((int)Enumerations.LedgerEntryTransType.Balance, trans.LedgerEntryTransTypeId);
            Assert.AreEqual(-100, trans.Balance);
            Assert.IsNull(trans.Amount);
            Assert.AreEqual("10", trans.TypeQualifier);
            Assert.AreEqual(2, trans.RelatedLedgerEntryId);

            trans = transactions[2];
            Assert.AreEqual(4, trans.TransactionId);
            Assert.AreEqual(voidAccountingDate, trans.AccountingDate);
            Assert.AreEqual((int)Enumerations.LedgerEntryTransType.Balance, trans.LedgerEntryTransTypeId);
            Assert.AreEqual(100, trans.Balance);
            Assert.IsNull(trans.Amount);
            Assert.AreEqual("10", trans.TypeQualifier);
            Assert.AreEqual(2, trans.RelatedLedgerEntryId);

            charge = _ledgerEntryRepo.GetById(2);
            Assert.AreEqual(accountingDate, charge.AccountingDate);
            Assert.AreEqual((int)Enumerations.LedgerEntryType.Charge, charge.LedgerEntryTypeId);
            Assert.AreEqual("Test Charge", charge.Description);
            Assert.AreEqual(0, charge.Amount);
            Assert.AreEqual(0, charge.Balance);
            Assert.AreEqual("11", charge.TypeQualifier);

            transactions = _ledgerEntryTransRepo.GetByLedgerEntryId(charge.Id);
            Assert.AreEqual(4, transactions.Count);
            trans = transactions[0];
            Assert.AreEqual(2, trans.TransactionId);
            Assert.AreEqual(accountingDate, trans.AccountingDate);
            Assert.AreEqual((int)Enumerations.LedgerEntryTransType.Amount, trans.LedgerEntryTransTypeId);
            Assert.AreEqual(200, trans.Amount);
            Assert.IsNull(trans.Balance);
            Assert.AreEqual("11", trans.TypeQualifier);
            Assert.IsNull(trans.RelatedLedgerEntryId);

            trans = transactions[1];
            Assert.AreEqual(3, trans.TransactionId);
            Assert.AreEqual(applyAccountingDate, trans.AccountingDate);
            Assert.AreEqual((int)Enumerations.LedgerEntryTransType.Balance, trans.LedgerEntryTransTypeId);
            Assert.AreEqual(-100, trans.Balance);
            Assert.IsNull(trans.Amount);
            Assert.AreEqual("11", trans.TypeQualifier);
            Assert.AreEqual(1, trans.RelatedLedgerEntryId);

            trans = transactions[2];
            Assert.AreEqual(4, trans.TransactionId);
            Assert.AreEqual(voidAccountingDate, trans.AccountingDate);
            Assert.AreEqual((int)Enumerations.LedgerEntryTransType.Amount, trans.LedgerEntryTransTypeId);
            Assert.AreEqual(-200, trans.Amount);
            Assert.IsNull(trans.Balance);
            Assert.AreEqual("11", trans.TypeQualifier);
            Assert.IsNull(trans.RelatedLedgerEntryId);

            trans = transactions[3];
            Assert.AreEqual(4, trans.TransactionId);
            Assert.AreEqual(voidAccountingDate, trans.AccountingDate);
            Assert.AreEqual((int)Enumerations.LedgerEntryTransType.Balance, trans.LedgerEntryTransTypeId);
            Assert.AreEqual(100, trans.Balance);
            Assert.IsNull(trans.Amount);
            Assert.AreEqual("11", trans.TypeQualifier);
            Assert.AreEqual(1, trans.RelatedLedgerEntryId);

        }


    }
}
