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

namespace Wist.Client.Wpf.ViewModels
{
    public class VoteViewModel : ViewModelBase
    {
        #region ============================================ MEMBERS ==================================================

        private readonly IDataAccessService _dataAccessService;
        private readonly IBlockParsersRepositoriesRepository _blockParsersRepositoriesRepository;

        #endregion

        #region ========================================== CONSTRUCTORS ===============================================

        public VoteViewModel(IDataAccessService dataAccessService, IBlockParsersRepositoriesRepository blockParsersRepositoriesRepository)
        {
            GetPollData();

            _dataAccessService = dataAccessService;
            _blockParsersRepositoriesRepository = blockParsersRepositoriesRepository;
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
            Dictionary<byte[], string> dictionary = new Dictionary<byte[], string>();

            List<byte[]> assetIds = null; 
            List<string> values = null;
            string voteSetTitle = string.Empty;

            List<TransactionalIncomingBlock> transactionalIncomingBlocksTags = _dataAccessService.GetIncomingBlocksByBlockType(BlockTypes.Transaction_IssueAssets).Where(t=> t.TagId != 1).ToList();
            transactionalIncomingBlocksTags.ForEach(t => 
            {
                var blockParserRep = _blockParsersRepositoriesRepository.GetBlockParsersRepository(PacketType.Transactional);
                var parser = blockParserRep.GetInstance(t.BlockType);
                var transferAsset = (IssueAssetsBlock)parser.Parse(t.Content);
                voteSetTitle = transferAsset.IssuanceInfo;
                values = transferAsset.IssuedAssetInfo.ToList();
            });

            List<TransactionalIncomingBlock> transactionalIncomingBlocks = _dataAccessService.GetIncomingBlocksByBlockType(BlockTypes.Transaction_TransferAssetsToUtxo);
            transactionalIncomingBlocks.ForEach(t =>
            {
                var blockParserRep = _blockParsersRepositoriesRepository.GetBlockParsersRepository(PacketType.Transactional);
                var parser = blockParserRep.GetInstance(t.BlockType);
                var transferAsset = (TransferAssetToUtxoBlock)parser.Parse(t.Content);

                assetIds = transferAsset.AssetIds.ToList();
            });

            FillData(voteSetTitle, assetIds, values);
        }

        private void FillData(string title, List<byte[]> assetIds, List<string> values)
        {
            string pollTitle = title.Split('|')[0];
            string voteSetRequest = title.Split('|')[1];

            Poll = new Poll
            {
                Title = pollTitle,
                VoteSets = new List<IVoteSet>()
            };

            int index = 0;

            assetIds.ForEach(t =>
            {
                Poll.VoteSets.Add(
                    new VoteSet
                    {
                        Request = voteSetRequest,
                        VoteItems = new List<IVoteItem>
                        {
                            new VoteItem
                            {
                                Id = t,
                                IsSelected = false,
                                Label = values[index++]    
                            }
                        }
                    });
            });

            //Poll = new Poll
            //{
            //    Title = "Important Survey",
            //    VoteSets = new List<IVoteSet>
            //    {
            //        new VoteSet()
            //        {
            //            Request = "What is your favorite car?",
            //            VoteItems = new List<IVoteItem>
            //                    {
            //                        new VoteItem
            //                        {
            //                            Id = new byte[]{ 1 },
            //                            IsSelected = false,
            //                            Label = "Mazda",
            //                        },
            //                        new VoteItem
            //                        {
            //                            Id = new byte[]{ 1 },
            //                            IsSelected = false,
            //                            Label = "Ferrari",
            //                        }
            //                    }
            //        },
            //        new VoteSet()
            //        {
            //            Request = "What is your pet?",
            //            VoteItems = new List<IVoteItem>
            //                    {
            //                        new VoteItem
            //                        {
            //                            Id = new byte[]{ 1 },
            //                            IsSelected = false,
            //                            Label = "Dog",
            //                        },
            //                        new VoteItem
            //                        {
            //                            Id = new byte[]{ 1 },
            //                            IsSelected = false,
            //                            Label = "Cat",
            //                        }
            //                    }
            //        }

            //    }
            //};
        }

        #endregion

    }
}