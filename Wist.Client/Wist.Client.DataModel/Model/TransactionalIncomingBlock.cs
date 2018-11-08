using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Wist.Client.DataModel.Model
{
    [Table("transactional_incoming_blocks")]
    public class TransactionalIncomingBlock
    {
        public ulong TransactionalIncomingBlockId { get; set; }

        public ulong SyncBlockHeight { get; set; }

        public ulong CombinedRegistryBlockHeight { get; set; }

        public ulong Height { get; set; }

        public ushort BlockType { get; set; }

        public Identity Owner { get; set; }

        public Identity Target { get; set; }

        public TransactionalIncomingBlock PrecendantBlock { get; set; }

        public TransactionalIncomingBlock AscendantBlock { get; set; }

        public TransactionalIncomingBlock DependantBlock { get; set; }

        public ulong TagId { get; set; }

        public byte[] Content { get; set; }

        public BlockHash ThisBlockHash { get; set; }

        public BlockHash PrevBlockHash { get; set; }

        #region Transition Account based to UTXO based transaction

        public bool IsTransition { get; set; }

        public UtxoTransactionKey TransactionKey { get; set; }

        public UtxoOutput Output { get; set; }

        #endregion Transition Account based to UTXO based transaction

        public bool IsVerified { get; set; }

        public bool IsCorrect { get; set; }
    }
}
