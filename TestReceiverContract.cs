using System;
using System.ComponentModel;
using System.Numerics;
using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;

#nullable enable

namespace DevHawk.SampleContracts
{
    [DisplayName("TestReceiver")]
    [ManifestExtra("Author", "Harry Pierson")]
    [ManifestExtra("Email", "harrypierson@hotmail.com")]
    [ManifestExtra("Description", "This is an example contract")]
    [ManifestExtra("GitHubRepo", "https://github.com/ngdenterprise/neo-test-receiver")]
    [ContractPermission("*", "transfer")]
    public class TestReceiverContract : SmartContract
    {
        // using an extern ByteString property allows the contract to specify a single byte 
        // storage key or prefix without a costly byte[] -> ByteString CONVERT operation
        static extern ByteString Key_ContractOwner { [OpCode(OpCode.PUSHDATA1, "01FF")] get; }

        public static void OnNEP17Payment(UInt160? from, BigInteger amount, object _)
        {
            if (amount <= 0) throw new Exception("Invalid payment amount");

#if DEBUG
            // Note, this is in a DEBUG conditional compilation block
            // In a production contract, it would be wasteful (in contract GAS charge)
            // to retrieve the token symbol, format the amount as a decimal or the account
            // as an address for a Log call
            Runtime.Log($"Received {ToDecimal(Runtime.CallingScriptHash, amount)} {TokenSymbol(Runtime.CallingScriptHash)} from {ToAddress(from)}");
#endif
        }

        public static bool Withdraw(UInt160 scriptHash, UInt160 receiver, BigInteger amount)
        {
            ValidateOwner("Only the contract owner can withdraw tokens");
            if (receiver == UInt160.Zero || !receiver.IsValid)
                throw new Exception("Invalid withrdrawl address");

            var transferResult = Nep17Transfer(scriptHash, Runtime.ExecutingScriptHash, receiver, amount);
#if DEBUG
            // Note, this is in a DEBUG conditional compilation block
            // In a production contract, it would be wasteful (in contract GAS charge)
            // to retrieve the token symbol or format the account as an address
            // for a Log call
            if (transferResult)
            {
                Runtime.Log($"Withdrew {ToDecimal(scriptHash, amount)} {TokenSymbol(scriptHash)} to {ToAddress(receiver)}");
            }
#endif
            return transferResult;
        }

        [DisplayName("_deploy")]
        public static void Deploy(object _, bool update)
        {
            if (update) return;

            var tx = (Transaction)Runtime.ScriptContainer;
            Storage.Put(Storage.CurrentContext, Key_ContractOwner, tx.Sender);
        }

        public static void Update(ByteString nefFile, string manifest)
        {
            ValidateOwner("Only the contract owner can update the contract");
            ContractManagement.Update(nefFile, manifest, null);
        }

        static void ValidateOwner(string? message = null)
        {
            message ??= "Only the contract owner can do this";
            var owner = (UInt160)Storage.Get(Storage.CurrentContext, Key_ContractOwner);
            if (!Runtime.CheckWitness(owner))
                throw new Exception(message);
        }

#if DEBUG
        // Note, these methods are in a DEBUG conditional compilation block.
        // These methods are GAS wasteful and should not be used on a production
        // Neo N3 blockchain like MainNet 

        static string ToAddress(UInt160? account) 
        {
            // Since '0' is not a valid base58 encoding character, there's no danger
            // that "N000000000000000000000000000000000" will be interpreted as a
            // valid Neo N3 address.
            if (account is null
                || account.IsZero
                || !account.IsValid) return "N000000000000000000000000000000000";

            var prefix = (ByteString)(new byte[] { Runtime.AddressVersion });
            return StdLib.Base58CheckEncode(prefix.Concat(account));
        }

        static string ToDecimal(UInt160 scriptHash, BigInteger amount)
        {
            var decimals = TokenDecimals(scriptHash);
            var str = StdLib.Itoa(amount);
            if (decimals == 0) return str;
            if (decimals < str.Length) 
            {
                var len = str.Length - decimals;
                var str1 = str[..len];
                var str2 = str[len..];
                return $"{str1}.{str2}";
            }
            if (decimals == str.Length)
            {
                return $".{str}";
            }

            var zeros = "";
            for (int i = 0; i < decimals - str.Length; i++)
            {
                zeros += '0';
            }
            return $".{zeros}{str}";
        }

        static string TokenSymbol(UInt160 scriptHash)
        {
            return (string)Contract.Call(scriptHash, "symbol", CallFlags.ReadOnly);
        }

        static byte TokenDecimals(UInt160 scriptHash)
        {
            return (byte)Contract.Call(scriptHash, "decimals", CallFlags.ReadOnly);
        }
#endif

        static bool Nep17Transfer(UInt160 scriptHash, UInt160 sender, UInt160 receiver, BigInteger amount, object? data = null)
        {
            return (bool)Contract.Call(scriptHash, "transfer", CallFlags.All, sender, receiver, amount, data);
        }
    }
}
