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
    [ContractPermission("*", "transfer")]
    public class TestReceiverContract : SmartContract
    {
        [InitialValue("0x17", ContractParameterType.ByteArray)]
        private static readonly ByteString PREFIX_NEP17 = default!;

        [InitialValue("0xFF", ContractParameterType.ByteArray)]
        private static readonly ByteString Key_ContractOwner = default!;

        public static void OnNEP17Payment(UInt160 from, BigInteger amount, object data)
        {
            if (amount <= 0) throw new Exception("Invalid payment amount");

            var key = PREFIX_NEP17.Concat(Runtime.CallingScriptHash);
            var balance = (BigInteger)Storage.Get(Storage.CurrentContext, key);
            Storage.Put(Storage.CurrentContext, key, balance + amount);
        }

        public bool Withdraw(UInt160 scriptHash, UInt160 receiver, BigInteger amount)
        {
            ValidateOwner("Only the contract owner can withdraw tokens");
            if (receiver == UInt160.Zero || !receiver.IsValid)
                throw new Exception("Invalid withrdrawl address");

            return Nep17Transfer(scriptHash, Runtime.ExecutingScriptHash, receiver, amount);
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

        static bool Nep17Transfer(UInt160 scriptHash, UInt160 sender, UInt160 receiver, BigInteger amount, object? data = null)
        {
            return (bool)Contract.Call(scriptHash, "transfer", CallFlags.All, sender, receiver, amount, data);
        }
    }
}
