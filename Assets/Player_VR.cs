using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;


namespace Com.Kpu.SimpleHostile
{
    public class Player_VR : MonoBehaviourPunCallbacks
    {
        #region Variables
        
        public int max_health;
        
     
        

        private Transform ui_healthbar;
        private Rigidbody rig;

        private int current_health;

        private Manager manager;
        #endregion

        #region MonoBehaviour Callbacks
        private void Start()
        {
            manager = GameObject.Find("Manager").GetComponent<Manager>();
            current_health = max_health;

     
            if (!photonView.IsMine) gameObject.layer = 8;

            

            
            rig = GetComponent<Rigidbody>();

            if (photonView.IsMine)
            {
                ui_healthbar = GameObject.Find("HUD/Health/Bar").transform;
                RefreshHealthBar();
            }

        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.U)) TakeDamage(100);

            //UI Refreshes
            RefreshHealthBar();

        }

        private void FixedUpdate()
        {
           

         
        }

        #endregion

        #region private Methods
       
        

        void RefreshHealthBar()
        {
            float t_health_ratio = (float)current_health / (float)max_health;
            ui_healthbar.localScale = Vector3.Lerp(ui_healthbar.localScale, new Vector3(t_health_ratio, 1, 1), Time.deltaTime * 8f);

        }

        #endregion

        #region Public methods


        public void TakeDamage(int p_damage)
        {
            if (photonView.IsMine)
            {
                current_health -= p_damage;
                RefreshHealthBar();

                if (current_health <= 0)
                {
                    //manager.VrSpawn();
                    PhotonNetwork.Destroy(gameObject);
                }
            }
        }


        #endregion
    }
}


