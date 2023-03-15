using System;
using System.ComponentModel;
using System.Numerics;
using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;

namespace DevHawk.SampleContracts
{
    [DisplayName("TestReceiver")]
    [ManifestExtra("Author", "Harry Pierson")]
    [ManifestExtra("Email", "harrypierson@hotmail.com")]
    [ManifestExtra("Description", "This is an example contract")]
    public class TestReceiverContract : SmartContract
    {
        public delegate void OnReceiveNep17Delegate(UInt160 from, UInt160 tokenHash, BigInteger amount);

        [DisplayName("ReceiveNep17")]
        public static event OnReceiveNep17Delegate OnReceiveNep17 = default!;

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
            OnReceiveNep17(from, Runtime.CallingScriptHash, amount);       
        }

        public static BigInteger BalanceOf(UInt160 tokenHash)
        {
            var key = PREFIX_NEP17.Concat(tokenHash);
            var balance = (BigInteger)Storage.Get(Storage.CurrentContext, key);
            return balance;
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
            var contractOwner = (UInt160)Storage.Get(Storage.CurrentContext, Key_ContractOwner);
            if (!Runtime.CheckWitness(contractOwner))
            {
                throw new Exception("Only the contract owner can update the contract");
            }
            ContractManagement.Update(nefFile, manifest, null);
        }

    }
}
