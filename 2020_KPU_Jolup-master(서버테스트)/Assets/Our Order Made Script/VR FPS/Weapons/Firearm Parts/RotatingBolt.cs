using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    public class RotatingBolt : Hinge //저격총 볼트 액션 할때 장전손잡이 회전
    {
        public delegate void HingeEvent();
        public HingeEvent _RotateUp;
        public HingeEvent _RotateDown;

        public Slide slider;

        public float slideTolerance;
        public float hingeTolerance;

        float previousRotation;
        float previousSliderNormal;

        protected enum ActionType //액션타입
        {
            upAndBack, // 올리고 당기기
            backAndUp //당기고 올리기
        }

        [SerializeField] protected ActionType actionType;

        protected override void Update()
        {
            Hand tempHand = interactionVolume.Hand;

            if (!tempHand)
            {
                return;
            }

            if (actionType == ActionType.upAndBack)
            {
                UpAndBack(tempHand);
            }
            else
            {
                BackAndUp(tempHand);
            }

            previousRotation = transform.localEulerAngles.z;
            previousSliderNormal = slider.slidePosition;
        }

        void UpAndBack(Hand hand) //일정 이상 올라가지 않으면 당길수 없게
        {

            if (previousSliderNormal < 1 - slideTolerance && slider.slidePosition >= 1 - slideTolerance)
            {
                HingeHand = hand;
            }
            if (previousSliderNormal >= 1 - slideTolerance && slider.slidePosition < 1 - slideTolerance)
            {
                HingeHand = null;
            }

            PullHinge();

            if (previousRotation <= maxAngle - hingeTolerance && transform.localEulerAngles.z >= maxAngle - hingeTolerance)
            {
                slider.GrabSlide(hand.transform);
            }
            if (previousRotation >= maxAngle - hingeTolerance && transform.localEulerAngles.z < maxAngle - hingeTolerance)
            {
                slider.DetachSlide();
            }
        }

        void BackAndUp(Hand hand) //당기지 않으면 회전 시키지 못하게
        {
            if (previousSliderNormal > slideTolerance && slider.slidePosition <= slideTolerance)
            {
                HingeHand = hand;
            }
            if (previousSliderNormal < slideTolerance && slider.slidePosition >= slideTolerance)
            {
                HingeHand = null;
            }

            PullHinge();

            if (previousRotation > hingeTolerance && transform.localEulerAngles.z <= hingeTolerance)
            {
                if (_RotateDown != null)
                {
                    _RotateDown();
                }
            }

            if (previousRotation < hingeTolerance && transform.localEulerAngles.z >= hingeTolerance)
            {
                if (_RotateUp != null)
                {
                    _RotateUp();
                }
            }
        }

        protected override void GrabHinge()
        {
            Hand hand = interactionVolume.Hand;

            if (actionType == ActionType.upAndBack)
            {
                if (slider.slidePosition >= 1 - slideTolerance)
                {
                    HingeHand = hand;
                }
                if (transform.localEulerAngles.z >= maxAngle - hingeTolerance)
                {
                    slider.GrabSlide(hand.transform);
                }
            }
            else
            {
                if (slider.slidePosition <= slideTolerance)
                {
                    HingeHand = hand;
                }
                slider.GrabSlide(hand.transform);
            }
        }

        protected override void RemoveHingeHand()
        {
            HingeHand = null;
            slider.DetachSlide();
        }

    }
}