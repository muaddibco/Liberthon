using System.Collections.Generic;
using Wist.Client.Wpf.Interfaces;

namespace Wist.Client.Wpf.Models
{
    
    public class VoteSet : IVoteSet
    {
        public string Request { get; set; }
        public ICollection<IVoteItem> VoteItems { get; set; }
    }
}