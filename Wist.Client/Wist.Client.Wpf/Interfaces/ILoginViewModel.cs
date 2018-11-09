using System.Windows.Input;

namespace Wist.Client.Wpf.Interfaces
{
    public interface ILoginViewModel
    {
        string privateKey { get; set; }
        string publicKey { get; set; }

        ICommand VoteCommand
        {
            get;
        }
        ICommand CreatePollCommand
        {
            get;
        }

        ICommand RegisterUserCommand { get; }
    }
}
