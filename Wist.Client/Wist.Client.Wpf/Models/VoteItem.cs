using Wist.Client.Wpf.Interfaces;

namespace Wist.Client.Wpf.Models
{
    public class VoteItem : IVoteItem
    {
        public string Label { get; set; }
        public byte[] Id { get; set; }
        public bool IsSelected { get; set; }
        public object Model { get; set; }
    }
}