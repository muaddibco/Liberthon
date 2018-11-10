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

        public bool IsVoteSelected { get; set; }
        public bool IsRegisterUserSelected { get; set; }
        public bool IsCreatePollSelected { get; set; }

        public ICommand VoteCommand
        {
            get => new RelayCommand<IClosable>(c => 
            {
                _clientState.InitializeConfidential(ConfidentialAssetsHelper.GetRandomSeed(), ConfidentialAssetsHelper.GetRandomSeed());
                IsVoteSelected = true;
                c?.Close();
            });
        }
        public ICommand CreatePollCommand
        {
            get => new RelayCommand<IClosable>(c => 
            {
                byte[] seed = ConfidentialAssetsHelper.GetRandomSeed();
                _cryptoService.Initialize(seed);
                _clientState.InitializeAccountBased(seed);
                IsCreatePollSelected = true;
                c?.Close();
            });
        }

        public ICommand RegisterUserCommand =>
            new RelayCommand<IClosable>(c => 
            {
                byte[] seed = ConfidentialAssetsHelper.GetRandomSeed();
                _cryptoService.Initialize(seed);
                _clientState.InitializeAccountBased(seed);
                IsRegisterUserSelected = true;
                c?.Close();
            });


        #endregion

        #region ======================================== PRIVATE FUNCTIONS ============================================

        #endregion

    }
}