using System;
using Sandbox.Game.GameSystems.BankingAndCurrency;

namespace CrunchEconomy.Helpers
{
    public class EconUtils
    {
        public static long GetBalance(long WalletId)
        {
            return MyBankingSystem.Static.TryGetAccountInfo(WalletId, out var info) ? info.Balance : 0L;
        }
        public static void AddMoney(long WalletId, long Amount)
        {
            MyBankingSystem.ChangeBalance(WalletId, Amount);
        }
        public static void TakeMoney(long WalletId, long Amount)
        {
            if (GetBalance(WalletId) < Amount) return;
            Amount = Amount * -1;
            MyBankingSystem.ChangeBalance(WalletId, Amount);
        }
    }
}
