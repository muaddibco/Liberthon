using Chaos.NaCl;
using System;
using System.IO;
using Wist.BlockLattice.Core.Enums;
using Wist.BlockLattice.Core.Parsers.Synchronization;
using Wist.Core.Identity;
using Wist.Core.ExtensionMethods;
using Wist.Tests.Core;
using Xunit;
using NSubstitute;
using Wist.BlockLattice.Core.DataModel.Synchronization;
using Wist.BlockLattice.Core.Parsers.Registry;
using Wist.BlockLattice.Core.DataModel.Registry;
using Wist.Core.Cryptography;
using System.Collections.Generic;
using Wist.BlockLattice.Core.Serializers.Signed.Registry;
using System.Linq;
using Wist.BlockLattice.Core.Parsers;

namespace Wist.BlockLattice.Core.Tests.ParserTests
{
    public class ParsersTests : TestBase
    {
        public ParsersTests() : base()
        {
        }

        [Fact]
        public void SynchronizationConfirmedBlockParserTest()
        {
            byte signersCount = 10;
            byte[] body;
            ushort round = 1;
            ulong syncBlockHeight = 1;
            uint nonce = 4;
            ushort version = 1;
            ulong blockHeight = 9;

            byte[][] expectedSignerPKs = new byte[signersCount][];
            byte[][] expectedSignerSignatures = new byte[signersCount][];

            DateTime expectedDateTime = DateTime.Now;

            byte[] powHash = BinaryBuilder.GetPowHash(1234);
            byte[] prevHash = BinaryBuilder.GetDefaultHash(1234);

            for (int i = 0; i < signersCount; i++)
            {
                byte[] privateSignerKey = CryptoHelper.GetRandomSeed();
                Ed25519.KeyPairFromSeed(out byte[] publicSignerKey, out byte[] expandedSignerKey, privateSignerKey);

                expectedSignerPKs[i] = publicSignerKey;

                byte[] roundBytes = BitConverter.GetBytes(round);

                byte[] signerSignature = Ed25519.Sign(roundBytes, expandedSignerKey);

                expectedSignerSignatures[i] = signerSignature;
            }

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(expectedDateTime.ToBinary());
                    bw.Write(round);
                    bw.Write(signersCount);

                    for (int i = 0; i < signersCount; i++)
                    {
                        bw.Write(expectedSignerPKs[i]);
                        bw.Write(expectedSignerSignatures[i]);
                    }
                }

                body = ms.ToArray();
            }

            byte[] packet = BinaryBuilder.GetSignedPacket(
                PacketType.Synchronization, 
                syncBlockHeight, 
                nonce, powHash, version, 
                BlockTypes.Synchronization_ConfirmedBlock, blockHeight, prevHash, body, _privateKey, out byte[] expectedSignature);
            string packetExpectedString = packet.ToHexString();

            SynchronizationConfirmedBlockParser synchronizationConfirmedBlockParser = new SynchronizationConfirmedBlockParser(_identityKeyProvidersRegistry, _hashCalculationRepository);
            SynchronizationConfirmedBlock block = (SynchronizationConfirmedBlock)synchronizationConfirmedBlockParser.Parse(packet);

            string packetActualString = block.RawData.ToHexString();
            Assert.Equal(syncBlockHeight, block.SyncBlockHeight);
            Assert.Equal(nonce, block.Nonce);
            Assert.Equal(powHash, block.PowHash);
            Assert.Equal(version, block.Version);
            Assert.Equal(blockHeight, block.BlockHeight);
            Assert.Equal(prevHash, block.HashPrev);
            Assert.Equal(expectedDateTime, block.ReportedTime);
            Assert.Equal(round, block.Round);
                
            for (int i = 0; i < signersCount; i++)
            {
                Assert.Equal(expectedSignerPKs[i], block.PublicKeys[i]);
                Assert.Equal(expectedSignerSignatures[i], block.Signatures[i]);
            }

            Assert.Equal(_publicKey, block.Signer.Value.ToArray());
            Assert.Equal(expectedSignature, block.Signature.ToArray());
        }

        [Fact]
        public void RegistryRegisterBlockParserTest()
        {
            ulong syncBlockHeight = 1;
            uint nonce = 4;
            byte[] powHash = BinaryBuilder.GetPowHash(1234);
            ushort version = 1;
            ulong blockHeight = 9;
            byte[] prevHash = null;

            PacketType expectedReferencedPacketType = PacketType.Transactional;
            ushort expectedReferencedBlockType = BlockTypes.Transaction_TransferFunds;
            byte[] expectedReferencedBodyHash = BinaryBuilder.GetDefaultHash(473826643);
            byte[] expectedTarget = BinaryBuilder.GetDefaultHash(BinaryBuilder.GetRandomPublicKey());

            byte[] body;


            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write((ushort)expectedReferencedPacketType);
                    bw.Write(expectedReferencedBlockType);
                    bw.Write(expectedReferencedBodyHash);
                    bw.Write(expectedTarget);
                }

                body = ms.ToArray();
            }

            byte[] packet = BinaryBuilder.GetSignedPacket(
                PacketType.Registry,
                syncBlockHeight,
                nonce, powHash, version,
                BlockTypes.Registry_Register, blockHeight, prevHash, body, _privateKey, out byte[] expectedSignature);

            RegistryRegisterBlockParser registryRegisterBlockParser = new RegistryRegisterBlockParser(_identityKeyProvidersRegistry, _hashCalculationRepository);
            RegistryRegisterBlock block = (RegistryRegisterBlock)registryRegisterBlockParser.Parse(packet);

            Assert.Equal(syncBlockHeight, block.SyncBlockHeight);
            Assert.Equal(nonce, block.Nonce);
            Assert.Equal(powHash, block.PowHash);
            Assert.Equal(version, block.Version);
            Assert.Equal(blockHeight, block.BlockHeight);

            Assert.Equal(expectedReferencedPacketType, block.ReferencedPacketType);
            Assert.Equal(expectedReferencedBlockType, block.ReferencedBlockType);
            Assert.Equal(expectedReferencedBodyHash, block.ReferencedBodyHash);
            Assert.Equal(expectedTarget, block.ReferencedTarget);

            Assert.Equal(_publicKey, block.Signer.Value.ToArray());
            Assert.Equal(expectedSignature, block.Signature.ToArray());
        }

        [Fact]
        public void RegistryShortBlockParserTest()
        {
            ulong syncBlockHeight = 1;
            uint nonce = 4;
            byte[] powHash = BinaryBuilder.GetPowHash(1234);
            ushort version = 1;
            ulong blockHeight = 9;
            byte[] prevHash = null;

            byte[] body;

            ushort expectedCount = 10;

            SortedList<ushort, IKey> transactionHeaders = new SortedList<ushort, IKey>();
            for (ushort i = 0; i < expectedCount; i++)
            {
                transactionHeaders.Add(i, new Key16(BinaryBuilder.GetTransactionKeyHash(i)));
            }

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write((ushort)transactionHeaders.Count);

                    foreach (ushort order in transactionHeaders.Keys)
                    {
                        bw.Write(order);
                        bw.Write(transactionHeaders[order].Value.ToArray());
                    }
                }

                body = ms.ToArray();
            }

            byte[] packet = BinaryBuilder.GetSignedPacket(
                PacketType.Registry,
                syncBlockHeight,
                nonce, powHash, version,
                BlockTypes.Registry_ShortBlock, blockHeight, prevHash, body, _privateKey, out byte[] expectedSignature);

            RegistryShortBlockParser registryFullBlockParser = new RegistryShortBlockParser(_identityKeyProvidersRegistry, _hashCalculationRepository);
            RegistryShortBlock block = (RegistryShortBlock)registryFullBlockParser.Parse(packet);

            Assert.Equal(syncBlockHeight, block.SyncBlockHeight);
            Assert.Equal(nonce, block.Nonce);
            Assert.Equal(powHash, block.PowHash);
            Assert.Equal(version, block.Version);
            Assert.Equal(blockHeight, block.BlockHeight);

            foreach (var item in transactionHeaders)
            {
                Assert.True(block.TransactionHeaderHashes.ContainsKey(item.Key));
                Assert.Equal(item.Value, block.TransactionHeaderHashes[item.Key]);
            }

            Assert.Equal(_publicKey, block.Signer.Value.ToArray());
            Assert.Equal(expectedSignature, block.Signature.ToArray());
        }

        [Fact]
        public void RegistryFullBlockParserTest()
        {
            IBlockParser blockParser = new RegistryRegisterBlockParser(_identityKeyProvidersRegistry, _hashCalculationRepository);
            _blockParsersRepository.GetInstance(0).ReturnsForAnyArgs(blockParser);

            ulong syncBlockHeight = 1;
            uint nonce = 4;
            byte[] powHash = BinaryBuilder.GetPowHash(1234);
            ushort version = 1;
            ulong blockHeight = 9;
            byte[] prevHash = null;

            PacketType expectedReferencedPacketType = PacketType.Transactional;
            ushort expectedReferencedBlockType = BlockTypes.Transaction_TransferFunds;

            byte[] body;

            ushort expectedCount = 1000;
            byte[] expectedShortBlockHash;

            SortedList<ushort, RegistryRegisterBlock> transactionHeaders = new SortedList<ushort, RegistryRegisterBlock>();
            for (ushort i = 0; i < expectedCount; i++)
            {
                RegistryRegisterBlock registryRegisterBlock = new RegistryRegisterBlock
                {
                    SyncBlockHeight = syncBlockHeight,
                    Nonce = nonce + i,
                    PowHash = BinaryBuilder.GetPowHash(1234 + i),
                    BlockHeight = blockHeight,
                    ReferencedPacketType = expectedReferencedPacketType,
                    ReferencedBlockType = expectedReferencedBlockType,
                    ReferencedBodyHash = BinaryBuilder.GetDefaultHash(473826643 + i),
                    ReferencedTarget = BinaryBuilder.GetDefaultHash(BinaryBuilder.GetRandomPublicKey())
                };

                RegistryRegisterBlockSerializer serializer1 = new RegistryRegisterBlockSerializer(_cryptoService, _identityKeyProvidersRegistry, _hashCalculationRepository);
                serializer1.Initialize(registryRegisterBlock);
                serializer1.FillBodyAndRowBytes();

                transactionHeaders.Add(i, registryRegisterBlock);
            }

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write((ushort)transactionHeaders.Count);

                    foreach (ushort order in transactionHeaders.Keys)
                    {
                        bw.Write(order);
                        bw.Write(transactionHeaders[order].RawData.ToArray());
                    }

                    expectedShortBlockHash = BinaryBuilder.GetDefaultHash(1111);
                    bw.Write(expectedShortBlockHash);
                }

                body = ms.ToArray();
            }

            byte[] packet = BinaryBuilder.GetSignedPacket(
                PacketType.Registry,
                syncBlockHeight,
                nonce, powHash, version,
                BlockTypes.Registry_FullBlock, blockHeight, prevHash, body, _privateKey, out byte[] expectedSignature);

            RegistryFullBlockParser registryFullBlockParser = new RegistryFullBlockParser(_identityKeyProvidersRegistry, _hashCalculationRepository);
            RegistryFullBlock block = (RegistryFullBlock)registryFullBlockParser.Parse(packet);

            Assert.Equal(syncBlockHeight, block.SyncBlockHeight);
            Assert.Equal(nonce, block.Nonce);
            Assert.Equal(powHash, block.PowHash);
            Assert.Equal(version, block.Version);
            Assert.Equal(blockHeight, block.BlockHeight);

            foreach (var item in transactionHeaders)
            {
                RegistryRegisterBlock registryRegisterBlock = (RegistryRegisterBlock)block.TransactionHeaders[item.Key];
                Assert.True(block.TransactionHeaders.ContainsKey(item.Key));
                Assert.Equal(item.Value.PacketType, registryRegisterBlock.PacketType);
                Assert.Equal(item.Value.SyncBlockHeight, registryRegisterBlock.SyncBlockHeight);
                Assert.Equal(item.Value.Nonce, registryRegisterBlock.Nonce);
                Assert.Equal(item.Value.PowHash, registryRegisterBlock.PowHash);
                Assert.Equal(item.Value.BlockHeight, registryRegisterBlock.BlockHeight);
                Assert.Equal(item.Value.BlockType, registryRegisterBlock.BlockType);
                Assert.Equal(item.Value.ReferencedPacketType, registryRegisterBlock.ReferencedPacketType);
                Assert.Equal(item.Value.ReferencedBlockType, registryRegisterBlock.ReferencedBlockType);
                Assert.Equal(item.Value.ReferencedBodyHash, registryRegisterBlock.ReferencedBodyHash);
                Assert.Equal(item.Value.ReferencedTarget, registryRegisterBlock.ReferencedTarget);
                Assert.Equal(item.Value.Signature.ToArray(), registryRegisterBlock.Signature.ToArray());
                Assert.Equal(item.Value.Signer, registryRegisterBlock.Signer);
            }

            Assert.Equal(expectedShortBlockHash, block.ShortBlockHash);

            Assert.Equal(_publicKey, block.Signer.Value.ToArray());
            Assert.Equal(expectedSignature, block.Signature.ToArray());
        }

        [Fact]
        public void RegistryConfidenceBlockParserTest()
        {
            ulong syncBlockHeight = 1;
            uint nonce = 4;
            byte[] powHash = BinaryBuilder.GetPowHash(1234);
            ushort version = 1;
            ulong blockHeight = 9;
            byte[] prevHash = null;

            Random randNum = new Random();
            ushort bitMaskLength = 375;
            byte[] bitMask = Enumerable.Repeat(0, bitMaskLength).Select(i => (byte)randNum.Next(0, 255)).ToArray();
            byte[] expectedProof = Enumerable.Repeat(0, 16).Select(i => (byte)randNum.Next(0, 255)).ToArray();
            byte[] expectedReferencedBodyHash = BinaryBuilder.GetDefaultHash(473826643);

            byte[] body;

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write((ushort)bitMask.Length);
                    bw.Write(bitMask);
                    bw.Write(expectedProof);
                    bw.Write(expectedReferencedBodyHash);
                }

                body = ms.ToArray();
            }

            byte[] packet = BinaryBuilder.GetSignedPacket(
                PacketType.Registry,
                syncBlockHeight,
                nonce, powHash, version,
                BlockTypes.Registry_ConfidenceBlock, blockHeight, prevHash, body, _privateKey, out byte[] expectedSignature);

            RegistryConfidenceBlockParser registryFullBlockParser = new RegistryConfidenceBlockParser(_identityKeyProvidersRegistry, _hashCalculationRepository);
            RegistryConfidenceBlock block = (RegistryConfidenceBlock)registryFullBlockParser.Parse(packet);

            Assert.Equal(syncBlockHeight, block.SyncBlockHeight);
            Assert.Equal(nonce, block.Nonce);
            Assert.Equal(powHash, block.PowHash);
            Assert.Equal(version, block.Version);
            Assert.Equal(blockHeight, block.BlockHeight);

            Assert.Equal(bitMask, block.BitMask);
            Assert.Equal(expectedProof, block.ConfidenceProof);
            Assert.Equal(expectedReferencedBodyHash, block.ReferencedBlockHash);

            Assert.Equal(_publicKey, block.Signer.Value.ToArray());
            Assert.Equal(expectedSignature, block.Signature.ToArray());
        }

        [Fact]
        public void SynchronizationRegistryCombinedBlockParserTest()
        {
            ulong syncBlockHeight = 1;
            uint nonce = 4;
            byte[] powHash = BinaryBuilder.GetPowHash(1234);
            ushort version = 1;
            ulong blockHeight = 9;
            byte[] prevHash = BinaryBuilder.GetDefaultHash(1234);

            byte[] body;

            DateTime expectedDateTime = DateTime.Now;
            byte[][] expectedHashes = new byte[2][] { CryptoHelper.GetRandomSeed(), CryptoHelper.GetRandomSeed() };

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(expectedDateTime.ToBinary());
                    bw.Write((ushort)2);
                    bw.Write(expectedHashes[0]);
                    bw.Write(expectedHashes[1]);
                }

                body = ms.ToArray();
            }

            byte[] packet = BinaryBuilder.GetSignedPacket(
                PacketType.Synchronization,
                syncBlockHeight,
                nonce, powHash, version,
                BlockTypes.Synchronization_RegistryCombinationBlock, blockHeight, prevHash, body, _privateKey, out byte[] expectedSignature);


            SynchronizationRegistryCombinedBlockParser parser = new SynchronizationRegistryCombinedBlockParser(_identityKeyProvidersRegistry, _hashCalculationRepository);
            SynchronizationRegistryCombinedBlock block = (SynchronizationRegistryCombinedBlock)parser.Parse(packet);

            Assert.Equal(syncBlockHeight, block.SyncBlockHeight);
            Assert.Equal(nonce, block.Nonce);
            Assert.Equal(powHash, block.PowHash);
            Assert.Equal(version, block.Version);
            Assert.Equal(blockHeight, block.BlockHeight);

            Assert.Equal(expectedHashes.Length, block.BlockHashes.Length);
            for (int i = 0; i < expectedHashes.Length; i++)
            {
                Assert.Equal(expectedHashes[i], block.BlockHashes[i]);
            }

            Assert.Equal(_publicKey, block.Signer.Value.ToArray());
            Assert.Equal(expectedSignature, block.Signature.ToArray());
        }
    }
}
