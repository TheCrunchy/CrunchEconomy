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
        public static void AddMoney(long WalletId, long amount)
        {
            MyBankingSystem.ChangeBalance(WalletId, amount);
        }
        public static void TakeMoney(long walletID, long amount)
        {
            if (GetBalance(walletID) < amount) return;
            amount = amount * -1;
            MyBankingSystem.ChangeBalance(walletID, amount);
        }
    }
}
