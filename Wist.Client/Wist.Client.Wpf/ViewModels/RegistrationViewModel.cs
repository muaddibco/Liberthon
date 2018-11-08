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

namespace Wist.Client.Wpf.ViewModels
{
    public class RegistrationViewModel : ViewModelBase
    {
        #region ============================================ MEMBERS ==================================================

        private readonly IDataAccessService _dataAccessService;

        private ICollection<User> _registeredUsers;

        #endregion

        #region ========================================== CONSTRUCTORS ===============================================

        public RegistrationViewModel(IDataAccessService dataAccessService)
        {
            User = new User();

            _registeredUsers = new List<User>();

            _dataAccessService = dataAccessService;

            InitData();

        }

        #endregion

        #region ======================================== PUBLIC FUNCTIONS =============================================

        public User User { get; set; }

        public ICommand SubmitUser
        {
            get => new RelayCommand(() =>
            {
                _registeredUsers.Add(new User
                {
                    Address = User.Address,
                    FirstName = User.FirstName,
                    LastName = User.LastName,
                    Id = User.Id
                });
                User = new User();
            });
        }

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