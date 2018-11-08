using System.IO;
using Wist.BlockLattice.Core.DataModel.UtxoConfidential;
using Wist.BlockLattice.Core.Enums;
using Wist.BlockLattice.Core.Parsers.UtxoConfidential;
using Wist.Core.ExtensionMethods;
using Wist.Core.Cryptography;
using Wist.Tests.Core;
using Xunit;

namespace Wist.BlockLattice.Core.Tests.ParserTests
{
    public class UtxoConfidentialParsersTests : TestBase
    {
        [Fact]
        public void NonQuantitativeAssetTransferBlockParserTest()
        {
            ulong tagId = 199;
            ulong syncBlockHeight = 1;
            uint nonce = 4;
            byte[] powHash = BinaryBuilder.GetPowHash(1234);
            ushort version = 1;
            byte[] body;
            byte[] transactionPublicKey = CryptoHelper.GetRandomSeed();
            byte[] destinationKey = CryptoHelper.GetRandomSeed();
            byte[] keyImage = BinaryBuilder.GetRandomPublicKey();
            byte[] assetCommitment = CryptoHelper.GetRandomSeed();
            ushort pubKeysCount = 10;
            byte[][] assetCommitments = new byte[pubKeysCount][];
            byte[][] pubKeys = new byte[pubKeysCount][];
            byte[] secretKey = null;
            ushort secretKeyIndex = 5;
            byte[] e = CryptoHelper.GetRandomSeed();
            byte[][] s = new byte[pubKeysCount][];
            byte[] mask = CryptoHelper.GetRandomSeed();

            for (int i = 0; i < pubKeysCount; i++)
            {
                pubKeys[i] = BinaryBuilder.GetRandomPublicKey(out byte[] secretKeyTemp);
                if(i == secretKeyIndex)
                {
                    secretKey = secretKeyTemp;
                }
                assetCommitments[i] = CryptoHelper.GetRandomSeed();
                s[i] = CryptoHelper.GetRandomSeed();
            }

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(assetCommitment);
                    bw.Write(pubKeysCount);
                    for (int i = 0; i < pubKeysCount; i++)
                    {
                        bw.Write(assetCommitments[i]);
                    }
                    bw.Write(e);
                    for (int i = 0; i < pubKeysCount; i++)
                    {
                        bw.Write(s[i]);
                    }
                    bw.Write(mask);
                    bw.Write(assetCommitment);
                }

                body = ms.ToArray();
            }

            byte[] packet = BinaryBuilder.GetUtxoConfidentialPacket(PacketType.UtxoConfidential, syncBlockHeight, nonce, powHash, version, 
                BlockTypes.UtxoConfidential_NonQuantitativeAssetTransfer, tagId, keyImage, destinationKey, transactionPublicKey, body, pubKeys, secretKey, secretKeyIndex, 
                out RingSignature[] ringSignatures);
            NonQuantitativeAssetTransferBlockParser parser = new NonQuantitativeAssetTransferBlockParser(_identityKeyProvidersRegistry);
            NonQuantitativeAssetTransferBlock block = (NonQuantitativeAssetTransferBlock)parser.Parse(packet);

            Assert.Equal(syncBlockHeight, block.SyncBlockHeight);
            Assert.Equal(nonce, block.Nonce);
            Assert.Equal(powHash, block.PowHash);
            Assert.Equal(version, block.Version);
            Assert.Equal(keyImage, block.KeyImage.Value.ToArray());
            Assert.Equal(destinationKey, block.DestinationKey);
            Assert.Equal(transactionPublicKey, block.TransactionPublicKey);
            Assert.Equal(assetCommitment, block.AssetCommitment);
            Assert.Equal(pubKeysCount, block.SurjectionProof.AssetCommitments.Length);

            for (int i = 0; i < pubKeysCount; i++)
            {
                Assert.Equal(assetCommitments[i], block.SurjectionProof.AssetCommitments[i]);
                Assert.Equal(s[i], block.SurjectionProof.Rs.S[i]);
                Assert.Equal(ringSignatures[i].C, block.Signatures[i].C);
                Assert.Equal(ringSignatures[i].R, block.Signatures[i].R);
            }

            Assert.Equal(e, block.SurjectionProof.Rs.E);
            Assert.Equal(mask, block.EcdhTuple.Mask);
            Assert.Equal(assetCommitment, block.EcdhTuple.AssetId);
        }
    }
}
