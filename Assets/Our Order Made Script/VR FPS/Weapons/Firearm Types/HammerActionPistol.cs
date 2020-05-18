using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    public class HammerActionPistol : Firearm  //해머 있는 권총용 firearm
    {
        protected Hammer hammer;
        [SerializeField] protected bool doubleAction;

        protected override void Start()
        {
            base.Start();

            if (!hammer)
            {
                hammer = GetComponentInChildren<Hammer>();
            }

            hammer._Fire += base.Fire;
        }

        protected override void Fire()
        {
            if (hammer && doubleAction)
            {
                if (!hammer.Cocked && !hammer.Firing)
                {
                    hammer.LockHammer();
                }
            }

            float tempSlidePosition = slide ? slide.slidePosition : -1;
            bool slideForward = tempSlidePosition >= 1 || tempSlidePosition == -1;

            if (slideForward)
            {
                hammer.Fire(LocalInputDown(null, PrimaryHand.TouchpadInput), PrimaryHand.TouchpadAxis);
            }
        }

        protected override void Update()
        {
            base.Update();

            Vector2 touchpadAxis = PrimaryHand ? PrimaryHand.TouchpadAxis : Vector2.zero;
            float triggerRotation = PrimaryHand ? PrimaryHand.TriggerRotation : 0;
            float tempSlidePosition = slide ? slide.slidePosition : 0;

            if (doubleAction)
            {
                hammer.triggerAxis = fireSelector.FireMode == FireSelector._FireMode.safety ? 0 
                    : trigger.TriggerAxis(triggerRotation);
            }
            hammer.PullHammer(touchpadAxis, tempSlidePosition);
        }
    }
}