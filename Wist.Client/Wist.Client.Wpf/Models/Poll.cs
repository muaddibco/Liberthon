using System;
using System.Collections.Generic;
using Wist.Client.Wpf.Interfaces;

namespace Wist.Client.Wpf.Models
{
    public class Poll : IPoll
    {
        public Poll()
        {
            Random random = new Random();
            TagId = (ulong)random.Next();
        }

        public string Title { get; set; }
        public ICollection<IVoteSet> VoteSets { get; set; }

        public ulong TagId { get; }
    }
}