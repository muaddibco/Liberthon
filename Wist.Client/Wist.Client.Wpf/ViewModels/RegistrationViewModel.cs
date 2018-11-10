using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wist.Client.Wpf.Interfaces;
using Wist.Client.DataModel.Services;
using Wist.Client.Wpf.Models;
using Wist.BlockLattice.Core.Enums;
using Wist.Client.DataModel.Model;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight;
using Wist.Client.Common.Interfaces;
using System.Collections.ObjectModel;
using Wist.Core.States;
using Wist.Client.Common.Services;
using Wist.Client.Common.Entities;
using Wist.Core.ExtensionMethods;

namespace Wist.Client.Wpf.ViewModels
{
    public class RegistrationViewModel : ViewModelBase
    {
        #region ============================================ MEMBERS ==================================================

        private readonly IDataAccessService _dataAccessService;
        private readonly IWalletManager _walletManager;
        private readonly IClientState _clientState;
        private User _user;

        private ObservableCollection<User> _registeredUsers;

        #endregion

        #region ========================================== CONSTRUCTORS ===============================================

        public RegistrationViewModel(IDataAccessService dataAccessService, IWalletManager walletManager, IStatesRepository statesRepository)
        {
            User = new User();
            _clientState = statesRepository.GetInstance<IClientState>();
            RegisteredUsers = new ObservableCollection<User>();

            _dataAccessService = dataAccessService;
            _walletManager = walletManager;
            InitData();

        }

        #endregion

        #region ======================================== PUBLIC FUNCTIONS =============================================

        public byte[] PublicKey => _clientState.GetPublicKeyHash();

        public User User
        {
            get
            {
                return _user;
            }
            set
            {
                _user = value;
                RaisePropertyChanged(() => User);
            }
        }

        public ICommand SubmitUser
        {
            get => new RelayCommand(() =>
            {
                RegisteredUsers.Add(new User
                {
                    Address = User.Address,
                    FirstName = User.FirstName,
                    LastName = User.LastName,
                    PublicViewKey = User.PublicViewKey,
                    PublicSpendKey = User.PublicSpendKey,
                    Id = User.Id
                });

                User = new User();
            });
        }

        public ICommand SubmitIdCards => new RelayCommand(() =>
                                                   {
                                                       _walletManager.IssueAssets(
                                                           "Creation of ID cards",
                                                           RegisteredUsers.Select(r => GetAssetIdFromUser(r)).ToArray(),
                                                           RegisteredUsers.Select(
                                                               r => string.Join("|", string.Join(" ", r.FirstName, r.LastName), r.PublicViewKey, r.PublicSpendKey)).ToArray(), 1);
                                                   });

        private static byte[] GetAssetIdFromUser(User r)
        {
            byte[] assetId = new byte[32];
            Array.Copy(BitConverter.GetBytes(r.Id), 0, assetId, 0, sizeof(uint));

            return assetId;
        }

        public ICommand Clear => new RelayCommand(() => 
        {
            RegisteredUsers.Clear();
        });

        public ICommand DistributeIdCards => new RelayCommand(() => 
        {
            byte[][] idcards = RegisteredUsers.Select(u => GetAssetIdFromUser(u)).ToArray();
            
            foreach (User user in RegisteredUsers)
            {
                byte[] assetId = GetAssetIdFromUser(user);
                ConfidentialAccount confidentialAccount = new ConfidentialAccount { PublicViewKey =  user.PublicViewKey.HexStringToByteArray(), PublicSpendKey = user.PublicSpendKey.HexStringToByteArray() };
                int i = Array.FindIndex(idcards, b => b.Equals32(assetId));

                _walletManager.SendAssetToUtxo(idcards, i, 1, confidentialAccount);
            }
        });

        public ObservableCollection<User> RegisteredUsers { get => _registeredUsers; set => _registeredUsers = value; }

        private void InitData()
        {
            List<TransactionalIncomingBlock> transactionalIncomingBlocks = _dataAccessService.GetIncomingBlocksByBlockType(BlockTypes.Transaction_IssueAssets);

            transactionalIncomingBlocks.ForEach(t =>
            {
                if (t.BlockType == BlockTypes.Transaction_IssueAssets)
                {

                }
            });

        }

        #endregion

        #region ======================================== PRIVATE FUNCTIONS ============================================

        #endregion

    }
}