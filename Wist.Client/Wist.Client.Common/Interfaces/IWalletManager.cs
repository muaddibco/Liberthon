using System.Collections;
using System.Collections.Generic;
using Wist.BlockLattice.Core.DataModel;
using Wist.BlockLattice.Core.DataModel.Registry;
using Wist.BlockLattice.Core.DataModel.Transactional;
using Wist.BlockLattice.Core.DataModel.UtxoConfidential;
using Wist.Client.Common.Entities;
using Wist.Core.Architecture;

namespace Wist.Client.Common.Interfaces
{
    [ServiceContract]
    public interface IWalletManager
    {
        void InitializeNetwork();

        bool SendFunds(uint amount, Account receiver);

        bool IssueAssets(string issuanceInfo, byte[][] assetIds, string[] assetInfos, ulong tagId);

        bool SendAssetToUtxo(byte[][] assetIds, int index, ulong tagId, ConfidentialAccount receiver, byte[] sk = null);

        bool SendAssetTransition(byte[] assetId, byte[] transactionKey, byte[] assetCommitment, byte[] prevDestinationKey, ulong tagId, Account target);

        bool SendAcceptAsset(byte[] transactionKey, byte[] assetCommitment, byte[] blindingFactor, byte[] assetId, ulong tagId);
    }
}