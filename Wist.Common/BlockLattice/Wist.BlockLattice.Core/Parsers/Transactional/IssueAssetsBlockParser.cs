using System;
using System.Buffers.Binary;
using System.Text;
using Wist.BlockLattice.Core.DataModel.Transactional;
using Wist.BlockLattice.Core.Enums;
using Wist.BlockLattice.Core.Exceptions;
using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;
using Wist.Core.HashCalculations;
using Wist.Core.Identity;

namespace Wist.BlockLattice.Core.Parsers.Transactional
{
    [RegisterExtension(typeof(IBlockParser), Lifetime = LifetimeManagement.Singleton)]
    public class IssueAssetsBlockParser : TransactionalBlockParserBase
    {
        public IssueAssetsBlockParser(IHashCalculationsRepository proofOfWorkCalculationRepository, IIdentityKeyProvidersRegistry identityKeyProvidersRegistry) 
            : base(proofOfWorkCalculationRepository, identityKeyProvidersRegistry)
        {
        }

        public override ushort BlockType => BlockTypes.Transaction_IssueAssets;

        protected override Memory<byte> ParseTransactional(ushort version, Memory<byte> spanBody, uint assetCount, out TransactionalBlockBase transactionalBlockBase)
        {
            if (version == 1)
            {
                int readBytes = 0;

                uint count = BinaryPrimitives.ReadUInt32LittleEndian(spanBody.Span.Slice(readBytes));
                readBytes += sizeof(uint);

                byte[][] issuedAssetIds = new byte[count][];
                string[] issuedAssetInfos = new string[count];

                for (int i = 0; i < count; i++)
                {
                    issuedAssetIds[i] = spanBody.Slice(readBytes, Globals.NODE_PUBLIC_KEY_SIZE).ToArray();
                    readBytes += Globals.NODE_PUBLIC_KEY_SIZE;
                }

                for (int i = 0; i < count; i++)
                {
                    byte strLen = spanBody.Slice(readBytes, 1).ToArray()[0];
                    readBytes++;

                    issuedAssetInfos[i] = Encoding.ASCII.GetString(spanBody.Slice(readBytes, strLen).ToArray());
                    readBytes += strLen;
                }

                byte strLen2 = spanBody.Slice(readBytes, 1).ToArray()[0];
                readBytes++;

                string issuanceInfo = Encoding.ASCII.GetString(spanBody.Slice(readBytes, strLen2).ToArray());
                readBytes += strLen2;

                transactionalBlockBase = new IssueAssetsBlock
                {
                    IssuedAssetIds = issuedAssetIds,
                    IssuedAssetInfo = issuedAssetInfos,
                    IssuanceInfo = issuanceInfo
                };

                return spanBody.Slice(readBytes);
            }

            throw new BlockVersionNotSupportedException(version, BlockType);
        }
    }
}
