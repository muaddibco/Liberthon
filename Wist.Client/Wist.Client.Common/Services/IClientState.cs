using System;
using System.Collections.Generic;
using System.Text;
using Wist.Core.States;

namespace Wist.Client.Common.Services
{
    public interface IClientState : IState
    {
        void InitializeAccountBased(byte[] secretKey);
        void InitializeConfidential(byte[] secretViewKey, byte[] secretSpendKey);
        bool IsConfidential();
        byte[] GetPublicKey();
        byte[] GetPublicKeyHash();
        byte[] GetSecretViewKey();
        byte[] GetPublicSpendKey();
        byte[] GetSecretSpendKey();
    }
}
