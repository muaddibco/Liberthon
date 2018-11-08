using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.Windows.Input;
using Wist.Client.Common.Services;
using Wist.Client.Wpf.Interfaces;
using Wist.Client.Wpf.Views;
using Wist.Core.States;
using Wist.Crypto.ConfidentialAssets;

namespace Wist.Client.Wpf.ViewModels
{
    public class LoginViewModel : ViewModelBase,  ILoginViewModel 
    {
        #region ============================================ MEMBERS ==================================================

        private readonly IClientState _clientState;

        #endregion

        #region ========================================== CONSTRUCTORS ===============================================
        
        public LoginViewModel(IStatesRepository statesRepository)
        {
            _clientState = statesRepository.GetInstance<IClientState>();
        }

        #endregion

        #region ======================================== PUBLIC FUNCTIONS =============================================

        public string privateKey { get; set; }
        public string publicKey { get; set; }

        public ICommand UtxoLoginCommand
        {
            get => new RelayCommand(() => 
            {
                _clientState.InitializeConfidential(ConfidentialAssetsHelper.GetRandomSeed(), ConfidentialAssetsHelper.GetRandomSeed());
                new PollWindow().ShowDialog();
            });
        }
        public ICommand AccountBasedLoginCommand
        {
            get => new RelayCommand(() => 
            {
                _clientState.InitializeAccountBased(ConfidentialAssetsHelper.GetRandomSeed());
                new PollWindow().ShowDialog();
            });
        }

        public ICommand RegisterUserCommand =>
            new RelayCommand(() => 
            {
                new RegistrationWindow().Show();
            });


        #endregion

        #region ======================================== PRIVATE FUNCTIONS ============================================

        #endregion

    }
}