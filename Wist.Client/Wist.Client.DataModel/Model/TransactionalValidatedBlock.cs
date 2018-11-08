using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Wist.Client.DataModel.Model
{
    [Table("transactional_validated_blocks")]
    public class TransactionalValidatedBlock
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public ulong TransactionalValidatedBlockId { get; set; }

        public ulong SyncBlockHeight { get; set; }

        public ulong CombinedRegistryBlockHeight { get; set; }

        public ulong Height { get; set; }

        public ushort BlockType { get; set; }

        public Identity Owner { get; set; }

        public ulong TagId { get; set; }

        public byte[] Content { get; set; }

        public BlockHash ThisBlockHash { get; set; }
    }
}
