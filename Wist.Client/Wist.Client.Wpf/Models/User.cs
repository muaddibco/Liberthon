namespace Wist.Client.Wpf.Models
{
    public class User
    {
        public uint Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Address { get; set; }
        public string PublicViewKey { get; set; }
        public string PublicSpendKey { get; set; }
    }
}