using System;
using System.Collections.Generic;
using System.Text;
using Wist.Client.DataModel.Model;
using Wist.Core.Architecture;

namespace Wist.Client.DataModel.Services
{
    [ServiceContract]
    public interface IDataAccessService
    {
        void Initialize();
        void UpdateLastSyncBlock(ulong height, byte[] hash);
        bool GetLastSyncBlock(out ulong height, out byte[] hash);
        void UpdateLastRegistryCombinedBlock(ulong height, byte[] content);
        bool GetLastRegistryCombinedBlock(out ulong height, out byte[] content);
        void StoreOutcomingTransactionalBlock(ulong syncBlockHeight, ulong combinedRegistryBlockHeight, ulong blockHeight, ushort blockType, Span<byte> owner, ulong tagId, Span<byte> content, Span<byte> target);
        void StoreOutcomingTransitionTransactionalBlock(ulong syncBlockHeight, ulong combinedRegistryBlockHeight, ulong blockHeight, ushort blockType, Span<byte> owner, ulong tagId, Span<byte> content, Span<byte> transactionKey, Span<byte> commitment, Span<byte> destinationKey);
        void StoreIncomingTransactionalBlock(ulong syncBlockHeight, ulong combinedRegistryBlockHeight, ulong blockHeight, ushort blockType, Span<byte> owner, ulong tagId, Span<byte> content, Span<byte> target);
        void StoreIncomingTransitionTransactionalBlock(ulong syncBlockHeight, ulong combinedRegistryBlockHeight, ulong blockHeight, ushort blockType, Span<byte> owner, ulong tagId, Span<byte> content, Span<byte> transactionKey, Span<byte> commitment, Span<byte> destinationKey);
        void StoreOutcomingUtxoTransactionBlock(ulong syncBlockHeight, ulong combinedRegistryBlockHeight, ushort blockType, ulong tagId, Span<byte> content, Span<byte> transactionKey, Span<byte> keyImage, Span<byte> commitment, Span<byte> destinationKey);
        void StoreOutcomingTransitionUtxoTransactionBlock(ulong syncBlockHeight, ulong combinedRegistryBlockHeight, ushort blockType, ulong tagId, Span<byte> content, Span<byte> transactionKey, Span<byte> keyImage, Span<byte> commitment, Span<byte> target);
        void StoreIncomingUtxoTransactionBlock(ulong syncBlockHeight, ulong combinedRegistryBlockHeight, ushort blockType, ulong tagId, Span<byte> content, Span<byte> transactionKey, Span<byte> keyImage, Span<byte> commitment, Span<byte> destinationKey);
        void StoreIncomingTransitionUtxoTransactionBlock(ulong syncBlockHeight, ulong combinedRegistryBlockHeight, ushort blockType, ulong tagId, Span<byte> content, Span<byte> keyImage, Span<byte> commitment, Span<byte> target);
        void StoreUtxoUnspentOutputs(ulong syncBlockHeight, ulong combinedRegistryBlockHeight, ushort blockType, Span<byte> content, Span<byte> transactionKey, Span<byte> keyImage, ulong tagId, Span<byte> commitment, Span<byte> destinationKey, ulong amount, Span<byte> assetId);
        List<ulong> GetUtxoUnspentBlockTagIds();
        List<UtxoUnspentBlock> GetUtxoUnspentBlocksByTagId(ulong tagId);
        List<TransactionalIncomingBlock> GetIncomingBlocksByBlockType(ushort blockType);
        void StoreUtxoOutput(ulong tagId, Span<byte> commitment, Span<byte> destinationKey);
        int GetTotalUtxoOutputsAmount(ulong tagId);
        void GetUtxoOutputByIndex(out Span<byte> commitment, out Span<byte> destinationKey, ulong tagId, int index);
    }
}
