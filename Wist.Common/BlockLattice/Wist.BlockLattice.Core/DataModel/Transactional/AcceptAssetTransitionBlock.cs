using System;
using System.Collections.Generic;
using System.Text;
using Wist.BlockLattice.Core.Enums;
using Wist.Core.Identity;

namespace Wist.BlockLattice.Core.DataModel.Transactional
{
    public class AcceptAssetTransitionBlock : TransactionalBlockBase
    {
        public override ushort Version => 1;

        public override ushort BlockType => BlockTypes.Transaction_AcceptAssetTransition;

        public byte[] AcceptedTransactionKey { get; set; }

        public byte[] AcceptedCommitment { get; set; }

        public byte[] AcceptedBlindingFactor { get; set; }

        public byte[] AcceptedAssetId { get; set; }
    }
}
