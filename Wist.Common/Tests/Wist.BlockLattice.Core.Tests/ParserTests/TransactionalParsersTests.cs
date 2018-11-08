using System;
using System.IO;
using System.Text;
using Wist.BlockLattice.Core.DataModel.Transactional;
using Wist.BlockLattice.Core.Enums;
using Wist.BlockLattice.Core.Parsers.Transactional;
using Wist.Core.Cryptography;
using Wist.Tests.Core;
using Xunit;

namespace Wist.BlockLattice.Core.Tests.ParserTests
{
    public class TransactionalParsersTests : TestBase
    {
        [Fact]
        public void TransferFundsBlockParserTest()
        {
            ulong tagId = 147;
            ulong syncBlockHeight = 1;
            uint nonce = 4;
            byte[] powHash = BinaryBuilder.GetPowHash(1234);
            ushort version = 1;
            ulong blockHeight = 9;
            byte[] prevHash = BinaryBuilder.GetDefaultHash(1234);
            byte[] body;

            ulong uptodateFunds = 10001;
            byte[] targetOriginalHash = BinaryBuilder.GetDefaultHash(112233);

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(tagId);
                    bw.Write(uptodateFunds);
                    bw.Write((uint)0);
                    bw.Write(targetOriginalHash);
                }

                body = ms.ToArray();
            }

            byte[] packet = BinaryBuilder.GetSignedPacket(PacketType.Transactional, syncBlockHeight, nonce, powHash, version,
                BlockTypes.Transaction_TransferFunds, blockHeight, prevHash, body, _privateKey, out byte[] expectedSignature);

            TransferFundsBlockParser parser = new TransferFundsBlockParser(_hashCalculationRepository, _identityKeyProvidersRegistry);
            TransferFundsBlock block = (TransferFundsBlock)parser.Parse(packet);

            Assert.Equal(syncBlockHeight, block.SyncBlockHeight);
            Assert.Equal(nonce, block.Nonce);
            Assert.Equal(powHash, block.PowHash);
            Assert.Equal(version, block.Version);
            Assert.Equal(blockHeight, block.BlockHeight);
            Assert.Equal(prevHash, block.HashPrev);
            Assert.Equal(uptodateFunds, block.UptodateFunds);
            Assert.Equal(targetOriginalHash, block.TargetOriginalHash);

            Assert.Equal(_publicKey, block.Signer.Value.ToArray());
            Assert.Equal(expectedSignature, block.Signature.ToArray());
        }

        [Fact]
        public void IssueAssetsBlockParserTest()
        {
            ulong tagId = 147;
            ulong syncBlockHeight = 1;
            uint nonce = 4;
            byte[] powHash = BinaryBuilder.GetPowHash(1234);
            ushort version = 1;
            ulong blockHeight = 9;
            byte[] prevHash = BinaryBuilder.GetDefaultHash(1234);
            byte[] body;

            ulong uptodateFunds = 10001;

            uint ownedAssetIdsCount = 0;
            byte[][] assetIds = new byte[ownedAssetIdsCount][];
            ulong[] assetAmounts = new ulong[ownedAssetIdsCount];

            uint issuedAssetsCount = 10;
            byte[][] issuedAssetIds = new byte[issuedAssetsCount][];
            string[] issuedAssetInfos = new string[issuedAssetsCount];
            string issuanceInfo = "Issuance Info";

            Random random = new Random();

            for (int i = 0; i < ownedAssetIdsCount; i++)
            {
                assetIds[i] = CryptoHelper.GetRandomSeed();
                assetAmounts[i] = (ulong)random.Next();
            }

            for (int i = 0; i < issuedAssetsCount; i++)
            {
                issuedAssetIds[i] = CryptoHelper.GetRandomSeed();
                issuedAssetInfos[i] = $"Issued Asset Id {i}";
            }

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(tagId);
                    bw.Write(uptodateFunds);
                    bw.Write(ownedAssetIdsCount);

                    for (int i = 0; i < ownedAssetIdsCount; i++)
                    {
                        bw.Write(assetIds[i]);
                    }

                    for (int i = 0; i < ownedAssetIdsCount; i++)
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
                        byte strLen = (byte)issuedAssetInfos[i].Length;
                        bw.Write(strLen);
                        bw.Write(Encoding.ASCII.GetBytes(issuedAssetInfos[i]));
                    }

                    bw.Write((byte)issuanceInfo.Length);
                    bw.Write(Encoding.ASCII.GetBytes(issuanceInfo));
                }

                body = ms.ToArray();
            }

            byte[] packet = BinaryBuilder.GetSignedPacket(PacketType.Transactional, syncBlockHeight, nonce, powHash, version,
                BlockTypes.Transaction_IssueAssets, blockHeight, prevHash, body, _privateKey, out byte[] expectedSignature);

            IssueAssetsBlockParser parser = new IssueAssetsBlockParser(_hashCalculationRepository, _identityKeyProvidersRegistry);
            IssueAssetsBlock block = (IssueAssetsBlock)parser.Parse(packet);

            Assert.Equal(syncBlockHeight, block.SyncBlockHeight);
            Assert.Equal(nonce, block.Nonce);
            Assert.Equal(powHash, block.PowHash);
            Assert.Equal(version, block.Version);
            Assert.Equal(blockHeight, block.BlockHeight);
            Assert.Equal(prevHash, block.HashPrev);
            Assert.Equal(uptodateFunds, block.UptodateFunds);
            Assert.Equal(ownedAssetIdsCount, (uint)block.AssetIds.Length);

            for (int i = 0; i < ownedAssetIdsCount; i++)
            {
                Assert.Equal(assetIds[i], block.AssetIds[i]);
                Assert.Equal(assetAmounts[i], block.AssetAmounts[i]);
            }

            Assert.Equal(_publicKey, block.Signer.Value.ToArray());
            Assert.Equal(expectedSignature, block.Signature.ToArray());
        }

        [Fact]
        public void TransferAssetToUtxoBlockParserTest()
        {
            ulong tagId = 147;
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
            ulong[] assetAmount = new ulong[assetsCount];
            byte[][] assetCommitments = new byte[assetsCount][];
            byte[] e = CryptoHelper.GetRandomSeed();
            byte[][] s = new byte[assetsCount][];
            byte[] mask = CryptoHelper.GetRandomSeed();

            Random random = new Random();

            for (int i = 0; i < assetsCount; i++)
            {
                assetIds[i] = CryptoHelper.GetRandomSeed();
                assetAmount[i] = 1;
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
                        bw.Write(assetAmount[i]);
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

            byte[] packet = BinaryBuilder.GetSignedPacket(PacketType.Transactional, syncBlockHeight, nonce, powHash, version,
                BlockTypes.Transaction_TransferAssetsToUtxo, blockHeight, prevHash, body, _privateKey, out byte[] expectedSignature);

            TransferAssetToUtxoBlockParser parser = new TransferAssetToUtxoBlockParser(_hashCalculationRepository, _identityKeyProvidersRegistry);
            TransferAssetToUtxoBlock block = (TransferAssetToUtxoBlock)parser.Parse(packet);

            Assert.Equal(syncBlockHeight, block.SyncBlockHeight);
            Assert.Equal(nonce, block.Nonce);
            Assert.Equal(powHash, block.PowHash);
            Assert.Equal(version, block.Version);
            Assert.Equal(blockHeight, block.BlockHeight);
            Assert.Equal(prevHash, block.HashPrev);
            Assert.Equal(uptodateFunds, block.UptodateFunds);
            Assert.Equal(destinationKey, block.DestinationKey);
            Assert.Equal(transactionPublicKey, block.TransactionPublicKey);
            Assert.Equal(assetId, block.AssetId);
            Assert.Equal(assetCommitment, block.AssetCommitment);
            Assert.Equal(assetsCount, (uint)block.SurjectionProof.AssetCommitments.Length);

            for (int i = 0; i < assetsCount; i++)
            {
                Assert.Equal(assetCommitments[i], block.SurjectionProof.AssetCommitments[i]);
                Assert.Equal(s[i], block.SurjectionProof.Rs.S[i]);
            }
            Assert.Equal(e, block.SurjectionProof.Rs.E);
            Assert.Equal(mask, block.EcdhTuple.Mask);
            Assert.Equal(assetId, block.EcdhTuple.AssetId);
            Assert.Equal(_publicKey, block.Signer.Value.ToArray());
            Assert.Equal(expectedSignature, block.Signature.ToArray());
        }
    }
}
