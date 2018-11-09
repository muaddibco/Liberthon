using System.Collections.Generic;

namespace Wist.Client.Wpf.Interfaces
{
    public interface IPoll
    {
        string Title { get; set; }
        ulong TagId { get; }
        ICollection<IVoteSet> VoteSets { get; set; }
    }
}