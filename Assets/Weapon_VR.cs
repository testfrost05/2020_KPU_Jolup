using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
namespace Com.Kpu.SimpleHostile
{
    public class Weapon_VR : MonoBehaviourPunCallbacks
    {
        #region Variables


        #endregion

        #region MonoBehaviour Callbacks

    
        #endregion

        #region Private Methods

       

 

        
        [PunRPC]
        private void TakeDamage(int p_damage)
        {
            GetComponent<Player_VR>().TakeDamage(p_damage);

        }
        #endregion
    }
}