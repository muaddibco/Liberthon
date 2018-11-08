using System;
using System.Buffers.Binary;
using Wist.BlockLattice.Core.DataModel.UtxoConfidential;
using Wist.BlockLattice.Core.Enums;
using Wist.BlockLattice.Core.Exceptions;
using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;
using Wist.Core.Cryptography;
using Wist.Core.Identity;

namespace Wist.BlockLattice.Core.Parsers.UtxoConfidential
{
    [RegisterExtension(typeof(IBlockParser), Lifetime = LifetimeManagement.Singleton)]
    public class NonQuantitativeTransitionAssetTransferBlockParser : UtxoConfidentialParserBase
    {
        public NonQuantitativeTransitionAssetTransferBlockParser(IIdentityKeyProvidersRegistry identityKeyProvidersRegistry) : base(identityKeyProvidersRegistry)
        {
        }

        public override ushort BlockType => BlockTypes.UtxoConfidential_NonQuantitativeAssetTransfer;

        protected override Memory<byte> ParseUtxoConfidential(ushort version, Memory<byte> spanBody, out UtxoConfidentialBase utxoConfidentialBase)
        {
            UtxoConfidentialBase block = null;

            if (version == 1)
            {
                int readBytes = 0;

                ReadCommitmentAndProof(ref spanBody, ref readBytes, out byte[] assetCommitment, out SurjectionProof surjectionProof);
                ReadCommitmentAndProof(ref spanBody, ref readBytes, out byte[] affiliationAssetCommitment, out SurjectionProof affiliationSurjectionProof);

                ushort affiliationPseudoKeysCount = BinaryPrimitives.ReadUInt16LittleEndian(spanBody.Span.Slice(readBytes));
                readBytes += 2;

                byte[][] affiliationPseudoKeys = new byte[affiliationPseudoKeysCount][];
                for (int i = 0; i < affiliationPseudoKeysCount; i++)
                {
                    affiliationPseudoKeys[i] = spanBody.Slice(readBytes, 32).ToArray();
                    readBytes += 32;
                }

                byte[] e = spanBody.Slice(readBytes, 32).ToArray();
                readBytes += 32;

                ushort sCount = BinaryPrimitives.ReadUInt16LittleEndian(spanBody.Span.Slice(readBytes));
                readBytes += 2;

                byte[][] s = new byte[sCount][];
                for (int i = 0; i < sCount; i++)
                {
                    s[i] = spanBody.Slice(readBytes, 32).ToArray();
                    readBytes += 32;
                }

                BorromeanRingSignature borromeanRingSignature = new BorromeanRingSignature { E = e, S = s };

                SurjectionProof surjectionEvidenceProof = GetSurhectionProof(ref spanBody, ref readBytes);

                ReadEcdhTupleCA(ref spanBody, ref readBytes, out byte[] mask, out byte[] assetId);

                block = new NonQuantitativeTransitionAssetTransferBlock
                {
                    AssetCommitment = assetCommitment,
                    SurjectionProof = surjectionProof,
                    AffiliationCommitment = affiliationAssetCommitment,
                    AffiliationSurjectionProof = affiliationSurjectionProof,
                    AffiliationPseudoKeys = affiliationPseudoKeys,
                    AffiliationBorromeanSignature = borromeanRingSignature,
                    AffiliationEvidenceSurjectionProof = surjectionEvidenceProof,
                    EcdhTuple = new EcdhTupleCA
                    {
                        Mask = mask,
                        AssetId = assetId
                    }
                };

                utxoConfidentialBase = block;

                return spanBody.Slice(readBytes);
            }

            throw new BlockVersionNotSupportedException(version, BlockType);
        }

        private static void ReadEcdhTupleCA(ref Memory<byte> spanBody, ref int readBytes, out byte[] mask, out byte[] assetId)
        {
            mask = spanBody.Slice(readBytes, 32).ToArray();
            readBytes += 32;

            assetId = spanBody.Slice(readBytes, 32).ToArray();
            readBytes += 32;
        }

        private static void ReadCommitmentAndProof(ref Memory<byte> spanBody, ref int readBytes, out byte[] assetCommitment, out SurjectionProof surjectionProof)
        {
            assetCommitment = spanBody.Slice(readBytes, 32).ToArray();
            readBytes += 32;

            surjectionProof = GetSurhectionProof(ref spanBody, ref readBytes);
        }

        private static SurjectionProof GetSurhectionProof(ref Memory<byte> spanBody, ref int readBytes)
        {
            SurjectionProof surjectionProof;
            ushort assetCommitmentsCount = BinaryPrimitives.ReadUInt16LittleEndian(spanBody.Slice(readBytes).Span);
            readBytes += 2;

            byte[][] assetCommitments = new byte[assetCommitmentsCount][];
            for (int i = 0; i < assetCommitmentsCount; i++)
            {
                assetCommitments[i] = spanBody.Slice(readBytes, 32).ToArray();
                readBytes += 32;
            }

            byte[] e = spanBody.Slice(readBytes, 32).ToArray();
            readBytes += 32;

            byte[][] s = new byte[assetCommitmentsCount][];
            for (int i = 0; i < assetCommitmentsCount; i++)
            {
                s[i] = spanBody.Slice(readBytes, 32).ToArray();
                readBytes += 32;
            }

            surjectionProof = new SurjectionProof
            {
                AssetCommitments = assetCommitments,
                Rs = new BorromeanRingSignature
                {
                    E = e,
                    S = s
                }
            };
            return surjectionProof;
        }
    }
}
