using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Wist.BlockLattice.Core.DataModel.UtxoConfidential;
using Wist.BlockLattice.Core.Enums;
using Wist.BlockLattice.Core.Parsers;
using Wist.Client.Common.Interfaces;
using Wist.Client.Common.Services;
using Wist.Client.DataModel.Model;
using Wist.Client.DataModel.Services;
using Wist.Core;
using Wist.Core.ExtensionMethods;
using Wist.Core.States;
using Wist.Crypto.ConfidentialAssets;

namespace Wist.Client.Wpf.ViewModels
{
    public class ResultsViewModel : ViewModelBase
    {
        private readonly IDataAccessService _dataAccessService;
        private readonly IWalletManager _walletManager;
        private readonly IBlockParsersRepositoriesRepository _blockParsersRepositoriesRepository;
        private readonly IClientState _clientState;

        public ResultsViewModel(IDataAccessService dataAccessService, IWalletManager walletManager, 
            IBlockParsersRepositoriesRepository blockParsersRepositoriesRepository, IStatesRepository statesRepository)
        {
            _dataAccessService = dataAccessService;
            _walletManager = walletManager;
            _blockParsersRepositoriesRepository = blockParsersRepositoriesRepository;
            _clientState = statesRepository.GetInstance<IClientState>();
            Utxos = new ObservableCollection<UtxoIncomingBlockDesc>();
        }

        public ObservableCollection<UtxoIncomingBlockDesc> Utxos { get; set; }

        public ICommand Refresh => new RelayCommand(() => 
        {
            List<UtxoIncomingBlock> utxos = _dataAccessService.GetIncomingUtxoBlocksByType(BlockTypes.UtxoConfidential_NonQuantitativeTransitionAssetTransfer);
            foreach (UtxoIncomingBlock item in utxos)
            {
                IBlockParsersRepository blockParsersRepository = _blockParsersRepositoriesRepository.GetBlockParsersRepository(PacketType.UtxoConfidential);
                IBlockParser blockParser = blockParsersRepository.GetInstance(item.BlockType);
                NonQuantitativeTransitionAssetTransferBlock transferBlock = (NonQuantitativeTransitionAssetTransferBlock)blockParser.Parse(item.Content);
                ConfidentialAssetsHelper.GetAssetIdFromEcdhTupleCA(transferBlock.EcdhTuple, transferBlock.TransactionPublicKey, _clientState.GetSecretViewKey(), out byte[] blindingFactor, out byte[] assetId);
                Utxos.Add(new UtxoIncomingBlockDesc { Block = transferBlock, AssetId = assetId, BlindingFactor = blindingFactor, Accepted = false });
            }
        });

        public ICommand AcceptVote => new RelayCommand<UtxoIncomingBlockDesc>(d => 
        {
            d.Accepted = _walletManager.SendAcceptAsset(d.Block.TransactionPublicKey, d.Block.AssetCommitment, d.BlindingFactor, d.AssetId, d.Block.TagId);
        });

        public class UtxoIncomingBlockDesc : ViewModelBase
        {
            private bool _accepted;

            public NonQuantitativeTransitionAssetTransferBlock Block { get; set; }

            public byte[] AssetId { get; set; }

            public byte[] BlindingFactor { get; set; }

            public bool Accepted
            {
                get => _accepted;
                set
                {
                    _accepted = value;
                    RaisePropertyChanged(() => Accepted);
                }
            }
        }
    }
}
