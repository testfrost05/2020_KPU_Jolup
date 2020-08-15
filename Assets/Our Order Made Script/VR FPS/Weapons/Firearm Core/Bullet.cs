using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    public class Bullet : Item
    {
        [SerializeField] protected bool spent;
        public bool Spent { get { return spent; } }

        [SerializeField] private MeshRenderer meshRenderer;

        [SerializeField] protected Mesh casingMesh;
        [SerializeField] protected Mesh cartridgeMesh;
        [SerializeField] protected GameObject bulletMesh;

        protected MeshFilter meshFltr;

        [SerializeField] protected Projectile projectile;

        [HideInInspector] public bool chambered;
        [HideInInspector] public bool held;

        public bool Held { get { return held; } }

        public Projectile Projectile { get { return projectile; } }

        public MeshRenderer MeshRenderer { get => meshRenderer; set => meshRenderer = value; }

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
            if (!meshFltr) meshFltr = GetComponent<MeshFilter>();
            if (!meshRenderer) meshRenderer = GetComponentInChildren<MeshRenderer>();

            if (meshFltr == null) meshFltr = GetComponentInChildren<MeshFilter>();

            if (!projectile) projectile = GetComponentInChildren<Projectile>(true);

            if (spent)
            {
                if (meshFltr && casingMesh) meshFltr.mesh = casingMesh;

                if (bulletMesh) bulletMesh.SetActive(false);
            }
            else
            {
                if (meshFltr && cartridgeMesh) meshFltr.mesh = cartridgeMesh;

                if (bulletMesh) bulletMesh.SetActive(true);
            }

            base.Awake();
        }

        public void Fire(Transform muzzle)
        {
            spent = true;

            if (meshFltr && casingMesh) meshFltr.mesh = casingMesh;

            if (bulletMesh) bulletMesh.SetActive(false);

            if (projectile)
            {
                projectile.transform.SetParent(null, true);
                projectile.transform.position = muzzle.position;
                projectile.transform.rotation = muzzle.rotation;
                projectile.gameObject.SetActive(true);
                projectile.Fire();
            }
        }

        public void Fire(Transform muzzle, float muzzleVelocity, float spread)
        {
            spent = true;

            if (meshFltr && casingMesh) meshFltr.mesh = casingMesh;

            if (bulletMesh) bulletMesh.SetActive(false);

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