using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    public class ExplosiveItem : Item //폭발하는 아이템
    {
        [SerializeField] protected Explosive explosive;
        [SerializeField] protected bool armed;

        public bool Armed
        {
            get
            {
                return armed;
            }
        }
        [SerializeField] protected float armDelay;
        [SerializeField] protected float delay;
        [SerializeField] protected float volume = 100.0f;
        [SerializeField] protected AudioClip explosionSFX; //폭발소리

        protected override void Start()
        {
            base.Start();

            if (explosive)
            {
                explosive._Explode += Detach;
                explosive._Explode += DetachSlot;
            }
        }

        public virtual void Arm()
        {
            armed = !armed;
        }

        public virtual void DelayedArm()
        {
            StartCoroutine(DelayedArmRoutine(armDelay));
        }

        public virtual void Explode()
        {
            StartCoroutine(ExplodeRoutine());
        }

        protected virtual IEnumerator DelayedArmRoutine(float seconds)
        {
            if (armed)
            {
                armed = false;
                yield break;
            }

            yield return new WaitForSeconds(seconds);

            armed = true;
        }

        protected virtual IEnumerator ExplodeRoutine()
        {
            if (!armed)
            {
                yield break;
            }
            armed = false;

            yield return new WaitForSeconds(delay);

            Detach();
            DetachSlot();

            if (explosionSFX)
            {
                AudioSource.PlayClipAtPoint(explosionSFX, transform.position, volume);
            }
            yield return new WaitForEndOfFrame();

            if (explosive)
            {
                explosive.Explode(0);
            }
        }
    }
}