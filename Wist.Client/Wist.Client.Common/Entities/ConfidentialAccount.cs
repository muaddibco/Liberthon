using System;
using System.Collections.Generic;
using System.Text;
using Wist.BlockLattice.Core.DataModel;

namespace Wist.Client.Common.Entities
{
    public class ConfidentialAccount : AccountBase
    {
        public byte[] PublicViewKey { get; set; }

        public byte[] PublicSpendKey { get; set; }
    }
}
