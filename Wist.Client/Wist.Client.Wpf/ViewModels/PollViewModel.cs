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
using Wist.Client.DataModel.Services;
using Wist.BlockLattice.Core.Enums;
using Wist.Client.DataModel.Model;
using Wist.BlockLattice.Core.Parsers;
using Wist.BlockLattice.Core.DataModel.Transactional;
using Wist.Client.Common.Entities;

namespace Wist.Client.Wpf.ViewModels
{

    public class PollViewModel : ViewModelBase
    {
        private readonly IWalletManager _walletManager;
        private readonly IDataAccessService _dataAccessService;
        private readonly IBlockParsersRepositoriesRepository _blockParsersRepositoriesRepository;
        private readonly IClientState _clientState;

        #region ============================================ MEMBERS ==================================================

        private IPoll _selectedPollBottom;
        private IVoteSet _selectedVoteSetBottom;

        #endregion

        #region ========================================== CONSTRUCTORS ===============================================

        public PollViewModel(IWalletManager walletManager, IStatesRepository statesRepository, IDataAccessService dataAccessService, IBlockParsersRepositoriesRepository blockParsersRepositoriesRepository)
        {
            InitPoll();
            Polls = new ObservableCollection<IPoll>();
            _walletManager = walletManager;
            _dataAccessService = dataAccessService;
            _blockParsersRepositoriesRepository = blockParsersRepositoriesRepository;
            _clientState = statesRepository.GetInstance<IClientState>();
            ResultsViewModel = new ResultsViewModel(dataAccessService, walletManager, blockParsersRepositoriesRepository, statesRepository);
        }

        #endregion

        #region ======================================== PUBLIC FUNCTIONS =============================================

        public ResultsViewModel ResultsViewModel { get; set; }

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

        public ICommand IssuePoll =>
            new RelayCommand(() => 
            {
                Random random = new Random();

                foreach (IPoll poll in Polls)
                {
                    foreach (var voteSet in poll.VoteSets)
                    {
                        _walletManager.IssueAssets($"{poll.Title}|{voteSet.Request}",
                            voteSet.VoteItems.Select(v => v.Id).ToArray(),
                            voteSet.VoteItems.Select(v => v.Label).ToArray(), poll.TagId);
                    }
                }
            });

        public ICommand DistributePoll => new RelayCommand(() => 
        {
            foreach (IPoll poll in Polls)
            {

                List<TransactionalIncomingBlock> incomingBlocks = _dataAccessService.GetIncomingBlocksByBlockType(BlockTypes.Transaction_IssueAssets).Where(b => b.TagId == 1).ToList();
                byte[][][] assetIdsBySets = poll.VoteSets.Select(v => v.VoteItems.Select(i => i.Id).ToArray()).ToArray();

                foreach (var item in incomingBlocks)
                {
                    IBlockParsersRepository blockParsersRepository = _blockParsersRepositoriesRepository.GetBlockParsersRepository(PacketType.Transactional);
                    IBlockParser blockParser = blockParsersRepository.GetInstance(item.BlockType);
                    IssueAssetsBlock issueAssetsBlock = (IssueAssetsBlock)blockParser.Parse(item.Content);

                    foreach (string info in issueAssetsBlock.IssuedAssetInfo)
                    {
                        string[] parts = info.Split('|');
                        string publicVK = parts[1];
                        string publicSK = parts[2];

                        ConfidentialAccount confidentialAccount = new ConfidentialAccount { PublicViewKey = publicVK.HexStringToByteArray(), PublicSpendKey = publicSK.HexStringToByteArray() };

                        for (int i = 0; i < assetIdsBySets.Length; i++)
                        {
                            byte[] sk = ConfidentialAssetsHelper.GetRandomSeed();
                            for (int j = 0; j < assetIdsBySets[i].Length; j++)
                            {
                                _walletManager.SendAssetToUtxo(assetIdsBySets[i], j, SelectedPoll.TagId, confidentialAccount, sk);
                            }
                        }
                    }
                }
            }
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