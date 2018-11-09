using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wist.Core.ExtensionMethods;
using System.Windows.Input;
using Wist.Client.Wpf.Interfaces;
using Wist.Client.Wpf.Models;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight;
using Wist.Crypto.ConfidentialAssets;
using System.Collections.ObjectModel;
using Wist.Client.Common.Interfaces;
using Wist.Core.States;
using Wist.Client.Common.Services;
using System.Windows;

namespace Wist.Client.Wpf.ViewModels
{

    public class PollViewModel : ViewModelBase
    {
        private readonly IWalletManager _walletManager;
        private readonly IClientState _clientState;

        #region ============================================ MEMBERS ==================================================

        private IPoll _selectedPollBottom;
        private IVoteSet _selectedVoteSetBottom;

        #endregion

        #region ========================================== CONSTRUCTORS ===============================================

        public PollViewModel(IWalletManager walletManager, IStatesRepository statesRepository)
        {
            InitPoll();
            Polls = new ObservableCollection<IPoll>();
            _walletManager = walletManager;
            _clientState = statesRepository.GetInstance<IClientState>();
        }

        #endregion

        #region ======================================== PUBLIC FUNCTIONS =============================================

        public string PollName { get; set; }

        public ObservableCollection<IPoll> Polls { get; set; }
        public ObservableCollection<IVoteSet> SelectedVoteSetsBottom { get; set; }
        public IVoteSet SelectedVoteSetBottom
        {
            get => _selectedVoteSetBottom;
            set { _selectedVoteSetBottom = value;
                RaisePropertyChanged();
            }
        }

        public IPoll SelectedPollBottom
        {
            get => _selectedPollBottom;
            set
            {
                _selectedPollBottom = value;
                SelectedVoteSetsBottom = new ObservableCollection<IVoteSet>(_selectedPollBottom.VoteSets);
                RaisePropertyChanged(() => SelectedVoteSetsBottom);
            }
        }
        public IPoll Poll { get; set; }
        public string Request { get; set; }
        public IVoteSet SelectedVoteSet { get; set; }
        public IPoll SelectedPoll { get; set; }
        public ObservableCollection<IVoteItem> VoteItems => new ObservableCollection<IVoteItem>(SelectedVoteSet?.VoteItems ?? new List<IVoteItem>());
        public string VoteItemLabel { get; set; }
        public string Id { get; set; }

        public byte[] PublicKey => _clientState.GetPublicKeyHash();

        public ICommand CopyPublicKeyToClipboard => new RelayCommand(() => 
        {
            Clipboard.SetText(PublicKey.ToHexString());
        });

        public VoteItem VoteItem { get; set; }

        public ICommand AddVoteSet =>
            new RelayCommand(() =>
                {
                    SelectedPoll?.VoteSets?.Add(
                        new VoteSet
                        {
                            Request = Request,
                            VoteItems = new List<IVoteItem>()
                        });
                    SelectedVoteSet = SelectedPoll?.VoteSets.ToList()[SelectedPoll.VoteSets.Count - 1];
                    RaisePropertyChanged(() => SelectedVoteSet);
                });

        public ICommand AddVoteItem =>
            new RelayCommand(() =>
                    {
                        SelectedVoteSet?.VoteItems?.Add(
                            new VoteItem
                            {
                                Id = ConfidentialAssetsHelper.GetRandomSeed(),
                                IsSelected = false,
                                Label = VoteItemLabel
                            });
                        VoteItemLabel = string.Empty;

                        RaisePropertyChanged(() => VoteItemLabel);
                        RaisePropertyChanged(() => VoteItems);
                        RaisePropertyChanged(() => SelectedVoteSet);
                        RaisePropertyChanged(() => SelectedVoteSet.VoteItems);
                    });


        public ICommand SubmitPoll =>
            new RelayCommand(() =>
            {
                Polls.Add(
                    new Poll()
                    {
                        Title = SelectedPoll.Title,
                        VoteSets = SelectedPoll.VoteSets
                    });
                InitPoll();
                RaisePropertyChanged(() => Polls);
            });

        public ICommand UpdateBlockchain =>
            new RelayCommand(() => 
            {

            });

        #endregion

        #region ======================================== PRIVATE FUNCTIONS ============================================

        private void InitPoll()
        {
            SelectedPoll = new Poll();
            SelectedPoll.VoteSets = new List<IVoteSet>();
        }

        #endregion

    }
}