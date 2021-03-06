﻿using Wist.BlockLattice.Core.DataModel;
using Wist.Core.Identity;

namespace Wist.Client.Common.Entities
{
    /// <classDetails>   
    ///*****************************************************************
    ///  Machine Name : AMI-PC
    /// 
    ///  Author       : Ami
    ///       
    ///  Date         : 10/1/2018 1:16:55 AM      
    /// *****************************************************************/
    /// </classDetails>
    /// <summary>
    /// </summary>
    public class Account : AccountBase
    {
        //============================================================================
        //                                 MEMBERS
        //============================================================================

        public byte[] PrivateKey { get; set; }

        public byte[] PublicKey { get; set; }

        //============================================================================
        //                                  C'TOR
        //============================================================================

        public Account()
        {

        }

        //============================================================================
        //                                FUNCTIONS
        //============================================================================

        #region ============ PUBLIC FUNCTIONS =============  


        #endregion

        #region ============ PRIVATE FUNCTIONS ============ 


        #endregion

    }
}
