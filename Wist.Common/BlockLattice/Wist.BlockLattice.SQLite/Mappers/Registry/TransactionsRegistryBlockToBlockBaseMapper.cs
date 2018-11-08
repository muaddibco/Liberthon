using Wist.BlockLattice.Core.DataModel;
using Wist.BlockLattice.Core.DataModel.Registry;
using Wist.BlockLattice.Core.Enums;
using Wist.BlockLattice.Core.Parsers;
using Wist.BlockLattice.DataModel;
using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;
using Wist.Core.Translators;

namespace Wist.BlockLattice.SQLite.Mappers.Registry
{
    [RegisterExtension(typeof(ITranslator), Lifetime = LifetimeManagement.Singleton)]
    public class TransactionsRegistryBlockToBlockBaseMapper : TranslatorBase<TransactionsRegistryBlock, BlockBase>
    {
        private readonly IBlockParsersRepository _blockParsersRepository;

        public TransactionsRegistryBlockToBlockBaseMapper(IBlockParsersRepositoriesRepository blockParsersFactoriesRepository)
        {
            _blockParsersRepository = blockParsersFactoriesRepository.GetBlockParsersRepository(PacketType.Registry);
        }

        public override BlockBase Translate(TransactionsRegistryBlock obj)
        {
            if(obj == null)
            {
                return null;
            }

            IBlockParser blockParser = _blockParsersRepository.GetInstance(BlockTypes.Registry_FullBlock);

            RegistryFullBlock registryFullBlock = (RegistryFullBlock)blockParser.Parse(obj.Content);

            return registryFullBlock;
        }
    }
}
