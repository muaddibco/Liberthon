using Wist.BlockLattice.Core.Enums;

namespace Wist.BlockLattice.Core.DataModel.Transactional
{
    public class IssueAssetsBlock : TransactionalBlockBase
    {
        public override ushort Version => 1;

        public override ushort BlockType => BlockTypes.Transaction_IssueAssets;

        public byte[][] IssuedAssetIds { get; set; }

        public string[] IssuedAssetInfo { get; set; }

        public string IssuanceInfo { get; set; }
    }
}