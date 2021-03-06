﻿using System;
using System.Buffers.Binary;
using System.Numerics;
using Wist.BlockLattice.Core.DataModel;
using Wist.BlockLattice.Core.Enums;
using Wist.BlockLattice.Core.Interfaces;
using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;
using Wist.Core.ExtensionMethods;
using Wist.Core.Logging;
using Wist.Core.HashCalculations;
using Wist.Core.States;
using Wist.Core.Synchronization;
using System.Linq;

namespace Wist.BlockLattice.Core.Handlers
{
    [RegisterExtension(typeof(ICoreVerifier), Lifetime = LifetimeManagement.TransientPerResolve)]
    public class SyncHeightVerifier : ICoreVerifier
    {
        private readonly ILogger _log;
        private readonly ISynchronizationContext _synchronizationContext;
        private readonly IHashCalculation _proofOfWorkCalculation;

        public SyncHeightVerifier(IStatesRepository statesRepository, IHashCalculationsRepository hashCalculationsRepository, ILoggerService loggerService)
        {
            _log = loggerService.GetLogger(GetType().Name);
            _synchronizationContext = statesRepository.GetInstance<ISynchronizationContext>();
            _proofOfWorkCalculation = hashCalculationsRepository.Create(Globals.POW_TYPE);
        }

        public bool VerifyBlock(BlockBase blockBase)
        {
            SyncedBlockBase syncedBlockBase = (SyncedBlockBase)blockBase;

            ulong syncBlockHeight = syncedBlockBase.SyncBlockHeight;

            if (!((_synchronizationContext.LastBlockDescriptor?.BlockHeight.Equals(syncBlockHeight) ?? true) ||
                (_synchronizationContext.PrevBlockDescriptor?.BlockHeight.Equals(syncBlockHeight) ?? true)))
            {
                _log.Error($"Synchronization block height is outdated: {blockBase.RawData.ToArray().ToHexString()}");
                return false;
            }

            return CheckSyncPOW(syncedBlockBase);
        }

        private bool CheckSyncPOW(SyncedBlockBase syncedBlockBase)
        {
            ulong syncBlockHeight = syncedBlockBase.SyncBlockHeight;

            uint nonce = syncedBlockBase.Nonce;
            byte[] powHash = syncedBlockBase.PowHash;
            byte[] baseHash;
            byte[] baseSyncHash;

            if (syncedBlockBase.PacketType != PacketType.Synchronization)
            {
                //TODO: make difficulty check dynamic
                //if (powHash[0] != 0 || powHash[1] != 0)
                //{
                //    return false;
                //}
                BigInteger bigInteger;
                baseSyncHash = new byte[Globals.DEFAULT_HASH_SIZE + 1]; // Adding extra 0 byte for avoiding negative values of BigInteger
                lock (_synchronizationContext)
                {
                    byte[] buf;
                    if (_synchronizationContext.LastBlockDescriptor != null || _synchronizationContext.PrevBlockDescriptor != null)
                    {
                        buf = (syncBlockHeight == _synchronizationContext.LastBlockDescriptor?.BlockHeight) ? _synchronizationContext.LastBlockDescriptor.Hash : _synchronizationContext.PrevBlockDescriptor.Hash;
                    }
                    else
                    {
                        _log.Warning("CheckSyncPOW - BOTH LastBlockDescriptor and PrevBlockDescriptor are NULL");
                        buf = new byte[Globals.DEFAULT_HASH_SIZE];
                    }

                    Array.Copy(buf, 0, baseSyncHash, 0, buf.Length);
                }

                bigInteger = new BigInteger(baseSyncHash);

                bigInteger += nonce;
                baseHash = bigInteger.ToByteArray().Take(Globals.DEFAULT_HASH_SIZE).ToArray();
            }
            else
            {
                lock (_synchronizationContext)
                {
                    if (_synchronizationContext.LastBlockDescriptor == null)
                    {
                        baseSyncHash = new byte[Globals.DEFAULT_HASH_SIZE];
                    }
                    else
                    {
                        baseSyncHash = (syncBlockHeight == _synchronizationContext.LastBlockDescriptor.BlockHeight) ? _synchronizationContext.LastBlockDescriptor.Hash : _synchronizationContext.PrevBlockDescriptor.Hash;
                    }
                }

                baseHash = baseSyncHash;
            }

            byte[] computedHash = _proofOfWorkCalculation.CalculateHash(baseHash);

            if (!computedHash.Equals24(powHash))
            {
                _log.Error($"Computed HASH differs from obtained one. PacketType is {syncedBlockBase.PacketType}, BlockType is {syncedBlockBase.BlockType}. Reported SyncBlockHeight is {syncedBlockBase.SyncBlockHeight}, Nonce is {syncedBlockBase.Nonce}, POW is {syncedBlockBase.PowHash.ToHexString()}. Hash of SyncBlock is {baseSyncHash.ToHexString()}, after adding Nonce is {baseHash.ToHexString()}, computed POW Hash is {computedHash.ToHexString()}");
                return false;
            }

            return true;
        }
    }
}
