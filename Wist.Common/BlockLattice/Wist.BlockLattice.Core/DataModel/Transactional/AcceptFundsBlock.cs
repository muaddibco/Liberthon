﻿using Wist.BlockLattice.Core.Enums;

namespace Wist.BlockLattice.Core.DataModel.Transactional
{
    public class AcceptFundsBlock : TransactionalBlockBase
    {
        public override ushort BlockType => BlockTypes.Transaction_AcceptFunds;

        public override ushort Version => 1;

        /// <summary>
        /// 32 byte of Original Hash value of Transactional Account that is source of transaction that Income Transaction relates to
        /// </summary>
        public byte[] SourceOriginalHash { get; set; }
    }
}