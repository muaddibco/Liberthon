using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wist.Client.DataModel.Model
{
    [Table("transactional_owtcoming_blocks")]
    public class TransactionalOutcomingBlock
    {
        public ulong TransactionalOutcomingBlockId { get; set; }

        public ulong SyncBlockHeight { get; set; }

        public ulong CombinedRegistryBlockHeight { get; set; }

        public ulong Height { get; set; }

        public ushort BlockType { get; set; }

        public Identity Owner { get; set; }

        public Identity Target { get; set; }

        public TransactionalIncomingBlock PrecendantBlock { get; set; }

        public TransactionalIncomingBlock AscendantBlock { get; set; }

        public ulong TagId { get; set; }

        public byte[] Content { get; set; }

        public BlockHash ThisBlockHash { get; set; }

        public BlockHash PrevBlockHash { get; set; }

        #region Transition Account based to UTXO based transaction

        public bool IsTransition { get; set; }

        public UtxoTransactionKey TransactionKey { get; set; }

        public UtxoOutput Output { get; set; }

        #endregion Transition Account based to UTXO based transaction
    }
}
