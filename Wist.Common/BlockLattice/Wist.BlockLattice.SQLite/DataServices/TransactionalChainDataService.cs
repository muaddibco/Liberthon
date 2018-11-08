using System;
using System.Collections.Generic;
using Wist.BlockLattice.Core.Enums;
using Wist.BlockLattice.Core.DataModel.Transactional;
using Wist.BlockLattice.Core.Interfaces;
using Wist.BlockLattice.DataModel;
using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;
using Wist.BlockLattice.Core.DataModel;
using Wist.Core.Translators;
using Wist.Core.Identity;
using Wist.BlockLattice.Core.Serializers;
using Wist.BlockLattice.SQLite.DataAccess;
using Wist.BlockLattice.Core.DAL.Keys;

namespace Wist.BlockLattice.SQLite.DataServices
{
    [RegisterExtension(typeof(IChainDataService), Lifetime = LifetimeManagement.Singleton)]
    public class TransactionalChainDataService : IChainDataService
    {
        private readonly ITranslatorsRepository _mapperFactory;

        public PacketType PacketType => PacketType.Transactional;


        public TransactionalChainDataService(ITranslatorsRepository mapperFactory)
        {
            _mapperFactory = mapperFactory;
        }

        public void Add(BlockBase block)
        {
            if(block is TransactionalBlockBase transactionalBlockBase)
            {
                IKey key = transactionalBlockBase.Signer;

                DataAccessService.Instance.AddTransactionalBlock(key, transactionalBlockBase.SyncBlockHeight, transactionalBlockBase.BlockType, transactionalBlockBase.BlockHeight, transactionalBlockBase.RawData.ToArray());
            }
        }

        public bool AreServiceActionsAllowed(IKey key)
        {
            return DataAccessService.Instance.IsTransactionalIdentityExist(key);
        }

        public BlockBase[] GetAllBlocks(IKey key)
        {
            throw new NotImplementedException();
        }

        public BlockBase GetBlockByOrder(IKey key, uint order)
        {
            throw new NotImplementedException();
        }

        public BlockBase GetLastBlock(IKey key)
        {
            TransactionalBlock transactionalBlock = DataAccessService.Instance.GetLastTransactionalBlock(key);

            if (transactionalBlock != null)
            {
                ITranslator<TransactionalBlock, BlockBase> mapper = _mapperFactory.GetInstance<TransactionalBlock, BlockBase>();

                BlockBase block = mapper?.Translate(transactionalBlock);

                return block;
            }

            return null;
        }

        public IEnumerable<T> GetAllLastBlocksByType<T>() where T : BlockBase
        {
            List<T> blocks = new List<T>();

            if(typeof(T) == typeof(TransactionalBlockBase))
            {
                foreach (TransactionalIdentity transactionalIdentity in DataAccessService.Instance.GetAllTransctionalIdentities())
                {
                    TransactionalBlock transactionalBlock = DataAccessService.Instance.GetLastTransactionalBlock(transactionalIdentity);

                    ITranslator<TransactionalBlock, BlockBase> translator = _mapperFactory.GetInstance<TransactionalBlock, BlockBase>();

                    BlockBase block = translator?.Translate(transactionalBlock);

                    blocks.Add(block as T);
                }
            }

            return blocks;
        }

        public List<BlockBase> GetAllLastBlocksByType(ushort blockType)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<BlockBase> GetAllByKey(IKey key)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<BlockBase> GetAll()
        {
            throw new NotImplementedException();
        }

        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public BlockBase Get(IDataKey key)
        {
            if(key is SyncHashKey syncHashKey)
            {
                TransactionalBlock transactionalBlock = DataAccessService.Instance.GetTransactionalBySyncAndHash(syncHashKey.SyncBlockHeight, syncHashKey.Hash);

                return _mapperFactory.GetInstance<TransactionalBlock, BlockBase>().Translate(transactionalBlock);
            }

            return null;
        }

        public void Update(IDataKey key, BlockBase item)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<BlockBase> GetAll(IDataKey key)
        {
            throw new NotImplementedException();
        }
    }
}
