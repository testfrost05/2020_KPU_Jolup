using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    public class Bullet : Item //총알은 아이템
    {
        [SerializeField] protected bool spent;
        public bool Spent
        {
            get
            {
                return spent;
            }
        }

        [SerializeField] protected Mesh casingMesh; 
        [SerializeField] protected Mesh cartridgeMesh;

        protected MeshFilter meshFltr;

        [SerializeField] protected Projectile projectile; //총쏘는 스크립트

        [HideInInspector] public bool chambered;
        [HideInInspector] public bool held;

        public bool Held
        {
            get
            {
                return held;
            }
        }

    
        public Projectile Projectile
        {
            get
            {
                return projectile;
            }
        }

        //총알 잡는 것은 아이템에서 상속해서 오버라이드
        protected override void PrimaryDrop()
        {
            base.PrimaryDrop();
            held = SecondaryHand;
        }

        protected override void PrimaryGrasp()
        {
            base.PrimaryGrasp();
            held = true;
        }

        protected override void SecondaryDrop()
        {
            base.SecondaryDrop();
            held = PrimaryHand;
        }

        protected override void SecondaryGrasp()
        {
            base.SecondaryGrasp();
            held = true;
        }

        protected override void Awake()
        {
            meshFltr = GetComponent<MeshFilter>();

            if (meshFltr == null)
            {
                meshFltr = GetComponentInChildren<MeshFilter>();
            }

            if (!projectile)
            {
                projectile = GetComponentInChildren<Projectile>(true);
            }

            base.Awake();
        }



        public void Fire(Transform muzzle) //총을 쏘면 머즐 위치에 생성
        {
            spent = true;

            if (meshFltr && casingMesh)
            {
                meshFltr.mesh = casingMesh;
            }

            if (projectile)
            {
                projectile.transform.SetParent(null, true);
                projectile.transform.position = muzzle.position;
                projectile.transform.rotation = muzzle.rotation;
                projectile.gameObject.SetActive(true);
                projectile.Fire();
            }
        }

        public void Fire(Transform muzzle, float muzzleVelocity, float spread) //탄퍼짐 적용
        {
            spent = true;

            if (meshFltr && casingMesh) meshFltr.mesh = casingMesh;

            if (projectile)
            {
                projectile.transform.SetParent(null, true);
                projectile.transform.position = muzzle.position;
                projectile.transform.rotation = muzzle.rotation;
                projectile.gameObject.SetActive(true);
                projectile.Fire(muzzleVelocity, spread);
            }
        }
    }
}