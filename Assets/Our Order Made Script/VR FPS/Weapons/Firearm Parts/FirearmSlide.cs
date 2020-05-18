using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    public class FirearmSlide : Slide
    {
        [SerializeField] protected bool animateForwardOnRelease;

        [SerializeField] protected float slideForwardSpeed; //장전할때 앞 미는 속도
        [SerializeField] protected float slideBackSpeed; // 장전할때 뒤 당기는 속도

        [SerializeField] protected float dropSlideForwardSpeed; //뒤로 당기고 놓았을때 앞으로 가는 속도

        protected bool slideStop; 

        public bool SlideStop //슬라이드 멈추게 하는 함수
        {
            set
            {
                if (value && slidePosition > slideStopPosition)
                {
                    return;
                }
                if (slideStopObject)
                {
                    slideStopObject.localEulerAngles = slideStopRotations[value ? 0 : 1];
                }
                maxSliderDistance = value ? slideStopPosition : 1;

                slideStop = value;

                if (!slideStop)
                {
                    if (restingOnSlideStop && !interactionPoint)
                    {
                        if (dropSlideForwardSpeed > 0)
                        {
                            StartCoroutine("AnimateSlideForward", dropSlideForwardSpeed);
                        }
                    }
                }
            }
        }

        [SerializeField] [Range(-1, 1)] protected float slideStopPosition; //슬라이드 멈추는  위치
        [SerializeField] [Range(0, 1)] protected float catchBulletPosition; //장전이 되는 위치 - 어느정도는 당겨야 장전이 되게

        public delegate void SlideStopEvent();
        public SlideStopEvent _PulledPassedSlideStop;
        public SlideStopEvent _CatchBullet;
        public SlideStopEvent _RestingOnSlideStop;

        [ReadOnly] [SerializeField] protected bool restingOnSlideStop;
        public bool RestingOnSlideStop
        {
            get
            {
                return restingOnSlideStop;
            }
        }

        [SerializeField] protected Transform slideStopObject;
        [SerializeField] protected Vector3[] slideStopRotations = new Vector3[2];

        [SerializeField] protected OVRInput.Button slideStopInput; //오큘러스 컨트롤러 버튼 입력 받는거
        public OVRInput.Button SlideStopInput { get { return slideStopInput; } }

        [SerializeField] protected Item.TouchPadDirection slideStopTouchpadDirection;
        public Item.TouchPadDirection SlideStopTouchpadDirection { get { return slideStopTouchpadDirection; } }

        [SerializeField] protected SlideStopFunction slideStopFunction;
        protected enum SlideStopFunction //슬라이드 멈추는거 기능
        {
            none,
            off,
            on,
            toggle
        }

        protected override void Start()
        {
            base.Start();
            _OnReachedEnd += AnimSlideFor;
        }

        void AnimSlideFor()
        {
            if (slideForwardSpeed > 0)
            {
                if (!InteractionPoint)
                {
                    StopSlideAnimations();
                    StartCoroutine("AnimateSlideForward", slideForwardSpeed);
                }
            }
        }

        public override void GrabSlide()
        {
            StopSlideAnimations();
            base.GrabSlide();
        }

        protected override void Update()
        {
            if (prevNormal != slidePosition)
            {
                if (prevNormal <= catchBulletPosition && slidePosition > catchBulletPosition)
                {
                    if (_CatchBullet != null)
                    {
                        _CatchBullet();
                    }
                }
                //장전손잡이 위치가 장전되는 위치 이상이면 장전 
                if (slideStopPosition > 0)
                {
                    if (prevNormal >= slideStopPosition && slidePosition < slideStopPosition)
                    {
                        if (_PulledPassedSlideStop != null)
                        {
                            _PulledPassedSlideStop();
                        }
                    }
                    //장전 손잡이가 최대치가 되면 더이상 못가게
                    if (prevNormal < slideStopPosition && slidePosition >= slideStopPosition && slideStop && !interactionPoint) //!interactionVolume.Hand
                    {
                        restingOnSlideStop = true;
                        SetSlidePosition(slideStopPosition);
                        StopSlideAnimations();

                        if (_RestingOnSlideStop != null)
                        {
                            _RestingOnSlideStop();
                        }
                    }
                }
            }

            base.Update();
        }

        public void LockSlideStop()
        {
            SlideStop = true;
        }

        public void UnlockSlideStop()
        {
            SlideStop = false;
        }

        public void ToggleSlideStop()
        {
            SlideStop = !slideStop;
        }

        public void SlideStopTouchpadInput() //슬라이드 스탑을 켜고 끼고함
        {
            switch (slideStopFunction)
            {
                case SlideStopFunction.none:
                    break;
                case SlideStopFunction.off:
                    UnlockSlideStop();
                    break;
                case SlideStopFunction.on:
                    LockSlideStop();
                    break;
                case SlideStopFunction.toggle:
                    ToggleSlideStop();
                    break;
                default:
                    break;
            }
        }

        public virtual void AnimateSlide()
        {
            if (slideForwardSpeed == 0 || slideBackSpeed == 0)
            {
                return;
            }
            if (interactionVolume)
            {
                if (interactionVolume.Hand)
                {
                    return;
                }
            }

            StopSlideAnimations();
            StartCoroutine("AnimateSlideBack", slideBackSpeed);
        }

        public void StopSlideAnimations()
        {
            StopCoroutine("AnimateSlide");
            StopCoroutine("AnimateSlideBack");
            StopCoroutine("AnimateSlideForward");
            if (slideBack != null)
            {
                StopCoroutine(slideBack);
            }
            if (slideForward != null)
            {
                StopCoroutine(slideForward);
            }
        }

        public override void DetachSlide()
        {
            base.DetachSlide();

            StopSlideAnimations();

            if (!(slidePosition >= slideStopPosition && slideStop) && animateForwardOnRelease)
            {
                StartCoroutine("AnimateSlideForward", dropSlideForwardSpeed);
            }
        }
    }
}