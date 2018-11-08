using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Wist.Client.DataModel.Model
{
    [Table("utxo_unspent_blocks")]
    public class UtxoUnspentBlock
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public ulong UtxoUnspentBlockId { get; set; }

        public ulong SyncBlockHeight { get; set; }

        public ulong RegistryCombinedBlockHeight { get; set; }

        public ushort BlockType { get; set; }

        public ulong TagId { get; set; }

        public byte[] TransactionKey { get; set; }

        public byte[] KeyImage { get; set; }

        public byte[] Content { get; set; }

        public UtxoOutput Output { get; set; }

        public ulong Amount { get; set; }

        public byte[] AssetId { get; set; }
    }
}
