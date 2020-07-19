using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.Kpu.SimpleHostile
{
    public class Weapon : MonoBehaviour
    {
        #region Variables

        public Gun[] loadout;
        public Transform weaponParent;

        private int currentIndex;
        private GameObject currentWeapon;

        #endregion

        #region MonoBehaviour Callbacks
    
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) Equip(0);

            if (currentWeapon != null)
            {
                Aim(Input.GetMouseButton(1));
            } 

        }
        #endregion

        #region Private Methods
        void Equip(int p_ind)
        {
            if (currentWeapon != null) Destroy(currentWeapon);

            currentIndex = p_ind;

            GameObject t_newWeapon = Instantiate(loadout[p_ind].prefab, weaponParent.position, weaponParent.rotation, weaponParent) as GameObject;
            t_newWeapon.transform.localPosition = Vector3.zero;
            t_newWeapon.transform.localEulerAngles = Vector3.zero;

            currentWeapon = t_newWeapon;
        }

        void Aim(bool p_isAiming) 
        {
            Transform t_anchor = currentWeapon.transform.Find("Anchor");
            Transform t_state_ads = currentWeapon.transform.Find("States/ADS");
            Transform t_state_hip = currentWeapon.transform.Find("States/Hip");


            if (p_isAiming)
            {
                //aim
                t_anchor.position = Vector3.Lerp(t_anchor.position, t_state_ads.position, Time.deltaTime * loadout[currentIndex].aimSpeed);
            }
            else 
            {
                //hip
                t_anchor.position = Vector3.Lerp(t_anchor.position, t_state_hip.position, Time.deltaTime * loadout[currentIndex].aimSpeed);

            }
        }
        #endregion
    }
}