using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.Windows.Input;
using Wist.Client.Common.Services;
using Wist.Client.Wpf.Interfaces;
using Wist.Client.Wpf.Views;
using Wist.Core.Cryptography;
using Wist.Core.States;
using Wist.Crypto.ConfidentialAssets;

namespace Wist.Client.Wpf.ViewModels
{
    public class LoginViewModel : ViewModelBase,  ILoginViewModel 
    {
        #region ============================================ MEMBERS ==================================================

        private readonly IClientState _clientState;
        private readonly ICryptoService _cryptoService;

        #endregion

        #region ========================================== CONSTRUCTORS ===============================================

        public LoginViewModel(IStatesRepository statesRepository, ICryptoService cryptoService)
        {
            _clientState = statesRepository.GetInstance<IClientState>();
            _cryptoService = cryptoService;
        }

        #endregion

        #region ======================================== PUBLIC FUNCTIONS =============================================

        public string privateKey { get; set; }
        public string publicKey { get; set; }

        public ICommand VoteCommand
        {
            get => new RelayCommand(() => 
            {
                _clientState.InitializeConfidential(ConfidentialAssetsHelper.GetRandomSeed(), ConfidentialAssetsHelper.GetRandomSeed());
                new VoteWindow().ShowDialog();
            });
        }
        public ICommand CreatePollCommand
        {
            get => new RelayCommand(() => 
            {
                byte[] seed = ConfidentialAssetsHelper.GetRandomSeed();
                _cryptoService.Initialize(seed);
                _clientState.InitializeAccountBased(seed);
                new PollWindow().ShowDialog();
            });
        }

        public ICommand RegisterUserCommand =>
            new RelayCommand(() => 
            {
                byte[] seed = ConfidentialAssetsHelper.GetRandomSeed();
                _cryptoService.Initialize(seed);
                _clientState.InitializeAccountBased(seed);
                new RegistrationWindow().Show();
            });


        #endregion

        #region ======================================== PRIVATE FUNCTIONS ============================================

        #endregion

    }
}