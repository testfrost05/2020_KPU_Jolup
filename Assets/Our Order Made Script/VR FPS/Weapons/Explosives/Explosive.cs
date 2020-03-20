using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    public class Explosive : MonoBehaviour //폭발물
    {
        [SerializeField] protected float radius; //반경
        [SerializeField] protected float force; //힘
        [SerializeField] protected float explosionForce; //폭발힘
        [SerializeField] protected float upwardForce;
        [SerializeField] protected AnimationCurve forceOverDistanceCurve;
        [SerializeField] protected Transform explosion;

        [SerializeField] protected bool explodeOnStart = true;
        [SerializeField] protected bool destroyOnExplode;
        [SerializeField] protected float delay;

        [SerializeField] protected bool detonateOtherExplosives;
        [SerializeField] protected bool detonatedByOtherExplosives;

        [SerializeField] protected AnimationCurve damageOverDistance;
        [SerializeField] protected float baseDamage;

        protected bool detonated;

        public bool DetonatedByOtherExplosives
        {
            get
            {
                return detonatedByOtherExplosives;
            }
        }

        public delegate void ExplodeEvent();
        public ExplodeEvent _Explode;

        void Start()
        {
            if (explodeOnStart)
            {
                StartCoroutine(ExplodeRoutine(0));
            }
        }

        public void Explode() //폭발
        {
            StartCoroutine(ExplodeRoutine(delay));
        }

        public void Explode(float delay) //딜레이 있이 폭발
        {
            StartCoroutine(ExplodeRoutine(delay));
        }

        public void ExplodeByOther(float delay)
        {
            if (!detonated)
            {
                if (_Explode != null)
                {
                    _Explode();
                }
            }
            StartCoroutine(ExplodeRoutine(delay));
        }

        IEnumerator ExplodeRoutine(float delay) //폭발 루틴
        {
            if (detonated)
            {
                yield break;
            }
            detonated = true;

            if (delay > 0)
            {
                yield return new WaitForSeconds(delay);
            }

            var cols = Physics.OverlapSphere(transform.position, radius);
            var rigidbodies = new List<Rigidbody>();

            foreach (var col in cols)
            {
                if (col.attachedRigidbody != null && !rigidbodies.Contains(col.attachedRigidbody))
                {
                    rigidbodies.Add(col.attachedRigidbody);
                }

                if (detonateOtherExplosives)
                    if (col.tag == "Explosive")
                    {
                        Explosive tempExplosive = col.GetComponent<Explosive>();

                        if (tempExplosive)
                            if (tempExplosive.DetonatedByOtherExplosives)
                                tempExplosive.ExplodeByOther(0.1f);
                    }
            }

            foreach (var rb in rigidbodies)
            {
                float distance = Vector3.Distance(transform.position, rb.transform.position);
                float forceCurve = forceOverDistanceCurve.Evaluate(distance / radius);
                rb.AddExplosionForce(forceCurve * explosionForce, transform.position, radius, upwardForce, ForceMode.Impulse);
                rb.AddForce(-(transform.position - rb.transform.position) * forceCurve * force, ForceMode.Impulse);
            }

            if (explosion)
            {
                Instantiate(explosion, transform.position, transform.rotation);
            }

            if (destroyOnExplode)
            {
                Destroy(gameObject, 0.05f);
            }

            yield return null;
        }
    }
}