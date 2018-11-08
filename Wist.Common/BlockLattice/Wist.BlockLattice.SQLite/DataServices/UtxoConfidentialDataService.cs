using System;
using System.Collections.Generic;
using Wist.BlockLattice.Core.DAL.Keys;
using Wist.BlockLattice.Core.DataModel;
using Wist.BlockLattice.Core.DataModel.UtxoConfidential;
using Wist.BlockLattice.Core.Enums;
using Wist.BlockLattice.Core.Interfaces;
using Wist.BlockLattice.DataModel;
using Wist.BlockLattice.SQLite.DataAccess;
using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;
using Wist.Core.Identity;
using Wist.Core.Translators;

namespace Wist.BlockLattice.SQLite.DataServices
{
    [RegisterExtension(typeof(IChainDataService), Lifetime = LifetimeManagement.Singleton)]
    public class UtxoConfidentialDataService : IChainDataService
    {
        private readonly ITranslatorsRepository _mapperFactory;
        public PacketType PacketType => PacketType.UtxoConfidential;

        public UtxoConfidentialDataService(ITranslatorsRepository mapperFactory)
        {
            _mapperFactory = mapperFactory;
        }

        public void Add(BlockBase item)
        {
            if(item is UtxoConfidentialBase utxoConfidential)
            {
                DataAccessService.Instance.AddUtxoConfidentialBlock(utxoConfidential.KeyImage, utxoConfidential.SyncBlockHeight, utxoConfidential.BlockType, utxoConfidential.DestinationKey, utxoConfidential.RawData.ToArray());
            }
        }

        public bool AreServiceActionsAllowed(IKey key)
        {
            return !DataAccessService.Instance.IsUtxoConfidentialImageKeyExist(key);
        }

        public BlockBase Get(IDataKey key)
        {
            if (key is SyncHashKey syncHashKey)
            {
                UtxoConfidentialBlock utxoConfidential = DataAccessService.Instance.GetUtxoConfidentialBySyncAndHash(syncHashKey.SyncBlockHeight, syncHashKey.Hash);

                return _mapperFactory.GetInstance<UtxoConfidentialBlock, BlockBase>().Translate(utxoConfidential);
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
