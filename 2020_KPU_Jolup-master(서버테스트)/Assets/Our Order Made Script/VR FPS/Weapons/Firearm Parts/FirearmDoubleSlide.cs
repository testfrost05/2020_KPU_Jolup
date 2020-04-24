using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    public class FirearmDoubleSlide : FirearmSlide
    {
        [SerializeField] protected FirearmSlide firearmSlide;

        bool lower;

        void LateUpdate()
        {
            if (interactionPoint && !firearmSlide.InteractionPoint)
            {
                
                if (lower)
                {
                    firearmSlide.StopSlideAnimations();
                    firearmSlide.SetSlidePosition(slidePosition);
                    firearmSlide.GrabSlide(interactionPoint);
                    GrabSlide(interactionPoint);
                }
            }

            lower = slidePosition < firearmSlide.slidePosition;
        }

        public override void DetachSlide()
        {
            if (firearmSlide.InteractionPoint)
                firearmSlide.DetachSlide();

            base.DetachSlide();
        }
    }
}