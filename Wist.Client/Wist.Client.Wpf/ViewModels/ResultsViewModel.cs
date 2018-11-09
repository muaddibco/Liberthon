using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wist.BlockLattice.Core.Enums;
using Wist.Client.DataModel.Model;
using Wist.Client.DataModel.Services;
using Wist.Core;
using Wist.Core.ExtensionMethods;

namespace Wist.Client.Wpf.ViewModels
{
    public class ResultsViewModel : ViewModelBase
    {
        private readonly IDataAccessService _dataAccessService;
        

        public ResultsViewModel(IDataAccessService dataAccessService)
        {
            _dataAccessService = dataAccessService;

            Utxos = new ObservableCollection<UtxoIncomingBlockDesc>();

            PeriodicTaskFactory.Start(() => 
            {
                List<UtxoIncomingBlock> utxos = _dataAccessService.GetIncomingUtxoBlocksByType(BlockTypes.UtxoConfidential_NonQuantitativeTransitionAssetTransfer);
                List< UtxoIncomingBlock> toAdd = utxos.Where(u => Utxos.Any(u1 => u1.Block.KeyImage.KeyImage.Equals32(u.KeyImage.KeyImage))).ToList();

                foreach (UtxoIncomingBlock item in toAdd)
                {
                    Utxos.Add(new UtxoIncomingBlockDesc { Block = item, Accepted = false });
                }
            }, 5000);
        }

        public ObservableCollection<UtxoIncomingBlockDesc> Utxos { get; set; }

        public class UtxoIncomingBlockDesc : ViewModelBase
        {
            private bool _accepted;

            public UtxoIncomingBlock Block { get; set; }

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
