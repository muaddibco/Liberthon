using System;
using System.Collections.Generic;
using System.Text;

namespace Wist.Client.Common.Entities
{
    public class SyncBlockDescriptor
    {
        public SyncBlockDescriptor(ulong height, byte[] hash)
        {
            Height = height;
            Hash = hash;
        }

        public ulong Height { get; }
        public byte[] Hash { get; set; }
    }
}
