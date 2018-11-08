using Wist.BlockLattice.Core.DataModel;
using Wist.BlockLattice.Core.Parsers;
using Wist.BlockLattice.DataModel;
using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;
using Wist.Core.Translators;

namespace Wist.BlockLattice.SQLite.Mappers.Transactional
{
    [RegisterExtension(typeof(ITranslator), Lifetime = LifetimeManagement.Singleton)]
    public class TransactionalBlockToBaseBlockMapper : TransactionalMapperBase<TransactionalBlock, BlockBase>
    {
        public TransactionalBlockToBaseBlockMapper(IBlockParsersRepositoriesRepository blockParsersFactoriesRepository)
            : base(blockParsersFactoriesRepository)
        {
        }

        public override BlockBase Translate(TransactionalBlock transactionalBlock)
        {
            if(transactionalBlock == null)
            {
                return null;
            }

            BlockBase transactionalBlockBase = null;

            ushort blockType = transactionalBlock.BlockType;

            IBlockParser blockParser = null;

            blockParser = _blockParsersRepository.GetInstance(blockType);

            transactionalBlockBase = blockParser.Parse(transactionalBlock.BlockContent);

            return transactionalBlockBase;
        }
    }
}
