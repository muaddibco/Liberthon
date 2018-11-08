using System;
using Wist.BlockLattice.Core.DataModel.Transactional;
using Wist.BlockLattice.Core.Enums;
using Wist.BlockLattice.Core.Exceptions;
using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;
using Wist.Core.Cryptography;
using Wist.Core.HashCalculations;
using Wist.Core.Identity;

namespace Wist.BlockLattice.Core.Parsers.Transactional
{
    [RegisterExtension(typeof(IBlockParser), Lifetime = LifetimeManagement.Singleton)]
    public class TransferAssetToUtxoBlockParser : TransactionalBlockParserBase
    {
        public TransferAssetToUtxoBlockParser(IHashCalculationsRepository proofOfWorkCalculationRepository, IIdentityKeyProvidersRegistry identityKeyProvidersRegistry) 
            : base(proofOfWorkCalculationRepository, identityKeyProvidersRegistry)
        {
        }

        public override ushort BlockType => BlockTypes.Transaction_TransferAssetsToUtxo;

        protected override Memory<byte> ParseTransactional(ushort version, Memory<byte> spanBody, uint assetsCount, out TransactionalBlockBase transactionalBlockBase)
        {
            TransactionalBlockBase block = null;

            if (version == 1)
            {
                int readBytes = 0;

                byte[] destinationKey = spanBody.Slice(readBytes, 32).ToArray();
                readBytes += 32;

                byte[] transactionPublicKey = spanBody.Slice(readBytes, 32).ToArray();
                readBytes += 32;

                byte[] assetId = spanBody.Slice(readBytes, 32).ToArray();
                readBytes += 32;

                byte[] assetCommitment = spanBody.Slice(readBytes, 32).ToArray();
                readBytes += 32;

                byte[][] assetCommitments = new byte[assetsCount][];
                for (int i = 0; i < assetsCount; i++)
                {
                    assetCommitments[i] = spanBody.Slice(readBytes, 32).ToArray();
                    readBytes += 32;
                }

                byte[] e = spanBody.Slice(readBytes, 32).ToArray();
                readBytes += 32;

                byte[][] s = new byte[assetsCount][];
                for (int i = 0; i < assetsCount; i++)
                {
                    s[i] = spanBody.Slice(readBytes, 32).ToArray();
                    readBytes += 32;
                }

                byte[] mask = spanBody.Slice(readBytes, 32).ToArray();
                readBytes += 32;
                byte[] asset = spanBody.Slice(readBytes, 32).ToArray();
                readBytes += 32;

                SurjectionProof surjectionProof = new SurjectionProof
                {
                    AssetCommitments = assetCommitments,
                    Rs = new BorromeanRingSignature
                    {
                        E = e,
                        S = s
                    }
                };

                block = new TransferAssetToUtxoBlock
                {
                    TransactionPublicKey = transactionPublicKey,
                    DestinationKey = destinationKey,
                    AssetId = assetId,
                    AssetCommitment = assetCommitment,
                    SurjectionProof = surjectionProof,
                    EcdhTuple = new EcdhTupleCA
                    {
                        Mask = mask,
                        AssetId = asset
                    }
                };

                transactionalBlockBase = block;
                return spanBody.Slice(readBytes);
            }

            throw new BlockVersionNotSupportedException(version, BlockType);
        }
    }
}
