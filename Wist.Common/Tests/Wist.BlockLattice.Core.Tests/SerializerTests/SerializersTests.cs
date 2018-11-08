using Chaos.NaCl;
using System;
using System.Collections.Generic;
using Wist.BlockLattice.Core.DataModel.Synchronization;
using Wist.BlockLattice.Core.Enums;
using Wist.BlockLattice.Core.Serializers.Signed.Synchronization;
using Wist.Core.Cryptography;
using Wist.Core.Identity;
using Wist.Tests.Core;
using Wist.Core.ExtensionMethods;
using Xunit;
using System.IO;
using Wist.BlockLattice.Core.DataModel.Registry;
using Wist.BlockLattice.Core.Serializers.Signed.Registry;
using System.Diagnostics;
using System.Linq;

namespace Wist.BlockLattice.Core.Tests.SerializerTests
{
    public class SerializersTests : TestBase
    {

        public SerializersTests() : base()
        {

        }

        [Fact]
        public void SynchronizationConfirmedBlockSerializerTest()
        {
            ulong syncBlockHeight = 1;
            uint nonce = 4;
            byte[] powHash = BinaryBuilder.GetPowHash(1234);
            ushort version = 1;
            ulong blockHeight = 9;
            byte[] prevHash = BinaryBuilder.GetDefaultHash(1234);

            ushort round = 1;
            byte signersCount = 10;
            byte[] body = new byte[11 + Globals.NODE_PUBLIC_KEY_SIZE * signersCount + Globals.SIGNATURE_SIZE * signersCount];

            byte[][] expectedSignerPKs = new byte[signersCount][];
            byte[][] expectedSignerSignatures = new byte[signersCount][];

            DateTime expectedDateTime = DateTime.Now;


            for (int i = 0; i < signersCount; i++)
            {
                byte[] privateSignerKey = CryptoHelper.GetRandomSeed();

                Ed25519.KeyPairFromSeed(out byte[] publicSignerKey, out byte[] expandedSignerKey, privateSignerKey);

                expectedSignerPKs[i] = publicSignerKey;

                byte[] roundBytes = BitConverter.GetBytes(round);
                byte[] signerSignature = Ed25519.Sign(roundBytes, expandedSignerKey);

                expectedSignerSignatures[i] = signerSignature;
            }

            using (MemoryStream ms = new MemoryStream(body))
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
            }

            byte[] expectedPacket = BinaryBuilder.GetSignedPacket(
                PacketType.Synchronization,
                syncBlockHeight,
                nonce, powHash, version,
                BlockTypes.Synchronization_ConfirmedBlock, blockHeight, prevHash, body, _privateKey, out byte[] expectedSignature);

            SynchronizationConfirmedBlock block = new SynchronizationConfirmedBlock()
            {
                SyncBlockHeight = syncBlockHeight,
                BlockHeight = blockHeight,
                Nonce = nonce,
                PowHash = powHash,
                HashPrev = prevHash,
                ReportedTime = expectedDateTime,
                Round = round,
                PublicKeys = new byte[signersCount][],
                Signatures = new byte[signersCount][]
            };

            for (int i = 0; i < signersCount; i++)
            {
                block.PublicKeys[i] = expectedSignerPKs[i];
                block.Signatures[i] = expectedSignerSignatures[i];
            }

            SynchronizationConfirmedBlockSerializer serializer = new SynchronizationConfirmedBlockSerializer(_cryptoService, _identityKeyProvidersRegistry, _hashCalculationRepository);
            serializer.Initialize(block);

            byte[] actualPacket = serializer.GetBytes();

            Assert.Equal(expectedPacket, actualPacket);
        }

        [Fact]
        public void RegistryRegisterBlockSerializerTest()
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

            byte[] expectedPacket = BinaryBuilder.GetSignedPacket(
                PacketType.Registry,
                syncBlockHeight,
                nonce, powHash, version,
                BlockTypes.Registry_Register, blockHeight, prevHash, body, _privateKey, out byte[] expectedSignature);

            RegistryRegisterBlock block = new RegistryRegisterBlock
            {
                SyncBlockHeight = syncBlockHeight,
                Nonce = nonce,
                PowHash = powHash,
                BlockHeight = blockHeight,
                ReferencedPacketType = expectedReferencedPacketType,
                ReferencedBlockType = expectedReferencedBlockType,
                ReferencedBodyHash = expectedReferencedBodyHash,
                ReferencedTarget = expectedTarget
            };

            RegistryRegisterBlockSerializer serializer = new RegistryRegisterBlockSerializer(_cryptoService, _identityKeyProvidersRegistry, _hashCalculationRepository);
            serializer.Initialize(block);

            byte[] actualPacket = serializer.GetBytes();

            Trace.WriteLine(expectedPacket.ToHexString());
            Trace.WriteLine(actualPacket.ToHexString());

            Assert.Equal(expectedPacket, actualPacket);
        }

        [Fact]
        public void RegistryShortBlockSerializerTest()
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

            byte[] expectedPacket = BinaryBuilder.GetSignedPacket(
                PacketType.Registry,
                syncBlockHeight,
                nonce, powHash, version,
                BlockTypes.Registry_ShortBlock, blockHeight, prevHash, body, _privateKey, out byte[] expectedSignature);

            RegistryShortBlock block = new RegistryShortBlock
            {
                SyncBlockHeight = syncBlockHeight,
                Nonce = nonce,
                PowHash = powHash,
                BlockHeight = blockHeight,
                TransactionHeaderHashes = transactionHeaders
            };

            RegistryShortBlockSerializer serializer = new RegistryShortBlockSerializer(_cryptoService, _identityKeyProvidersRegistry, _hashCalculationRepository);
            serializer.Initialize(block);

            byte[] actualPacket = serializer.GetBytes();

            Trace.WriteLine(expectedPacket.ToHexString());
            Trace.WriteLine(actualPacket.ToHexString());

            Assert.Equal(expectedPacket, actualPacket);
        }

        [Fact]
        public void RegistryFullBlockSerializerTest()
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

            ushort expectedCount = 1000;

            SortedList<ushort, ITransactionRegistryBlock> transactionHeaders = new SortedList<ushort, ITransactionRegistryBlock>();
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

                    bw.Write(BinaryBuilder.GetDefaultHash(1111));
                }

                body = ms.ToArray();
            }

            byte[] expectedPacket = BinaryBuilder.GetSignedPacket(
                PacketType.Registry,
                syncBlockHeight,
                nonce, powHash, version,
                BlockTypes.Registry_FullBlock, blockHeight, prevHash, body, _privateKey, out byte[] expectedSignature);

            RegistryFullBlock block = new RegistryFullBlock
            {
                SyncBlockHeight = syncBlockHeight,
                Nonce = nonce,
                PowHash = powHash,
                BlockHeight = blockHeight,
                TransactionHeaders = transactionHeaders,
                ShortBlockHash = BinaryBuilder.GetDefaultHash(1111)
            };

            RegistryFullBlockSerializer serializer = new RegistryFullBlockSerializer(_cryptoService, _identityKeyProvidersRegistry, _hashCalculationRepository);
            serializer.Initialize(block);

            byte[] actualPacket = serializer.GetBytes();

            Trace.WriteLine(expectedPacket.ToHexString());
            Trace.WriteLine(actualPacket.ToHexString());

            Assert.Equal(expectedPacket, actualPacket);
        }

        [Fact]
        public void RegistryConfidenceBlockSerializerTest()
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

            byte[] expectedPacket = BinaryBuilder.GetSignedPacket(
                PacketType.Registry,
                syncBlockHeight,
                nonce, powHash, version,
                BlockTypes.Registry_ConfidenceBlock, blockHeight, prevHash, body, _privateKey, out byte[] expectedSignature);

            RegistryConfidenceBlock block = new RegistryConfidenceBlock
            {
                SyncBlockHeight = syncBlockHeight,
                Nonce = nonce,
                PowHash = powHash,
                BlockHeight = blockHeight,
                BitMask = bitMask,
                ConfidenceProof = expectedProof,
                ReferencedBlockHash = expectedReferencedBodyHash
            };

            RegistryConfidenceBlockSerializer serializer = new RegistryConfidenceBlockSerializer(_cryptoService, _identityKeyProvidersRegistry, _hashCalculationRepository);
            serializer.Initialize(block);

            byte[] actualPacket = serializer.GetBytes();

            Trace.WriteLine(expectedPacket.ToHexString());
            Trace.WriteLine(actualPacket.ToHexString());

            Assert.Equal(expectedPacket, actualPacket);
        }

        [Fact]
        public void SynchronizationRegistryCombinedBlockSerializerTest()
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

            byte[] expectedPacket = BinaryBuilder.GetSignedPacket(
                PacketType.Synchronization,
                syncBlockHeight,
                nonce, powHash, version,
                BlockTypes.Synchronization_RegistryCombinationBlock, blockHeight, prevHash, body, _privateKey, out byte[] expectedSignature);

            SynchronizationRegistryCombinedBlock block = new SynchronizationRegistryCombinedBlock
            {
                SyncBlockHeight = syncBlockHeight,
                Nonce = nonce,
                PowHash = powHash,
                BlockHeight = blockHeight,
                HashPrev = prevHash,
                ReportedTime = expectedDateTime,
                BlockHashes = expectedHashes
            };

            SynchronizationRegistryCombinedBlockSerializer serializer = new SynchronizationRegistryCombinedBlockSerializer(_cryptoService, _identityKeyProvidersRegistry, _hashCalculationRepository);
            serializer.Initialize(block);

            byte[] actualPacket = serializer.GetBytes();

            Trace.WriteLine(expectedPacket.ToHexString());
            Trace.WriteLine(actualPacket.ToHexString());

            Assert.Equal(expectedPacket, actualPacket);
        }
    }
}
