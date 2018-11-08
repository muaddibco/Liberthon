using System;
using System.Collections.Generic;
using System.Linq;
using Wist.BlockLattice.Core.DAL.Keys;
using Wist.BlockLattice.Core.DataModel;
using Wist.BlockLattice.Core.DataModel.Synchronization;
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
    public class SynchronizationDataService : IChainDataService
    {
        private readonly ITranslatorsRepository _translatorsRepository;

        public SynchronizationDataService(ITranslatorsRepository translatorsRepository)
        {
            _translatorsRepository = translatorsRepository;
        }

        public PacketType PacketType => PacketType.Synchronization;

        public void Add(BlockBase item)
        {
            if (item is SynchronizationConfirmedBlock synchronizationConfirmedBlock)
            {
                DataAccessService.Instance.AddSynchronizationBlock(synchronizationConfirmedBlock.BlockHeight, DateTime.Now, synchronizationConfirmedBlock.ReportedTime, synchronizationConfirmedBlock.RawData.ToArray());
            }

            if(item is SynchronizationRegistryCombinedBlock combinedBlock)
            {
                DataAccessService.Instance.AddSynchronizationRegistryCombinedBlock(combinedBlock.BlockHeight, combinedBlock.SyncBlockHeight, combinedBlock.BlockHeight, combinedBlock.RawData.ToArray());
            }
        }

        public bool AreServiceActionsAllowed(IKey key)
        {
            return true;
        }

        public BlockBase Get(IDataKey key)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<BlockBase> GetAll()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<BlockBase> GetAll(IDataKey key)
        {
            if(key is BlockTypeLowHeightKey blockTypeLowHeightKey)
            {

                if (blockTypeLowHeightKey.BlockType == BlockTypes.Synchronization_ConfirmedBlock)
                {
                    return DataAccessService.Instance.GetAllLastSynchronizationBlocks(blockTypeLowHeightKey.Height).Select(b => _translatorsRepository.GetInstance<SynchronizationBlock, BlockBase>().Translate(b));
                }
                else if(blockTypeLowHeightKey.BlockType == BlockTypes.Synchronization_RegistryCombinationBlock)
                {
                    return DataAccessService.Instance.GetAllLastRegistryCombinedBlocks(blockTypeLowHeightKey.Height).Select(b => _translatorsRepository.GetInstance<RegistryCombinedBlock, BlockBase>().Translate(b));
                }
            }
            else if(key is BlockTypeKey blockTypeKey)
            {
                if (blockTypeKey.BlockType == BlockTypes.Synchronization_ConfirmedBlock)
                {
                    return DataAccessService.Instance.GetAllSynchronizationBlocks().Select(b => _translatorsRepository.GetInstance<SynchronizationBlock, BlockBase>().Translate(b));
                }
                else if (blockTypeKey.BlockType == BlockTypes.Synchronization_RegistryCombinationBlock)
                {
                    return DataAccessService.Instance.GetAllRegistryCombinedBlocks().Select(b => _translatorsRepository.GetInstance<RegistryCombinedBlock, BlockBase>().Translate(b));
                }
            }

            return null;
        }

        public IEnumerable<BlockBase> GetAllByKey(IKey key)
        {
            throw new NotImplementedException();
        }

        public List<BlockBase> GetAllLastBlocksByType(ushort blockType)
        {
            switch (blockType)
            {
                case BlockTypes.Synchronization_RegistryCombinationBlock:
                    {
                        RegistryCombinedBlock block = DataAccessService.Instance.GetLastRegistryCombinedBlock();
                        return new List<BlockBase> { _translatorsRepository.GetInstance<RegistryCombinedBlock, BlockBase>().Translate(block) };
                    }
                case BlockTypes.Synchronization_ConfirmedBlock:
                    {
                        SynchronizationBlock block = DataAccessService.Instance.GetLastSynchronizationBlock();
                        return  new List<BlockBase> { _translatorsRepository.GetInstance<SynchronizationBlock, BlockBase>().Translate(block) };
                    }
                default:
                    return null;
            }
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
        }

        public void Update(IDataKey key, BlockBase item)
        {
            throw new NotImplementedException();
        }
    }
}
