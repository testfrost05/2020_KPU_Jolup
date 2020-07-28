using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.Kpu.SimpleHostile
{
    [CreateAssetMenu(fileName = "New Gun", menuName = "Gun")]
    public class Gun : ScriptableObject
    {
        public string name;
        public int damage;
        public int ammo;
        public int clipsize;
        public float firerate;
        public float bloom;
        public float recoil;
        public float kickback;
        public float aimSpeed;
        public float reload;
        public GameObject prefab;

        private int clip; //current clip
        private int stash; //current ammo

        public void Initialize()
        {
            stash = ammo;
            clip = clipsize;
        }




        public bool FireBullet()
        {
            if (clip > 0)
            {
                clip -= 1;
                return true;
            }
            else return false;

        }
        public void Reload()
        {
            stash += clip;
            clip = Mathf.Min(clipsize, stash);
            stash -= clip; 

        }

        public int Getstash() { return stash; }
        public int Getclip() { return clip; }

       


    }
}
