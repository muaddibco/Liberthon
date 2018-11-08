using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Wist.Client.DataModel.Model
{
    [Table("registry_combined_blocks")]
    public class RegistryCombinedBlock
    {
        public ulong RegistryCombinedBlockId { get; set; }

        public byte[] Content { get; set; }
    }
}
