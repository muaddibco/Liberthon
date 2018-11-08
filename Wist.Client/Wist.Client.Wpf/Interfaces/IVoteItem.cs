namespace Wist.Client.Wpf.Interfaces
{
    public interface IVoteItem
    {
        string Label { get; set; }
        bool IsSelected { get; set; }
        byte[] Id { get; set; }
    }
}
