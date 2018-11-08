using System.Collections.Generic;

namespace Wist.Client.Wpf.Interfaces
{
    public interface IVoteSet
    {
        string Request { get; set; }
        ICollection<IVoteItem> VoteItems { get; set; }
    }
}