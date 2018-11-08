using System.Collections.Generic;
using Wist.Client.Wpf.Interfaces;

namespace Wist.Client.Wpf.Models
{
    public class Poll : IPoll
    {
        public string Title { get; set; }
        public ICollection<IVoteSet> VoteSets { get; set; }
    }
}