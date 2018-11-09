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

namespace Wist.Client.Wpf.ViewModels
{
    public class RegistrationViewModel : ViewModelBase
    {
        #region ============================================ MEMBERS ==================================================

        private readonly IDataAccessService _dataAccessService;
        private readonly IWalletManager _walletManager;
        private User _user;

        private ObservableCollection<User> _registeredUsers;

        #endregion

        #region ========================================== CONSTRUCTORS ===============================================

        public RegistrationViewModel(IDataAccessService dataAccessService, IWalletManager walletManager)
        {
            User = new User();

            RegisteredUsers = new ObservableCollection<User>();

            _dataAccessService = dataAccessService;
            _walletManager = walletManager;
            InitData();

        }

        #endregion

        #region ======================================== PUBLIC FUNCTIONS =============================================

        public byte[] MyProperty { get; set; }

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
                    Id = User.Id
                });

                User = new User();
            });
        }

        public ICommand SubmitIdCards
        {
            get => new RelayCommand(() => 
            {
                _walletManager.IssueAssets(
                    "Creation of ID cards",
                    RegisteredUsers.Select(
                        r =>
                        {
                            byte[] assetId = new byte[32];
                            Array.Copy(BitConverter.GetBytes(r.Id), 0, assetId, 0, sizeof(uint));

                            return assetId;
                        }).ToArray(),
                    RegisteredUsers.Select(
                        r => string.Join(" ", r.FirstName, r.LastName)).ToArray(), 1);
            });
        }
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