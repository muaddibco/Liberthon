using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Wist.BlockLattice.Core.DataModel.Transactional;
using Wist.BlockLattice.Core.Enums;
using Wist.BlockLattice.Core.Parsers;
using Wist.Client.DataModel.Model;
using Wist.Client.DataModel.Services;
using Wist.Client.Wpf.Interfaces;
using Wist.Client.Wpf.Models;
using Wist.Client.Common.Services;
using Wist.Core.States;
using Wist.Core.ExtensionMethods;
using System.Windows;
using Wist.Client.Common.Interfaces;

namespace Wist.Client.Wpf.ViewModels
{
    public class VoteViewModel : ViewModelBase
    {
        #region ============================================ MEMBERS ==================================================

        private readonly IDataAccessService _dataAccessService;
        private readonly IBlockParsersRepositoriesRepository _blockParsersRepositoriesRepository;
        private readonly IWalletManager _walletManager;
        private IPoll _poll;

        private readonly IClientState _clientState;

        #endregion

        #region ========================================== CONSTRUCTORS ===============================================

        public VoteViewModel(IDataAccessService dataAccessService, IStatesRepository statesRepository, 
            IBlockParsersRepositoriesRepository blockParsersRepositoriesRepository, IWalletManager walletManager)
        {
            _dataAccessService = dataAccessService;
            _blockParsersRepositoriesRepository = blockParsersRepositoriesRepository;
            _walletManager = walletManager;
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


        public ICommand Refresh => new RelayCommand(() => 
        {
            GetPollData();
        });

        public IPoll Poll
        {
            get => _poll;
            set
            {
                _poll = value;
                RaisePropertyChanged(() => Poll);
            }
        }

        public string TargetAddress { get; set; }

        public ICommand SubmitResults =>
            new RelayCommand(() =>
            {
                foreach (IVoteSet voteSet in Poll.VoteSets)
                {
                    IVoteItem selected = voteSet.VoteItems.FirstOrDefault(i => i.IsSelected);

                    if (selected != null)
                    {
                        TransferAssetToUtxoBlock model = (TransferAssetToUtxoBlock)selected.Model;

                        Common.Entities.Account account = new Common.Entities.Account { PublicKey = TargetAddress.HexStringToByteArray() };
                        _walletManager.SendAssetTransition(selected.Id, model.TransactionPublicKey, model.AssetCommitment, model.DestinationKey, model.TagId, account);
                    }
                }
            });

        #endregion

        #region ======================================== PRIVATE FUNCTIONS ============================================

        private void GetPollData()
        {
            Dictionary<byte[], string> dictionary = new Dictionary<byte[], string>();

            string title = null;
            List<List<byte[]>> assetIds = new List<List<byte[]>>();
            List<List<string>> values = new List<List<string>>();
            List<string> voteSetTitles = new List<string>();
            ulong tagId = 0;

            List<TransactionalIncomingBlock> transactionalIncomingBlocksTags = _dataAccessService.GetIncomingBlocksByBlockType(BlockTypes.Transaction_IssueAssets).Where(t => t.TagId != 1).ToList();
            transactionalIncomingBlocksTags.ForEach(t =>
            {
                tagId = t.TagId;
                var blockParserRep = _blockParsersRepositoriesRepository.GetBlockParsersRepository(PacketType.Transactional);
                var parser = blockParserRep.GetInstance(t.BlockType);
                var transferAsset = (IssueAssetsBlock)parser.Parse(t.Content);
                voteSetTitles.Add(transferAsset.IssuanceInfo.Split('|')[1]);
                if(title == null)
                {
                    title = transferAsset.IssuanceInfo.Split('|')[0];
                }
                values.Add(transferAsset.IssuedAssetInfo.ToList());
                assetIds.Add(transferAsset.IssuedAssetIds.ToList());
            });

            FillData(title, voteSetTitles, assetIds, values, tagId);
        }

        private void FillData(string title, List<string> questions, List<List<byte[]>> assetIds, List<List<string>> values, ulong tagId)
        {
            string pollTitle = title;

            List<UtxoUnspentBlock> transactionalIncomingBlocks = _dataAccessService.GetUtxoUnspentBlocksByTagId(tagId);
            transactionalIncomingBlocks.ForEach(t =>
            {
                var blockParserRep = _blockParsersRepositoriesRepository.GetBlockParsersRepository(PacketType.Transactional);
                var parser = blockParserRep.GetInstance(t.BlockType);
                var transferAsset = (TransferAssetToUtxoBlock)parser.Parse(t.Content);
            });

            Poll poll = new Poll
            {
                Title = pollTitle,
                VoteSets = new List<IVoteSet>()
            };

            for (int i = 0; i < questions.Count; i++)
            {
                VoteSet voteSet = new VoteSet()
                {
                    Request = questions[i],
                    VoteItems = new List<IVoteItem>()
                };

                poll.VoteSets.Add(voteSet);

                for (int j = 0; j < assetIds[i].Count; j++)
                {
                    UtxoUnspentBlock utxoUnspentBlock = transactionalIncomingBlocks.FirstOrDefault(t => t.AssetId.Equals32(assetIds[i][j]));
                    var blockParserRep = _blockParsersRepositoriesRepository.GetBlockParsersRepository(PacketType.Transactional);
                    var parser = blockParserRep.GetInstance(utxoUnspentBlock.BlockType);
                    var transferAsset = (TransferAssetToUtxoBlock)parser.Parse(utxoUnspentBlock.Content);
                    voteSet.VoteItems.Add(new VoteItem { Id = assetIds[i][j], Label = values[i][j], Model = transferAsset });
                }
            }

            Poll = poll;
        }

        #endregion

    }
}
