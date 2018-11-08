using Wist.BlockLattice.Core.DataModel;
using Wist.BlockLattice.Core.DataModel.Registry;
using Wist.BlockLattice.Core.DataModel.Transactional;
using Wist.Client.Common.Exceptions;
using Wist.Client.Common.Interfaces;
using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;

namespace Wist.Client.Common.Services
{
    [RegisterDefaultImplementation(typeof(IBlockCreator), Lifetime = LifetimeManagement.Singleton)]
    public class BlockCreator : IBlockCreator
    {
        public BlockCreator()
        {

        }

        #region ============ PUBLIC FUNCTIONS =============  

        /// <summary>
        /// TODO: remove this switch case and create smart system to instanciate objects 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public BlockBase GetInstance(ushort key)
        {
            switch (key)
            {
                case 1:
                    return new TransferFundsBlock();
                case 2:
                    return new RegistryRegisterBlock();
                default:
                    throw new UnknownTypeException();
            }
        }


        #endregion

        #region ============ PRIVATE FUNCTIONS ============ 


        #endregion

    }
}
