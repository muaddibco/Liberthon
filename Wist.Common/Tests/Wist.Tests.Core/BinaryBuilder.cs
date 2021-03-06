﻿using Chaos.NaCl;
using HashLib;
using System;
using System.IO;
using System.Security.Cryptography;
using Wist.BlockLattice.Core;
using Wist.BlockLattice.Core.Enums;
using Wist.Core.Cryptography;
using Wist.Crypto.ConfidentialAssets;

namespace Wist.Tests.Core
{
    public class BinaryBuilder
    {
        public static byte STX = 2;
        public static byte DLE = 10;

        private readonly byte[] _defaultPrivateKey;

        public BinaryBuilder(byte[] defaultPrivateKey)
        {
            _defaultPrivateKey = defaultPrivateKey;
        }

        public byte[] GetSignedPacket(PacketType packetType, ulong syncBlockHeight, uint nonce, byte[] powHash, ushort version, ushort blockType, ulong blockHeight, byte[] prevHash, byte[] body, out byte[] signature)
        {
            return GetSignedPacket(packetType, syncBlockHeight, nonce, powHash, version, blockType, blockHeight, prevHash, body, _defaultPrivateKey, out signature);
        }

        public static byte[] GetSignedPacket(PacketType packetType, ulong syncBlockHeight, uint nonce, byte[] powHash, ushort version, ushort blockType, ulong blockHeight, byte[] prevHash, byte[] body, byte[] privateKey, out byte[] signature)
        {
            byte[] result = null;

            byte[] bodyBytes = null;

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(version);
                    bw.Write(blockType);
                    bw.Write(blockHeight);
                    if (prevHash != null)
                    {
                        bw.Write(prevHash);
                    }
                    bw.Write(body);
                }

                bodyBytes = ms.ToArray();
            }

            byte[] publickKey = Ed25519.PublicKeyFromSeed(privateKey);
            byte[] expandedPrivateKey = Ed25519.ExpandedPrivateKeyFromSeed(privateKey);
            signature = Ed25519.Sign(bodyBytes, expandedPrivateKey);

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write((ushort)packetType);
                    bw.Write(syncBlockHeight);
                    bw.Write(nonce);
                    bw.Write(powHash);
                    bw.Write(bodyBytes);
                    bw.Write(signature);
                    bw.Write(publickKey);
                }

                result = ms.ToArray();
            }

            return result;
        }

        public static byte[] GetUtxoConfidentialPacket(PacketType packetType, ulong syncBlockHeight, uint nonce, byte[] powHash, ushort version, ushort blockType, ulong tagId, byte[] keyImage, byte[] destinationKey, byte[] transactionPublicKey, byte[] body, byte[][] pubKeys, byte[] secretKey, int secIndex, out RingSignature[] ringSignatures)
        {
            byte[] bodyBytes = null;
            byte[] result = null;

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(version);
                    bw.Write(blockType);
                    bw.Write(tagId);
                    bw.Write(keyImage);
                    bw.Write(destinationKey);
                    bw.Write(transactionPublicKey);
                    bw.Write(body);
                }

                bodyBytes = ms.ToArray();
            }

            ringSignatures = ConfidentialAssetsHelper.GenerateRingSignature(bodyBytes, keyImage, pubKeys, secretKey, secIndex);

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write((ushort)packetType);
                    bw.Write(syncBlockHeight);
                    bw.Write(nonce);
                    bw.Write(powHash);
                    bw.Write(bodyBytes);
                    bw.Write((ushort)ringSignatures.Length);
                    for (int i = 0; i < pubKeys.Length; i++)
                    {
                        bw.Write(pubKeys[i]);
                    }
                    for (int i = 0; i < pubKeys.Length; i++)
                    {
                        bw.Write(ringSignatures[i].C);
                        bw.Write(ringSignatures[i].R);
                    }
                }

                result = ms.ToArray();
            }

            return result;
        }

        public static byte[] GetRandomPublicKey()
        {
            byte[] seed = CryptoHelper.GetRandomSeed();

            return Ed25519.PublicKeyFromSeed(seed);
        }

        public static byte[] GetRandomPublicKey(out byte[] secretKey)
        {
            secretKey = CryptoHelper.GetRandomSeed();

            return Ed25519.PublicKeyFromSeed(secretKey);
        }

        public static byte[] GetTransactionKeyHash(int forValue = 0)
        {
            IHash hash = HashLib.HashFactory.Hash128.CreateMurmur3_128();
            byte[] originBytes = BitConverter.GetBytes(forValue);
            byte[] hashBytes = hash.ComputeBytes(originBytes).GetBytes();

            return hashBytes;
        }

        public static byte[] GetPowHash(int forValue = 0)
        {
            IHash hash = HashLib.HashFactory.Crypto.CreateTiger_4_192();
            byte[] powHashOrigin = BitConverter.GetBytes(forValue);
            byte[] powHash = hash.ComputeBytes(powHashOrigin).GetBytes();

            return powHash;
        }

        public static byte[] GetDefaultHash(int forValue = 0)
        {
            IHash hash = HashLib.HashFactory.Crypto.CreateSHA256();
            byte[] hashOrigin = BitConverter.GetBytes(forValue);
            byte[] hashBytes = hash.ComputeBytes(hashOrigin).GetBytes();

            return hashBytes;
        }

        public static byte[] GetDefaultHash(byte[] bytes)
        {
            IHash hash = HashLib.HashFactory.Crypto.CreateSHA256();
            byte[] hashBytes = hash.ComputeBytes(bytes).GetBytes();

            return hashBytes;
        }
    }
}
