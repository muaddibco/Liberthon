using System;
using System.Collections.Generic;
using System.Linq;
using Wist.BlockLattice.Core;
using Wist.Client.DataModel.Configuration;
using Wist.Client.DataModel.Model;
using Wist.Core;
using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;
using Wist.Core.Configuration;
using Wist.Core.ExtensionMethods;
using Wist.Core.HashCalculations;

namespace Wist.Client.DataModel.Services
{
    [RegisterDefaultImplementation(typeof(IDataAccessService), Lifetime = LifetimeManagement.Singleton)]
    public class DataAccessService : IDataAccessService
    {
        private readonly object _sync = new object();
        private readonly DataContext _dataContext;
        private readonly IHashCalculation _hashCalculation;
        private readonly Dictionary<int, ulong> _utxoOutputsIndiciesMap;

        public DataAccessService(IConfigurationService configurationService, IHashCalculationsRepository hashCalculationsRepository)
        {
            _utxoOutputsIndiciesMap = new Dictionary<int, ulong>();
            _dataContext = new DataContext(configurationService.Get<IClientDataContextConfiguration>());
            _hashCalculation = hashCalculationsRepository.Create(Globals.DEFAULT_HASH);
        }

        public bool GetLastRegistryCombinedBlock(out ulong height, out byte[] content)
        {
            lock (_sync)
            {
                RegistryCombinedBlock combinedBlock = _dataContext.RegistryCombinedBlocks.OrderBy(b => b.RegistryCombinedBlockId).FirstOrDefault();

                if(combinedBlock != null)
                {
                    height = combinedBlock.RegistryCombinedBlockId;
                    content = combinedBlock.Content;
                    return true;
                }

                height = 0;
                content = null;
                return false;
            }
        }

        public bool GetLastSyncBlock(out ulong height, out byte[] hash)
        {
            lock (_sync)
            {
                SyncBlock syncBlock = _dataContext.SyncBlocks.OrderBy(b => b.SyncBlockId).FirstOrDefault();

                if (syncBlock != null)
                {
                    height = syncBlock.SyncBlockId;
                    hash = syncBlock.Hash;
                    return true;
                }

                height = 0;
                hash = null;
                return false;
            }
        }

        public void StoreIncomingTransactionalBlock(ulong syncBlockHeight, ulong combinedRegistryBlockHeight, ulong blockHeight, ushort blockType, Span<byte> owner, ulong tagId, Span<byte> content, Span<byte> target)
        {
            TransactionalIncomingBlock transactionalIncomingBlock = new TransactionalIncomingBlock
            {
                SyncBlockHeight = syncBlockHeight,
                CombinedRegistryBlockHeight = combinedRegistryBlockHeight,
                Height = blockHeight,
                BlockType = blockType,
                TagId = tagId,
                Content = content.ToArray(),
                Owner = GetOrAddIdentity(owner),
                Target = GetOrAddIdentity(target),
                ThisBlockHash = GetOrAddBlockHash(_hashCalculation.CalculateHash(content.ToArray())),
                IsVerified = true,
                IsCorrect = true,
                IsTransition = false
            };

            lock(_sync)
            {
                _dataContext.TransactionalIncomingBlocks.Add(transactionalIncomingBlock);
                _dataContext.SaveChanges();
            }
        }

        public void StoreIncomingTransitionTransactionalBlock(ulong syncBlockHeight, ulong combinedRegistryBlockHeight, ulong blockHeight, ushort blockType, Span<byte> owner, ulong tagId, Span<byte> content, Span<byte> transactionKey, Span<byte> commitment, Span<byte> destinationKey)
        {
            TransactionalIncomingBlock transactionalIncomingBlock = new TransactionalIncomingBlock
            {
                SyncBlockHeight = syncBlockHeight,
                CombinedRegistryBlockHeight = combinedRegistryBlockHeight,
                Height = blockHeight,
                BlockType = blockType,
                TagId = tagId,
                Content = content.ToArray(),
                Owner = GetOrAddIdentity(owner),
                TransactionKey = GetOrAddUtxoTransactionKey(transactionKey),
                Output = GetOrAddUtxoOutput(tagId, commitment, destinationKey),
                ThisBlockHash = GetOrAddBlockHash(_hashCalculation.CalculateHash(content.ToArray())),
                IsVerified = true,
                IsCorrect = true,
                IsTransition = true
            };

            lock (_sync)
            {
                _dataContext.TransactionalIncomingBlocks.Add(transactionalIncomingBlock);
                _dataContext.SaveChanges();
            }
        }

        public void StoreIncomingTransitionUtxoTransactionBlock(ulong syncBlockHeight, ulong combinedRegistryBlockHeight, ushort blockType, ulong tagId, Span<byte> content, Span<byte> keyImage, Span<byte> commitment, Span<byte> target)
        {
            UtxoIncomingBlock utxoIncomingBlock = new UtxoIncomingBlock
            {
                SyncBlockHeight = syncBlockHeight,
                RegistryCombinedBlockHeight = combinedRegistryBlockHeight,
                BlockType = blockType,
                TagId = tagId,
                //TransactionKey = GetOrAddUtxoTransactionKey(transactionKey),
                KeyImage = GetOrAddUtxoKeyImage(keyImage),
                Output = GetOrAddUtxoOutput(tagId, commitment, target),
                Content = content.ToArray()
            };

            lock (_sync)
            {
                _dataContext.UtxoIncomingBlocks.Add(utxoIncomingBlock);
                _dataContext.SaveChanges();
            }
        }

        public void StoreIncomingUtxoTransactionBlock(ulong syncBlockHeight, ulong combinedRegistryBlockHeight, ushort blockType, ulong tagId, Span<byte> content, Span<byte> transactionKey, Span<byte> keyImage, Span<byte> commitment, Span<byte> destinationKey)
        {
            UtxoIncomingBlock utxoIncomingBlock = new UtxoIncomingBlock
            {
                SyncBlockHeight = syncBlockHeight,
                RegistryCombinedBlockHeight = combinedRegistryBlockHeight,
                BlockType = blockType,
                TagId = tagId,
                TransactionKey = GetOrAddUtxoTransactionKey(transactionKey),
                KeyImage = GetOrAddUtxoKeyImage(keyImage),
                Output = GetOrAddUtxoOutput(tagId, commitment, destinationKey),
                Content = content.ToArray()
            };

            lock (_sync)
            {
                _dataContext.UtxoIncomingBlocks.Add(utxoIncomingBlock);
                _dataContext.SaveChanges();
            }
        }

        public void StoreOutcomingTransactionalBlock(ulong syncBlockHeight, ulong combinedRegistryBlockHeight, ulong blockHeight, ushort blockType, Span<byte> owner, ulong tagId, Span<byte> content, Span<byte> target)
        {
            TransactionalOutcomingBlock transactionalOutcomingBlock = new TransactionalOutcomingBlock
            {
                SyncBlockHeight = syncBlockHeight,
                CombinedRegistryBlockHeight = combinedRegistryBlockHeight,
                Height = blockHeight,
                BlockType = blockType,
                TagId = tagId,
                Content = content.ToArray(),
                Owner = GetOrAddIdentity(owner),
                Target = GetOrAddIdentity(target),
                ThisBlockHash = GetOrAddBlockHash(_hashCalculation.CalculateHash(content.ToArray())),
                IsTransition = false
            };

            lock (_sync)
            {
                _dataContext.TransactionalOutcomingBlocks.Add(transactionalOutcomingBlock);
                _dataContext.SaveChanges();
            }
        }

        public void StoreOutcomingTransitionTransactionalBlock(ulong syncBlockHeight, ulong combinedRegistryBlockHeight, ulong blockHeight, ushort blockType, Span<byte> owner, ulong tagId, Span<byte> content, Span<byte> transactionKey, Span<byte> commitment, Span<byte> destinationKey)
        {
            TransactionalOutcomingBlock transactionalOutcomingBlock = new TransactionalOutcomingBlock
            {
                SyncBlockHeight = syncBlockHeight,
                CombinedRegistryBlockHeight = combinedRegistryBlockHeight,
                Height = blockHeight,
                BlockType = blockType,
                TagId = tagId,
                Content = content.ToArray(),
                Owner = GetOrAddIdentity(owner),
                Output = GetOrAddUtxoOutput(tagId, commitment, destinationKey),
                ThisBlockHash = GetOrAddBlockHash(_hashCalculation.CalculateHash(content.ToArray())),
                IsTransition = false
            };

            lock (_sync)
            {
                _dataContext.TransactionalOutcomingBlocks.Add(transactionalOutcomingBlock);
                _dataContext.SaveChanges();
            }
        }

        public void StoreOutcomingTransitionUtxoTransactionBlock(ulong syncBlockHeight, ulong combinedRegistryBlockHeight, ushort blockType, ulong tagId, Span<byte> content, Span<byte> transactionKey, Span<byte> keyImage, Span<byte> commitment, Span<byte> target)
        {
            UtxoOutcomingBlock utxoOutcomingBlock = new UtxoOutcomingBlock
            {
                SyncBlockHeight = syncBlockHeight,
                RegistryCombinedBlockHeight = combinedRegistryBlockHeight,
                BlockType = blockType,
                TagId = tagId,
                TransactionKey = GetOrAddUtxoTransactionKey(transactionKey),
                KeyImage = GetOrAddUtxoKeyImage(keyImage),
                Output = GetOrAddUtxoOutput(tagId, commitment, target),
                Content = content.ToArray()
            };

            lock (_sync)
            {
                _dataContext.UtxoOutcomingBlocks.Add(utxoOutcomingBlock);
                _dataContext.SaveChanges();
            }
        }

        public void StoreOutcomingUtxoTransactionBlock(ulong syncBlockHeight, ulong combinedRegistryBlockHeight, ushort blockType, ulong tagId, Span<byte> content, Span<byte> transactionKey, Span<byte> keyImage, Span<byte> commitment, Span<byte> destinationKey)
        {
            UtxoOutcomingBlock utxoOutcomingBlock = new UtxoOutcomingBlock
            {
                SyncBlockHeight = syncBlockHeight,
                RegistryCombinedBlockHeight = combinedRegistryBlockHeight,
                BlockType = blockType,
                TagId = tagId,
                TransactionKey = GetOrAddUtxoTransactionKey(transactionKey),
                KeyImage = GetOrAddUtxoKeyImage(keyImage),
                Output = GetOrAddUtxoOutput(tagId, commitment, destinationKey),
                Content = content.ToArray()
            };

            lock (_sync)
            {
                _dataContext.UtxoOutcomingBlocks.Add(utxoOutcomingBlock);
                _dataContext.SaveChanges();
            }
        }

        public void StoreUtxoUnspentOutputs(ulong syncBlockHeight, ulong combinedRegistryBlockHeight, ushort blockType, Span<byte> content, Span<byte> transactionKey, Span<byte> keyImage, ulong tagId, Span<byte> commitment, Span<byte> destinationKey, ulong amount, Span<byte> assetId)
        {
            UtxoUnspentBlock utxoUnspentBlock = new UtxoUnspentBlock
            {
                SyncBlockHeight = syncBlockHeight,
                RegistryCombinedBlockHeight = combinedRegistryBlockHeight,
                BlockType = blockType,
                TagId = tagId,
                Amount = amount,
                AssetId = assetId.ToArray(),
                TransactionKey = transactionKey.ToArray(),
                KeyImage = keyImage.ToArray(),
                Output = GetOrAddUtxoOutput(tagId, commitment, destinationKey),
                Content = content.ToArray()
            };

            lock(_sync)
            {
                _dataContext.UtxoUnspentBlocks.Add(utxoUnspentBlock);
                _dataContext.SaveChanges();
            }
        }

        public List<ulong> GetUtxoUnspentBlockTagIds()
        {
            lock (_sync)
            {
                List<ulong> utxoUnspentBlockTagIds = _dataContext.UtxoUnspentBlocks.Select(u => u.TagId).ToList();
                return utxoUnspentBlockTagIds;
            }
        }

        public List<UtxoUnspentBlock> GetUtxoUnspentBlocksByTagId(ulong tagId)
        {
            lock(_sync)
            {
                List<UtxoUnspentBlock> utxoUnspentBlocks = _dataContext.UtxoUnspentBlocks.Where(u => u.TagId == tagId).ToList();
                return utxoUnspentBlocks;
            }
        }

        public List<TransactionalIncomingBlock> GetIncomingBlocksByBlockType(ushort blockType)
        {
            lock(_sync)
            {
                List<TransactionalIncomingBlock> incomingBlocks = _dataContext.TransactionalIncomingBlocks.Where(b => b.BlockType == blockType).ToList();

                return incomingBlocks;
            }
        }

        public List<UtxoIncomingBlock> GetIncomingUtxoBlocksByType(ushort blockType)
        {
            lock(_sync)
            {
                return _dataContext.UtxoIncomingBlocks.Where(b => b.BlockType == blockType).ToList();
            }
        }

        public void UpdateLastRegistryCombinedBlock(ulong height, byte[] content)
        {
            lock(_sync)
            {
                if (!_dataContext.RegistryCombinedBlocks.Any(b => b.RegistryCombinedBlockId == height))
                {
                    _dataContext.RegistryCombinedBlocks.Add(new RegistryCombinedBlock { RegistryCombinedBlockId = height, Content = content });
                    _dataContext.SaveChanges();
                }
            }
        }

        public void UpdateLastSyncBlock(ulong height, byte[] hash)
        {
            lock (_sync)
            {
                if (!_dataContext.SyncBlocks.Any(b => b.SyncBlockId == height))
                {
                    _dataContext.SyncBlocks.Add(new SyncBlock { SyncBlockId = height, Hash = hash });
                    _dataContext.SaveChanges();
                }
            }
        }

        public void StoreUtxoOutput(ulong tagId, Span<byte> commitment, Span<byte> destinationKey)
        {
            GetOrAddUtxoOutput(tagId, commitment, destinationKey);
        }

        public int GetTotalUtxoOutputsAmount(ulong tagId)
        {
            lock (_sync)
            {
                if (tagId == 0)
                {
                    return _utxoOutputsIndiciesMap.Count;
                }
                else
                {
                    return _dataContext.UtxoOutputs.Where(u => u.TagId == tagId).Count();
                }
            }
        }

        public void GetUtxoOutputByIndex(out Span<byte> commitment, out Span<byte> destinationKey, ulong tagId, int index)
        {
            if(!_utxoOutputsIndiciesMap.ContainsKey(index))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            UtxoOutput utxoOutput = null;

            if (tagId == 0)
            {
                ulong utxoOutputId = _utxoOutputsIndiciesMap[index];

                lock (_sync)
                {
                    utxoOutput = _dataContext.UtxoOutputs.FirstOrDefault(u => u.UtxoOutputId == utxoOutputId);
                }
            }
            else
            {
                lock (_sync)
                {
                    utxoOutput = _dataContext.UtxoOutputs.Skip(index).FirstOrDefault();
                }
            }

            commitment = utxoOutput.Commitment;
            destinationKey = utxoOutput.DestinationKey;
        }

        public void Initialize()
        {
            lock (_sync)
            {
                _dataContext.Database.EnsureCreated();

                foreach (var utxoOutput in _dataContext.UtxoOutputs)
                {
                    _utxoOutputsIndiciesMap.Add(_utxoOutputsIndiciesMap.Count, utxoOutput.UtxoOutputId);
                }
            }
        }

        #region Private Functions

        private Identity GetOrAddIdentity(Span<byte> identityKey)
        {
            if(identityKey == null)
            {
                return null;
            }

            lock (_sync)
            {
                byte[] identityBytes = identityKey.ToArray();
                Identity identity = _dataContext.Identities.FirstOrDefault(i => i.Key.Equals32(identityBytes));

                if (identity == null)
                {
                    identity = new Identity
                    {
                        Key = identityBytes
                    };

                    _dataContext.Identities.Add(identity);
                    _dataContext.SaveChanges();
                }

                return identity;
            }
        }

        private BlockHash GetOrAddBlockHash(Span<byte> blockHash)
        {
            lock(_sync)
            {
                byte[] blockHashBytes = blockHash.ToArray();
                BlockHash block = _dataContext.BlockHashes.FirstOrDefault(b => b.Hash.Equals32(blockHashBytes));

                if(block == null)
                {
                    block = new BlockHash
                    {
                        Hash = blockHashBytes
                    };

                    _dataContext.BlockHashes.Add(block);
                    _dataContext.SaveChanges();
                }

                return block;
            }
        }

        private UtxoKeyImage GetOrAddUtxoKeyImage(Span<byte> keyImage)
        {
            lock (_sync)
            {
                byte[] keyImageBytes = keyImage.ToArray();
                UtxoKeyImage utxoKeyImage = _dataContext.UtxoKeyImages.FirstOrDefault(b => b.KeyImage.Equals32(keyImageBytes));

                if (utxoKeyImage == null)
                {
                    utxoKeyImage = new UtxoKeyImage
                    {
                        KeyImage = keyImageBytes
                    };

                    _dataContext.UtxoKeyImages.Add(utxoKeyImage);
                    _dataContext.SaveChanges();
                }

                return utxoKeyImage;
            }
        }


        private UtxoTransactionKey GetOrAddUtxoTransactionKey(Span<byte> transactionKey)
        {
            lock (_sync)
            {
                byte[] transactionKeyBytes = transactionKey.ToArray();
                UtxoTransactionKey utxoTransactionKey = _dataContext.UtxoTransactionKeys.FirstOrDefault(b => b.Key.Equals32(transactionKeyBytes));

                if (utxoTransactionKey == null)
                {
                    utxoTransactionKey = new UtxoTransactionKey
                    {
                        Key = transactionKeyBytes
                    };

                    _dataContext.UtxoTransactionKeys.Add(utxoTransactionKey);
                    _dataContext.SaveChanges();
                }

                return utxoTransactionKey;
            }
        }

        private UtxoOutput GetOrAddUtxoOutput(ulong tagId, Span<byte> commitment, Span<byte> destinationKey)
        {
            lock(_sync)
            {
                byte[] commitmentBytes = commitment.ToArray();
                byte[] destinationKeyBytes = destinationKey.ToArray();

                UtxoOutput utxoOutput = _dataContext.UtxoOutputs.FirstOrDefault(b => b.Commitment.Equals32(commitmentBytes) && b.DestinationKey.Equals32(destinationKeyBytes));

                if(utxoOutput == null)
                {
                    utxoOutput = new UtxoOutput
                    {
                        TagId = tagId,
                        Commitment = commitmentBytes,
                        DestinationKey = destinationKeyBytes
                    };

                    _dataContext.UtxoOutputs.Add(utxoOutput);
                    _dataContext.SaveChanges();

                    _utxoOutputsIndiciesMap.Add(_utxoOutputsIndiciesMap.Count, utxoOutput.UtxoOutputId);
                }

                return utxoOutput;
            }
        }

        #endregion Private Functions
    }
}
