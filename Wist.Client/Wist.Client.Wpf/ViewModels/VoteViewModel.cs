using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Wist.Client.Common.Services;
using Wist.Client.DataModel.Services;
using Wist.Client.Wpf.Interfaces;
using Wist.Client.Wpf.Models;
using Wist.Core.States;
using Wist.Core.ExtensionMethods;

namespace Wist.Client.Wpf.ViewModels
{
    public class VoteViewModel : ViewModelBase
    {
        #region ============================================ MEMBERS ==================================================

        private readonly IDataAccessService _dataAccessService;

        private readonly IClientState _clientState;

        #endregion

        #region ========================================== CONSTRUCTORS ===============================================

        public VoteViewModel(IDataAccessService dataAccessService, IStatesRepository statesRepository)
        {
            GetPollData();

            _dataAccessService = dataAccessService;
            _clientState = statesRepository.GetInstance<IClientState>();
        }

        #endregion

        #region ======================================== PUBLIC FUNCTIONS =============================================

        public byte[] PublicViewKey => _clientState.GetPublicViewKey();
        public byte[] PublicSpendKey => _clientState.GetPublicSpendKey();

        public ICommand CopyPublicViewKey => new RelayCommand(() => 
        {
            Clipboard.SetText(PublicViewKey.ToHexString());
        });

        public ICommand CopyPublicSpendKey => new RelayCommand(() =>
        {
            Clipboard.SetText(PublicSpendKey.ToHexString());
        });

        public IPoll Poll { get; set; }

        public ICommand SubmitResults =>
            new RelayCommand(() =>
            {
                // store results
            });

        #endregion

        #region ======================================== PRIVATE FUNCTIONS ============================================

        private void GetPollData()
        {
            Poll = new Poll
            {
                Title = "Important Survey",
                VoteSets = new List<IVoteSet>
                {
                    new VoteSet()
                    {
                        Request = "What is your favorite car?",
                        VoteItems = new List<IVoteItem>
                                {
                                    new VoteItem
                                    {
                                        Id = new byte[]{ 1 },
                                        IsSelected = false,
                                        Label = "Mazda",
                                    },
                                    new VoteItem
                                    {
                                        Id = new byte[]{ 1 },
                                        IsSelected = false,
                                        Label = "Ferrari",
                                    }
                                }
                    },
                    new VoteSet()
                    {
                        Request = "What is your pet?",
                        VoteItems = new List<IVoteItem>
                                {
                                    new VoteItem
                                    {
                                        Id = new byte[]{ 1 },
                                        IsSelected = false,
                                        Label = "Dog",
                                    },
                                    new VoteItem
                                    {
                                        Id = new byte[]{ 1 },
                                        IsSelected = false,
                                        Label = "Cat",
                                    }
                                }
                    }

                }
            };
        }

        #endregion

    }
}