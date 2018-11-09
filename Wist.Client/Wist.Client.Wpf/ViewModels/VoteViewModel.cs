using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.Collections.Generic;
using System.Windows.Input;
using Wist.Client.DataModel.Services;
using Wist.Client.Wpf.Interfaces;
using Wist.Client.Wpf.Models;

namespace Wist.Client.Wpf.ViewModels
{
    public class VoteViewModel : ViewModelBase
    {
        #region ============================================ MEMBERS ==================================================

        private readonly IDataAccessService _dataAccessService;

        #endregion

        #region ========================================== CONSTRUCTORS ===============================================

        public VoteViewModel(IDataAccessService dataAccessService)
        {
            GetPollData();

            _dataAccessService = dataAccessService;
        }

        #endregion

        #region ======================================== PUBLIC FUNCTIONS =============================================

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