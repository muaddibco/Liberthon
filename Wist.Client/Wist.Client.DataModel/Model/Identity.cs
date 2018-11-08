using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Wist.Client.DataModel.Model
{
    [Table("identities")]
    public class Identity
    {
        [Key]
        [DatabaseGenerated( DatabaseGeneratedOption.Identity)]
        public ulong IdentityId { get; set; }

        public byte[] Key { get; set; }
    }
}
