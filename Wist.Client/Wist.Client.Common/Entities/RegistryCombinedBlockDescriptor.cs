using System;
using System.Collections.Generic;
using System.Text;

namespace Wist.Client.Common.Entities
{
    public class RegistryCombinedBlockDescriptor
    {
        public RegistryCombinedBlockDescriptor(ulong height, byte[] content, byte[] hash)
        {
            Height = height;
            Content = content;
            Hash = hash;
        }

        public ulong Height { get; }
        public byte[] Content { get; }
        public byte[] Hash { get; set; }
    }
}
