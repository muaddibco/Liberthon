using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Wist.Client.DataModel.Model
{
    [Table("utxo_outputs")]
    public class UtxoOutput
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public ulong UtxoOutputId { get; set; }

        public ulong TagId { get; set; }

        public byte[] DestinationKey { get; set; }

        public byte[] Commitment { get; set; }
    }
}
