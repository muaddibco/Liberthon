using CommonServiceLocator;

namespace Wist.Client.Wpf.ViewModels
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    public class ViewModelLocator
    {
        public ViewModelLocator()
        {

        }

        public LoginViewModel LoginViewModel => ServiceLocator.Current.GetInstance<LoginViewModel>();
        public PollViewModel PollViewModel => ServiceLocator.Current.GetInstance<PollViewModel>();
        public RegistrationViewModel RegistrationViewModel => ServiceLocator.Current.GetInstance<RegistrationViewModel>();
        public VoteViewModel VoteViewModel => ServiceLocator.Current.GetInstance<VoteViewModel>();


        public static void Cleanup()
        {
            // TODO Clear the ViewModels
        }
    }
}