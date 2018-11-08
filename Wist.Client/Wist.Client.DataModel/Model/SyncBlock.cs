using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Wist.Client.DataModel.Model
{
    [Table("sync_blocks")]
    public class SyncBlock
    {
        [Key]
        public ulong SyncBlockId { get; set; }

        public byte[] Hash { get; set; }
    }
}
