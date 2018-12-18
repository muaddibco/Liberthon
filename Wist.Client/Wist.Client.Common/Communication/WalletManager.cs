using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Wist.BlockLattice.Core;
using Wist.BlockLattice.Core.DataModel;
using Wist.BlockLattice.Core.DataModel.Registry;
using Wist.BlockLattice.Core.DataModel.Transactional;
using Wist.BlockLattice.Core.DataModel.UtxoConfidential;
using Wist.BlockLattice.Core.Enums;
using Wist.BlockLattice.Core.Parsers;
using Wist.BlockLattice.Core.Serializers;
using Wist.BlockLattice.Core.Serializers.UtxoConfidential;
using Wist.Client.Common.Entities;
using Wist.Client.Common.Interfaces;
using Wist.Client.Common.Services;
using Wist.Client.DataModel.Model;
using Wist.Client.DataModel.Services;
using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;
using Wist.Core.Cryptography;
using Wist.Core.ExtensionMethods;
using Wist.Core.HashCalculations;
using Wist.Core.Identity;
using Wist.Core.States;
using Wist.Crypto.ConfidentialAssets;
using Wist.Proto.Model;

namespace Wist.Client.Common.Communication
{
    [RegisterDefaultImplementation(typeof(IWalletManager), Lifetime = LifetimeManagement.Singleton)]
    public class WalletManager : IWalletManager
    {
        private const ulong _idCardTagId = 1;
        private INetworkAdapter _networkAdapter;
        private readonly IBlockCreator _blockCreator;
        private readonly IDataAccessService _dataAccessService;
        private readonly ISerializersFactory _signatureSupportSerializersFactory;
        private readonly IBlockParsersRepositoriesRepository _blockParsersRepositoriesRepository;
        private readonly IDictionary<byte[], ulong> _heightsDictionary;
        private readonly IHashCalculation _hashCalculation;
        private readonly IHashCalculation _proofOfWorkCalculation;
        private readonly IIdentityKeyProvider _identityKeyProvider;
        private readonly IClientState _clientState;

        public WalletManager(INetworkAdapter networkAdapter, IBlockCreator blockCreator, IDataAccessService dataAccessService, IHashCalculationsRepository hashCalculationsRepository, 
            IIdentityKeyProvidersRegistry identityKeyProvidersRegistry, IStatesRepository statesRepository, ISerializersFactory signatureSupportSerializersFactory,
            IBlockParsersRepositoriesRepository blockParsersRepositoriesRepository)
        {
            _networkAdapter = networkAdapter;
            _blockCreator = blockCreator;
            _dataAccessService = dataAccessService;
            _signatureSupportSerializersFactory = signatureSupportSerializersFactory;
            _blockParsersRepositoriesRepository = blockParsersRepositoriesRepository;
            _heightsDictionary = new Dictionary<byte[], ulong>();
            _hashCalculation = hashCalculationsRepository.Create(Globals.DEFAULT_HASH);
            _proofOfWorkCalculation = hashCalculationsRepository.Create(Globals.POW_TYPE);
            _identityKeyProvider = identityKeyProvidersRegistry.GetInstance();
            _clientState = statesRepository.GetInstance<IClientState>();
        }

        #region ============ PUBLIC FUNCTIONS =============  

        public void InitializeNetwork()
        {
           
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool SendFunds(uint amount, Account receiver)
        {
            //1. create block of type funds
            //2. create serializer for it
            //3. send block
            TransferFundsBlock block = (TransferFundsBlock)CreateFundsBlock(amount, receiver);
            BlockBase registerBlock = CreateRegisterBlock(block, block.TargetOriginalHash);

            return _networkAdapter.SendTransaction(block, registerBlock);
        }

        public bool IssueAssets(string issuanceInfo, byte[][] assetIds, string[] assetInfos, ulong tagId)
        {
            IssueAssetsBlock block = (IssueAssetsBlock)CreateIssueAssetsBlock(issuanceInfo, assetIds, assetInfos, tagId);
            BlockBase registerBlock = CreateRegisterBlock(block, null);

            return _networkAdapter.SendTransaction(block, registerBlock);
        }

        public bool IssueAssets(ICollection<KeyValuePair<string, string>> assetDetails, string[] asstetId, ulong tagId = 0)
        {
            byte[][] arrays = new byte[assetDetails.Count][];

            List<KeyValuePair<string, string>> pairs = assetDetails.ToList();

            for (int i = 0; i < assetDetails.Count; i++)
            {
                KeyValuePair<string, string> pair = pairs[i];

                byte[] key  = Encoding.ASCII.GetBytes($"key: {pair.Key}@");
                byte[] value = Encoding.ASCII.GetBytes($"value: {pair.Value}");

                arrays[i] = key.Concat(value).ToArray();
            }
            IssueAssetsBlock block = (IssueAssetsBlock)CreateIssueAssetsBlock(null, arrays, asstetId, tagId);
            BlockBase registerBlock = CreateRegisterBlock(block, null);

            return _networkAdapter.SendTransaction(block, registerBlock);
        }

        public bool SendAssetToUtxo(byte[][] assetIds, int index, ulong tagId, ConfidentialAccount receiver, byte[] sk = null)
        {
            TransferAssetToUtxoBlock block = (TransferAssetToUtxoBlock)CreateTransferAssetToUtxoBlock(assetIds, index, tagId, receiver, sk);
            BlockBase registerBlock = CreateRegisterBlock(block, block.DestinationKey);

            return _networkAdapter.SendTransaction(block, registerBlock);
        }

        public bool SendAssetTransition(byte[] assetId, byte[] transactionKey, byte[] assetCommitment, byte[] prevDestinationKey, ulong tagId, Account target)
        {
            UtxoConfidentialBase block = (UtxoConfidentialBase)CreateNonQuantitativeTransitionAssetTransferBlock(target, assetId, transactionKey, assetCommitment, prevDestinationKey, 3, tagId, out byte[] otsk, out int pos);
            BlockBase registerBlock = CreateUtxoRegisterBlock(block, otsk, pos);

            return _networkAdapter.SendTransaction(block, registerBlock);
        }

        public bool SendAcceptAsset(byte[] transactionKey, byte[] assetCommitment, byte[] blindingFactor, byte[] assetId, ulong tagId)
        {
            TransactionalBlockBase block = (TransactionalBlockBase)CreateAcceptAssetTransitionBlock(transactionKey, assetCommitment, blindingFactor, assetId, tagId);
            BlockBase registerBlock = CreateRegisterBlock(block, null);

            return _networkAdapter.SendTransaction(block, registerBlock);
        }

        #endregion

        #region ============ PRIVATE FUNCTIONS ============ 

        private BlockBase CreateFundsBlock(ulong amount, Account receiver)
        {

            TransferFundsBlock fundsBlock = (TransferFundsBlock)_blockCreator.GetInstance(BlockTypes.Transaction_TransferFunds);

            fundsBlock.UptodateFunds = 0;// transactionalBlockEssense.UpToDateFunds > 0 ? transactionalBlockEssense.UpToDateFunds - amount : 100000
            fundsBlock.TargetOriginalHash = receiver.PublicKey;

            FillHeightInfo(fundsBlock);
            FillSyncData(fundsBlock);
            FillRawData(fundsBlock);

            return fundsBlock;
        }
            
        public BlockBase CreateRegisterBlock(TransactionalBlockBase transactionalBlock, byte[] target)
        {
            RegistryRegisterBlock registerBlock = new RegistryRegisterBlock
            {
                SyncBlockHeight = transactionalBlock.SyncBlockHeight,
                BlockHeight = transactionalBlock.BlockHeight,
                Nonce = transactionalBlock.Nonce,
                PowHash = transactionalBlock.PowHash,
                ReferencedPacketType = transactionalBlock.PacketType,
                ReferencedBlockType = transactionalBlock.BlockType,
                ReferencedTarget = target ?? new byte[32],
                ReferencedBodyHash = _hashCalculation.CalculateHash(transactionalBlock.RawData)
            };

            return registerBlock;
        }

        public BlockBase CreateUtxoRegisterBlock(UtxoConfidentialBase confidentialBase, byte[] otsk, int actualAssetPos)
        {
            byte[] msg = ConfidentialAssetsHelper.FastHash256(confidentialBase.RawData.ToArray());

            RegistryRegisterUtxoConfidentialBlock registryRegisterUtxoConfidentialBlock = new RegistryRegisterUtxoConfidentialBlock
            {
                SyncBlockHeight = confidentialBase.SyncBlockHeight,
                Nonce = confidentialBase.Nonce,
                PowHash = confidentialBase.PowHash,
                ReferencedPacketType = confidentialBase.PacketType,
                ReferencedBlockType = confidentialBase.BlockType,
                DestinationKey = confidentialBase.DestinationKey,
                KeyImage = confidentialBase.KeyImage,
                ReferencedBodyHash = _hashCalculation.CalculateHash(confidentialBase.RawData),
                TransactionPublicKey = confidentialBase.TransactionPublicKey,
                TagId = confidentialBase.TagId,
                PublicKeys = confidentialBase.PublicKeys,
                Signatures = ConfidentialAssetsHelper.GenerateRingSignature(msg, confidentialBase.KeyImage.Value.ToArray(), confidentialBase.PublicKeys.Select(p => p.Value.ToArray()).ToArray(), otsk, actualAssetPos)
            };

            return registryRegisterUtxoConfidentialBlock;
        }

        private BlockBase CreateTransferAssetToUtxoBlock(byte[][] assetIds, int index, ulong tagId, ConfidentialAccount receiver, byte[] sk = null)
        {
            byte[] assetId = assetIds[index];

            byte[] secretKey = sk ?? ConfidentialAssetsHelper.GetRandomSeed();
            byte[] transactionKey = ConfidentialAssetsHelper.GetTrancationKey(secretKey);
            byte[] destinationKey = ConfidentialAssetsHelper.GetDestinationKey(secretKey, receiver.PublicViewKey, receiver.PublicSpendKey);
            byte[] blindingFactor = ConfidentialAssetsHelper.GetRandomSeed();
            byte[] assetCommitment = ConfidentialAssetsHelper.GetAssetCommitment(assetId, blindingFactor);
            ulong[] assetAmounts = new ulong[assetIds.Length];
            for (int i = 0; i < assetAmounts.Length; i++)
            {
                assetAmounts[i] = 1;
            }

            TransferAssetToUtxoBlock transferAssetToUtxoBlock = new TransferAssetToUtxoBlock
            {
                TagId = tagId,
                AssetIds = assetIds,
                AssetAmounts = assetAmounts,
                TransactionPublicKey = transactionKey,
                DestinationKey = destinationKey,
                AssetId = assetId,
                AssetCommitment = assetCommitment,
                SurjectionProof = ConfidentialAssetsHelper.CreateNewIssuanceSurjectionProof(assetCommitment, assetIds, index, blindingFactor),
                EcdhTuple = ConfidentialAssetsHelper.CreateEcdhTupleCA(blindingFactor, assetId, secretKey, receiver.PublicViewKey)
            };

            FillHeightInfo(transferAssetToUtxoBlock);
            FillSyncData(transferAssetToUtxoBlock);
            FillRawData(transferAssetToUtxoBlock);

            return transferAssetToUtxoBlock;
        }

        private BlockBase CreateIssueAssetsBlock(string issuanceInfo, byte[][] assetIds, string[] assetInfos, ulong tagId)
        {
            TransactionalBlockEssense transactionalBlockEssense = _networkAdapter.GetLastBlock(_clientState.GetPublicKey());

            IssueAssetsBlock issueAssetsBlock = new IssueAssetsBlock
            {
                BlockHeight = transactionalBlockEssense.Height + 1,
                TagId = tagId,
                UptodateFunds = transactionalBlockEssense.UpToDateFunds,
                AssetIds = new byte[0][],
                AssetAmounts = new ulong[0],
                IssuedAssetIds = assetIds,
                IssuedAssetInfo = assetInfos,
                IssuanceInfo = issuanceInfo
            };

            FillHeightInfo(issueAssetsBlock);
            FillSyncData(issueAssetsBlock);
            FillRawData(issueAssetsBlock);

            return issueAssetsBlock;
        }

        private BlockBase CreateNonQuantitativeTransitionAssetTransferBlock(Account receiver, byte[] assetId, byte[] prevTransactionKey, byte[] prevCommitment, byte[] prevDestinationKey, int ringSize, ulong tagId, out byte[] otsk, out int pos)
        {
            if (!_clientState.IsConfidential())
            {
                otsk = null;
                pos = -1;
                return null;
            }

            byte[] otskAsset = ConfidentialAssetsHelper.GetOTSK(prevTransactionKey, _clientState.GetSecretViewKey(), _clientState.GetSecretSpendKey());
            otsk = otskAsset;
            byte[] keyImage = ConfidentialAssetsHelper.GenerateKeyImage(otskAsset);
            byte[] secretKey = ConfidentialAssetsHelper.GetRandomSeed();
            byte[] transactionKey = ConfidentialAssetsHelper.GetTrancationKey(secretKey);
            byte[] destinationKey = _hashCalculation.CalculateHash(receiver.PublicKey);
            byte[] blindingFactor = ConfidentialAssetsHelper.GetRandomSeed();
            byte[] assetCommitment = ConfidentialAssetsHelper.GetAssetCommitment(assetId, blindingFactor);

            byte[] msg = ConfidentialAssetsHelper.FastHash256(BitConverter.GetBytes(tagId), keyImage, destinationKey, transactionKey, assetCommitment);

            Random random = new Random(BitConverter.ToInt32(secretKey, 0));
            GetCommitmentAndProofs(prevCommitment, prevDestinationKey, ringSize, tagId, random, out int actualAssetPos, out byte[][] assetCommitments, out byte[][] assetPubs);
            pos = actualAssetPos;

            UtxoUnspentBlock idCardBlock = _dataAccessService.GetUtxoUnspentBlocksByTagId(_idCardTagId).First();
            byte[] otskAffiliation = ConfidentialAssetsHelper.GetOTSK(idCardBlock.TransactionKey, _clientState.GetSecretViewKey(), _clientState.GetSecretSpendKey());
            byte[] affiliationBlindingFactor = ConfidentialAssetsHelper.GetRandomSeed();
            byte[] affiliationAssetCommitment = ConfidentialAssetsHelper.GetAssetCommitment(idCardBlock.AssetId, affiliationBlindingFactor);
            GetCommitmentAndProofs(idCardBlock.Output.Commitment, idCardBlock.Output.DestinationKey, ringSize, _idCardTagId, random, out int actualAffiliationPos, out byte[][] affiliationCommitments, out byte[][] affiliationPubs);

            BorromeanRingSignature borromeanRingSignature = ConfidentialAssetsHelper.GenerateBorromeanRingSignature(msg, affiliationPubs, actualAffiliationPos, otskAffiliation);

            SurjectionProof assetSurjectionProof = ConfidentialAssetsHelper.CreateAssetRangeProof(assetCommitment, assetCommitments, actualAssetPos, blindingFactor);
            SurjectionProof affilaitionSurjectionProof = ConfidentialAssetsHelper.CreateAssetRangeProof(affiliationAssetCommitment, affiliationCommitments, actualAffiliationPos, affiliationBlindingFactor);

            List<TransactionalIncomingBlock> incomingBlocks = _dataAccessService.GetIncomingBlocksByBlockType(BlockTypes.Transaction_IssueAssets);
            List<IssueAssetsBlock> issueAssetsBlocks = incomingBlocks.Where(b => b.TagId == _idCardTagId).ToList().Select(b =>
            {
                return (IssueAssetsBlock)_blockParsersRepositoriesRepository.GetBlockParsersRepository(PacketType.Transactional).GetInstance(b.BlockType).Parse(b.Content);
            }).ToList();

            List<byte[]> rawIdCardAssetIds = issueAssetsBlocks.SelectMany(b => b.IssuedAssetIds).ToList();

            SurjectionProof affiliationEvidenceSurjectionProof = ConfidentialAssetsHelper.CreateNewIssuanceSurjectionProof(affiliationAssetCommitment, rawIdCardAssetIds.ToArray(), rawIdCardAssetIds.FindIndex(b => b.Equals32(idCardBlock.AssetId)), affiliationBlindingFactor);

            NonQuantitativeTransitionAssetTransferBlock block = new NonQuantitativeTransitionAssetTransferBlock
            {
                TagId = tagId,
                KeyImage = _identityKeyProvider.GetKey(keyImage),
                DestinationKey = destinationKey,
                TransactionPublicKey = transactionKey,
                AssetCommitment = assetCommitment,
                SurjectionProof = assetSurjectionProof,
                AffiliationCommitment = affiliationAssetCommitment,
                AffiliationPseudoKeys = affiliationPubs,
                AffiliationSurjectionProof = affilaitionSurjectionProof,
                AffiliationBorromeanSignature = borromeanRingSignature,
                AffiliationEvidenceSurjectionProof = affiliationEvidenceSurjectionProof,
                EcdhTuple = ConfidentialAssetsHelper.CreateEcdhTupleCA(blindingFactor, assetId, secretKey, receiver.PublicKey),
                PublicKeys = assetPubs.Select(p => _identityKeyProvider.GetKey(p)).ToArray(),
                Signatures = ConfidentialAssetsHelper.GenerateRingSignature(msg, keyImage, assetPubs, otskAsset, actualAssetPos)
            };

            FillSyncData(block);
            FillRawData(block);

            return block;
        }

        private void GetCommitmentAndProofs(byte[] prevCommitment, byte[] prevDestinationKey, int ringSize, ulong tagId, Random random, out int actualAssetPos, out byte[][] commitments, out byte[][] pubs)
        {
            int totalAssetUtxos = _dataAccessService.GetTotalUtxoOutputsAmount(tagId);
            int[] fakeAssetIndicies = new int[ringSize];
            actualAssetPos = random.Next(ringSize + 1);
            commitments = new byte[ringSize + 1][];
            pubs = new byte[ringSize + 1][];
            for (int i = 0; i < ringSize + 1; i++)
            {
                if (i == actualAssetPos)
                {
                    commitments[i] = prevCommitment;
                    pubs[i] = prevDestinationKey;
                }
                else
                {
                    int randomPos = random.Next(totalAssetUtxos);
                    _dataAccessService.GetUtxoOutputByIndex(out Span<byte> fakeCommitment, out Span<byte> fakeDestinationKey, tagId, randomPos);

                    commitments[i] = fakeCommitment.ToArray();
                    pubs[i] = fakeDestinationKey.ToArray();
                }
            }
        }

        private BlockBase CreateAcceptAssetTransitionBlock(byte[] transactionKey, byte[] assetCommitment, byte[] blindingFactor, byte[] assetId, ulong tagId)
        {
            if(_clientState.IsConfidential())
            {
                return null;
            }

            List<TransactionalOutcomingBlock> outcomingBlocks = _dataAccessService.GetOutcomingTransactionBlocks();

            List<TransactionalOutcomingBlock> acceptanceBlocks = outcomingBlocks?.Where(b => b.BlockType == BlockTypes.Transaction_AcceptAssetTransition).ToList();

            List<byte[]> assetIds = new List<byte[]>();
            List<ulong> amounts = new List<ulong>();

            if(acceptanceBlocks != null)
            {
                TransactionalOutcomingBlock outcomingBlock = acceptanceBlocks.OrderByDescending(b => b.Height).First();
                AcceptAssetTransitionBlock acceptAsset = (AcceptAssetTransitionBlock)_blockParsersRepositoriesRepository.
                    GetBlockParsersRepository(PacketType.Transactional).
                    GetInstance(BlockTypes.Transaction_AcceptAssetTransition).
                    Parse(outcomingBlock.Content);

                assetIds = new List<byte[]>(acceptAsset.AssetIds);
                amounts = new List<ulong>(acceptAsset.AssetAmounts);
            }

            int index = assetIds.FindIndex(a => a.Equals32(assetId));
            if (index >= 0)
            {
                amounts[index]++;
            }
            else
            {
                assetIds.Add(assetId);
                amounts.Add(1);
            }

            AcceptAssetTransitionBlock block = new AcceptAssetTransitionBlock
            {
                AssetIds = assetIds.ToArray(),
                AssetAmounts = amounts.ToArray(),
                TagId = tagId,
                UptodateFunds = 0,
                AcceptedTransactionKey = transactionKey,
                AcceptedCommitment = assetCommitment,
                AcceptedBlindingFactor = blindingFactor,
                AcceptedAssetId = assetId
            };

            FillHeightInfo(block);
            FillSyncData(block);
            FillRawData(block);

            return block;
        }

        private void FillHeightInfo(TransactionalBlockBase transactionalBlockBase)
        {
            TransactionalBlockEssense transactionalBlockEssense = _networkAdapter.GetLastBlock(_clientState.GetPublicKey());
            transactionalBlockBase.BlockHeight = transactionalBlockEssense.Height + 1;
            transactionalBlockBase.HashPrev = transactionalBlockEssense.Hash.ToByteArray();
        }

        private void FillSyncData(SyncedBlockBase block)
        {
            Proto.Model.SyncBlockDescriptor lastSyncBlock = _networkAdapter.GetLastSyncBlock();
            block.SyncBlockHeight = lastSyncBlock.Height;
            block.PowHash = GetPowHash(lastSyncBlock.Hash.ToByteArray(), 0);
        }

        private void FillSyncData(UtxoConfidentialBase block)
        {
            Proto.Model.SyncBlockDescriptor lastSyncBlock = _networkAdapter.GetLastSyncBlock();
            block.SyncBlockHeight = lastSyncBlock.Height;
            block.PowHash = GetPowHash(lastSyncBlock.Hash.ToByteArray(), 0);
        }

        private byte[] GetPowHash(byte[] hash, ulong nonce)
        {
            BigInteger bigInteger = new BigInteger(hash);
            bigInteger += nonce;
            byte[] hashNonce = bigInteger.ToByteArray();
            byte[] powHash = _proofOfWorkCalculation.CalculateHash(hashNonce);
            return powHash;
        }
        
        private void FillRawData(BlockBase blockBase)
        {
            ISerializer serializer = _signatureSupportSerializersFactory.Create(blockBase);
            serializer.FillBodyAndRowBytes();
        }

        #endregion

    }
}
