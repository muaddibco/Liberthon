using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Wist.Client.DataModel.Model
{
    [Table("utxo_transaction_keys")]
    public class UtxoTransactionKey
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public ulong UtxoTransactionKeyId { get; set; }

        public byte[] Key { get; set; }
    }
}
