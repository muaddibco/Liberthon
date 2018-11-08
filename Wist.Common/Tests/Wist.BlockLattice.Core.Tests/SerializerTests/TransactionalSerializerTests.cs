using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Wist.BlockLattice.Core.DataModel.Transactional;
using Wist.BlockLattice.Core.Enums;
using Wist.BlockLattice.Core.Serializers.Signed.Transactional;
using Wist.Core.Cryptography;
using Wist.Core.ExtensionMethods;
using Wist.Core.Identity;
using Wist.Tests.Core;
using Xunit;

namespace Wist.BlockLattice.Core.Tests.SerializerTests
{
    public class TransactionalSerializerTests : TestBase
    {
        public TransactionalSerializerTests() : base()
        {

        }

        [Fact]
        public void TransferFundsBlockSerializerTests()
        {
            ulong tagId = 113;
            ulong syncBlockHeight = 1;
            uint nonce = 4;
            byte[] powHash = BinaryBuilder.GetPowHash(1234);
            ushort version = 1;
            ulong blockHeight = 9;
            byte[] prevHash = BinaryBuilder.GetDefaultHash(1234);
            byte[] body;

            ulong uptodateFunds = 10001;
            byte[] targetHash = BinaryBuilder.GetDefaultHash(1235);

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(tagId);
                    bw.Write(uptodateFunds);
                    bw.Write((uint)0);
                    bw.Write(targetHash);
                }

                body = ms.ToArray();
            }

            byte[] expectedPacket = BinaryBuilder.GetSignedPacket(PacketType.Transactional, syncBlockHeight, nonce, powHash, version,
                BlockTypes.Transaction_TransferFunds, blockHeight, prevHash, body, _privateKey, out byte[] expectedSignature);

            TransferFundsBlock block = new TransferFundsBlock
            {
                SyncBlockHeight = syncBlockHeight,
                Nonce = nonce,
                PowHash = powHash,
                BlockHeight = blockHeight,
                HashPrev = prevHash,
                TagId = tagId,
                UptodateFunds = uptodateFunds,
                TargetOriginalHash = targetHash
            };

            TransferFundsBlockSerializer serializer = new TransferFundsBlockSerializer(_cryptoService, _identityKeyProvidersRegistry, _hashCalculationRepository);
            serializer.Initialize(block);

            byte[] actualPacket = serializer.GetBytes();

            Assert.Equal(expectedPacket, actualPacket);
        }

        [Fact]
        public void IssueAssetsBlockSerializerTest()
        {
            ulong tagId = 113;
            ulong syncBlockHeight = 1;
            uint nonce = 4;
            byte[] powHash = BinaryBuilder.GetPowHash(1234);
            ushort version = 1;
            ulong blockHeight = 9;
            byte[] prevHash = BinaryBuilder.GetDefaultHash(1234);
            byte[] body;

            ulong uptodateFunds = 10001;
            uint oldAssetIdsCount = 0;
            byte[][] assetIds = new byte[oldAssetIdsCount][];
            ulong[] assetAmounts = new ulong[oldAssetIdsCount];

            uint issuedAssetsCount = 10;
            byte[][] issuedAssetIds = new byte[issuedAssetsCount][];
            string[] issuedAssetInfos = new string[issuedAssetsCount];
            string issuanceInfo = "Issuance Info";

            Random random = new Random();

            for (int i = 0; i < oldAssetIdsCount; i++)
            {
                assetIds[i] = CryptoHelper.GetRandomSeed();
                assetAmounts[i] = (ulong)random.Next();
            }

            for (int i = 0; i < issuedAssetsCount; i++)
            {
                issuedAssetIds[i] = CryptoHelper.GetRandomSeed();
                issuedAssetInfos[i] = $"Asset ID {i}";
            }

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(tagId);
                    bw.Write(uptodateFunds);
                    bw.Write(oldAssetIdsCount);

                    for (int i = 0; i < oldAssetIdsCount; i++)
                    {
                        bw.Write(assetIds[i]);
                    }

                    for (int i = 0; i < oldAssetIdsCount; i++)
                    {
                        bw.Write(assetAmounts[i]);
                    }

                    bw.Write(issuedAssetsCount);

                    for (int i = 0; i < issuedAssetsCount; i++)
                    {
                        bw.Write(issuedAssetIds[i]);
                    }

                    for (int i = 0; i < issuedAssetsCount; i++)
                    {
                        bw.Write((byte)issuedAssetInfos[i].Length);
                        bw.Write(Encoding.ASCII.GetBytes(issuedAssetInfos[i]));
                    }

                    bw.Write((byte)issuanceInfo.Length);
                    bw.Write(Encoding.ASCII.GetBytes(issuanceInfo));
                }

                body = ms.ToArray();
            }

            byte[] expectedPacket = BinaryBuilder.GetSignedPacket(PacketType.Transactional, syncBlockHeight, nonce, powHash, version,
                BlockTypes.Transaction_IssueAssets, blockHeight, prevHash, body, _privateKey, out byte[] expectedSignature);

            IssueAssetsBlock block = new IssueAssetsBlock
            {
                SyncBlockHeight = syncBlockHeight,
                Nonce = nonce,
                PowHash = powHash,
                BlockHeight = blockHeight,
                HashPrev = prevHash,
                TagId = tagId,
                UptodateFunds = uptodateFunds,
                AssetIds = assetIds,
                AssetAmounts = assetAmounts,
                IssuedAssetIds = issuedAssetIds,
                IssuedAssetInfo = issuedAssetInfos,
                IssuanceInfo = issuanceInfo
            };

            IssueAssetsBlockSerializer serializer = new IssueAssetsBlockSerializer(_cryptoService, _identityKeyProvidersRegistry, _hashCalculationRepository);
            serializer.Initialize(block);

            byte[] actualPacket = serializer.GetBytes();

            Assert.Equal(expectedPacket, actualPacket);
        }

        [Fact]
        public void TransferAssetToUtxoBlockSerializerTest()
        {
            ulong tagId = 113;
            ulong syncBlockHeight = 1;
            uint nonce = 4;
            byte[] powHash = BinaryBuilder.GetPowHash(1234);
            ushort version = 1;
            ulong blockHeight = 9;
            byte[] prevHash = BinaryBuilder.GetDefaultHash(1234);
            byte[] body;

            ulong uptodateFunds = 10001;
            byte[] transactionPublicKey = CryptoHelper.GetRandomSeed();
            byte[] destinationKey = CryptoHelper.GetRandomSeed();
            byte[] assetId = CryptoHelper.GetRandomSeed();
            byte[] assetCommitment = CryptoHelper.GetRandomSeed();
            uint assetsCount = 10;
            byte[][] assetIds = new byte[assetsCount][];
            ulong[] assetAmounts = new ulong[assetsCount];
            byte[][] assetCommitments = new byte[assetsCount][];
            byte[] e = CryptoHelper.GetRandomSeed();
            byte[][] s = new byte[assetsCount][];
            byte[] mask = CryptoHelper.GetRandomSeed();

            Random random = new Random();

            for (int i = 0; i < assetsCount; i++)
            {
                assetIds[i] = CryptoHelper.GetRandomSeed();
                assetAmounts[i] = (ulong)random.Next();
                assetCommitments[i] = CryptoHelper.GetRandomSeed();
                s[i] = CryptoHelper.GetRandomSeed();
            }

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(tagId);
                    bw.Write(uptodateFunds);
                    bw.Write(assetsCount);

                    for (int i = 0; i < assetsCount; i++)
                    {
                        bw.Write(assetIds[i]);
                    }

                    for (int i = 0; i < assetsCount; i++)
                    {
                        bw.Write(assetAmounts[i]);
                    }

                    bw.Write(destinationKey);
                    bw.Write(transactionPublicKey);
                    bw.Write(assetId);
                    bw.Write(assetCommitment);
                    for (int i = 0; i < assetsCount; i++)
                    {
                        bw.Write(assetCommitments[i]);
                    }
                    bw.Write(e);
                    for (int i = 0; i < assetsCount; i++)
                    {
                        bw.Write(s[i]);
                    }
                    bw.Write(mask);
                    bw.Write(assetId);
                }

                body = ms.ToArray();
            }

            byte[] expectedPacket = BinaryBuilder.GetSignedPacket(PacketType.Transactional, syncBlockHeight, nonce, powHash, version,
                BlockTypes.Transaction_TransferAssetsToUtxo, blockHeight, prevHash, body, _privateKey, out byte[] expectedSignature);

            TransferAssetToUtxoBlock block = new TransferAssetToUtxoBlock
            {
                SyncBlockHeight = syncBlockHeight,
                Nonce = nonce,
                PowHash = powHash,
                BlockHeight = blockHeight,
                HashPrev = prevHash,
                TagId = tagId,
                UptodateFunds = uptodateFunds,
                AssetIds=assetIds,
                AssetAmounts = assetAmounts,
                DestinationKey = destinationKey,
                TransactionPublicKey = transactionPublicKey,
                AssetId = assetId,
                AssetCommitment = assetCommitment,
                SurjectionProof = new SurjectionProof
                {
                    AssetCommitments = assetCommitments,
                    Rs = new BorromeanRingSignature
                    {
                        E = e,
                        S = s
                    }
                },
                EcdhTuple = new EcdhTupleCA { Mask = mask, AssetId = assetId }
            };

            TransferAssetToUtxoBlockSerializer serializer = new TransferAssetToUtxoBlockSerializer(_cryptoService, _identityKeyProvidersRegistry, _hashCalculationRepository);
            serializer.Initialize(block);

            byte[] actualPacket = serializer.GetBytes();

            Assert.Equal(expectedPacket, actualPacket);
        }
    }
}
