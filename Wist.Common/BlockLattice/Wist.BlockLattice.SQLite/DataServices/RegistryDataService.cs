using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wist.BlockLattice.Core;
using Wist.BlockLattice.Core.DAL.Keys;
using Wist.BlockLattice.Core.DataModel;
using Wist.BlockLattice.Core.DataModel.Registry;
using Wist.BlockLattice.Core.Enums;
using Wist.BlockLattice.Core.Interfaces;
using Wist.BlockLattice.DataModel;
using Wist.BlockLattice.SQLite.DataAccess;
using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;
using Wist.Core.ExtensionMethods;
using Wist.Core.HashCalculations;
using Wist.Core.Identity;
using Wist.Core.Translators;

namespace Wist.BlockLattice.SQLite.DataServices
{
    [RegisterExtension(typeof(IChainDataService), Lifetime = LifetimeManagement.Singleton)]
    public class RegistryDataService : IChainDataService
    {
        private readonly ITranslatorsRepository _translatorsRepository;
        private readonly IHashCalculation _defaultHashCalculation;

        public RegistryDataService(ITranslatorsRepository translatorsRepository, IHashCalculationsRepository hashCalculationsRepository)
        {
            _translatorsRepository = translatorsRepository;
            _defaultHashCalculation = hashCalculationsRepository.Create(Globals.DEFAULT_HASH);
        }

        public PacketType PacketType => PacketType.Registry;

        public void Add(BlockBase item)
        {
            if(item is RegistryFullBlock block)
            {
                //TODO: shardId must be taken from somewhere
                DataAccessService.Instance.AddRegistryFullBlock(block.SyncBlockHeight, block.BlockHeight, block.TransactionHeaders?.Count??0, block.RawData.ToArray(), _defaultHashCalculation.CalculateHash(block.RawData));
            }
        }

        public bool AreServiceActionsAllowed(IKey key)
        {
            throw new NotImplementedException();
        }

        public BlockBase Get(IDataKey key)
        {
            if(key is DoubleHeightKey heightKey)
            {
                TransactionsRegistryBlock transactionsRegistryBlock = DataAccessService.Instance.GetTransactionsRegistryBlock(heightKey.Height1, heightKey.Height2);

                BlockBase blockBase = _translatorsRepository.GetInstance<TransactionsRegistryBlock, BlockBase>().Translate(transactionsRegistryBlock);

                return blockBase;
            }
            else if(key is SyncHashKey syncTransactionKey)
            {
                List<TransactionsRegistryBlock> transactionsRegistryBlocks = DataAccessService.Instance.GetTransactionsRegistryBlocks(syncTransactionKey.SyncBlockHeight);

                //TODO: !!! move storing default hash into database for reducing computational costs
                TransactionsRegistryBlock transactionsRegistryBlock = transactionsRegistryBlocks.FirstOrDefault(t => syncTransactionKey.Hash.Equals32(_defaultHashCalculation.CalculateHash(t.Content)));

                BlockBase blockBase = _translatorsRepository.GetInstance<TransactionsRegistryBlock, BlockBase>().Translate(transactionsRegistryBlock);

                return blockBase;
            }

            return null;
        }

        public IEnumerable<BlockBase> GetAll()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<BlockBase> GetAll(IDataKey key)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<BlockBase> GetAllByKey(IKey key)
        {
            throw new NotImplementedException();
        }

        public List<BlockBase> GetAllLastBlocksByType(ushort blockType)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> GetAllLastBlocksByType<T>() where T : BlockBase
        {
            throw new NotImplementedException();
        }

        public BlockBase GetBlockByOrder(IKey key, uint order)
        {
            throw new NotImplementedException();
        }

        public BlockBase GetLastBlock(IKey key)
        {
            throw new NotImplementedException();
        }

        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public void Update(IDataKey key, BlockBase item)
        {
            throw new NotImplementedException();
        }
    }
}
