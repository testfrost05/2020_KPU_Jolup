using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    public class MagazineEjectLatch : MonoBehaviour //탄창 배출
    {
        public string magTag; //탄창 태그
        [SerializeField] protected Firearm firearm; 
        [SerializeField] protected SocketSlide magWell;
        protected Item mag;
        public float velocityThreshold;

        void Start()
        {
            firearm = GetComponentInParent<Firearm>();
            magWell = transform.parent.GetComponentInChildren<SocketSlide>();
        }

        void FixedUpdate()
        {
            if ((!firearm.PrimaryHand && !firearm.SecondaryHand) || !mag)
            {
                return;
            }

            if (!mag.PrimaryHand)
            {
                return;
            }

            Vector3 firearmVelocity = firearm.Velocity;
            Vector3 magVelocity = mag.Velocity;

            if (Vector3.Dot(magVelocity.normalized, transform.forward) > 0.75f && Mathf.Abs(firearmVelocity.magnitude
                - magVelocity.magnitude) > velocityThreshold)
            {
                magWell.EjectSlider();
            }
        }

        void OnTriggerEnter(Collider other) //온트리거
        {
            if (other.gameObject.tag == "Interactable")
            {
                Item tempMag = other.GetComponentInParent<Item>();

                if (tempMag)
                {
                    if (tempMag.HasTag(magTag))
                    {
                        if (tempMag.PrimaryHand)
                        {
                            mag = tempMag;
                        }
                    }
                }
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (mag)
                if (other.transform == mag.transform)
                    mag = null;
        }
    }
}