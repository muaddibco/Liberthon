using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Wist.Client.DataModel.Model
{
    [Table("utxo_outcoming_blocks")]
    public class UtxoOutcomingBlock
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public ulong UtxoOutcomingBlockId { get; set; }

        public ulong SyncBlockHeight { get; set; }

        public ulong RegistryCombinedBlockHeight { get; set; }

        public ushort BlockType { get; set; }

        public ulong TagId { get; set; }

        public byte[] Content { get; set; }

        public UtxoTransactionKey TransactionKey { get; set; }

        public UtxoKeyImage KeyImage { get; set; }

        public UtxoOutput Output { get; set; }

        public virtual ICollection<UtxoIncomingBlock> PrecendantBlocks { get; set; }
    }
}
