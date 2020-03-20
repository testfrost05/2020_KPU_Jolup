using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    public class Hammer : MonoBehaviour // 해머 그 권총 엄지로 장전하는거
    {
        //해머 소리
        [SerializeField] protected List<AudioClip> lockSounds = new List<AudioClip>();
        [SerializeField] protected List<AudioClip> decockSounds = new List<AudioClip>();

        public float HammerRotation //해머 로테이션
        {
            get
            {
                return TwitchExtension.NormalizeInput(0, maxRotation, transform.localEulerAngles.x);
            }
        }

        public delegate void HammerEvent();

        public HammerEvent _Fire;
        public HammerEvent _Lock;

        [SerializeField] protected float restHammerSpeed;
        [SerializeField] protected float maxRotation;

        protected Vector2 initialTouchpadAxis;
        protected float initialHammerRot = -1;

        protected bool firingHammer;
        protected bool hammerCocked;

        public bool Firing //총쏨
        {
            get
            {
                return firingHammer;
            }
        }
        public bool Cocked //장전
        {
            get
            {
                return hammerCocked;
            }
        }

        protected Vector2 previousTouchpadAxis = Vector2.zero;
        protected float previousTriggerAxis = 0;

        public virtual void Fire(bool decock, Vector2 axis)
        {
            if (hammerCocked) 
            {
                Vector3 tempRot = transform.localEulerAngles;
                tempRot.x = maxRotation - 2;
                transform.localEulerAngles = tempRot;
                hammerCocked = false;

                if (decock)
                {
                    axis.y += 0.4f;
                    initialTouchpadAxis = axis;
                    initialHammerRot = 0;
                    TwitchExtension.PlayRandomAudioClip(decockSounds, transform.position);
                }

                else
                {
                    firingHammer = true;
                }

            }
        }

        protected virtual void PoseHammer(float delta) //해머가 포지션이랑 로테이션 조정해서 모양 변경
        {
            Vector3 tempHammerRot = transform.localEulerAngles;
            tempHammerRot.x = Mathf.Clamp(delta, 0, maxRotation);
            transform.localEulerAngles = tempHammerRot;

            if (hammerCocked)
            {
                return;
            }
            if (transform.localEulerAngles.x > maxRotation - 0.01f)
            {
                LockHammer();
            }
        }

        protected virtual void Rest()
        {
            PoseHammer(transform.localEulerAngles.x - (restHammerSpeed * Time.fixedDeltaTime));
            Vector3 tempHammerRot = transform.localEulerAngles;
            tempHammerRot.x = Mathf.Clamp(tempHammerRot.x - (restHammerSpeed * Time.fixedDeltaTime), 0, maxRotation);
            transform.localEulerAngles = tempHammerRot;
        }

        public virtual void LockHammer()
        {
            Vector3 tempRot = transform.localEulerAngles;
            tempRot.x = maxRotation;
            transform.localEulerAngles = tempRot;
            hammerCocked = true;
            TwitchExtension.PlayRandomAudioClip(lockSounds, transform.position);
        }

        [HideInInspector] public float touchPadDelta;

        [HideInInspector] public float triggerAxis;

        public virtual void PullHammer(Vector2 touchpadAxis, float slideAxis) //해머 당기기
        {
            float triggerAxis = !firingHammer ? this.triggerAxis : 0;

            if (touchpadAxis == Vector2.zero)
            {
                initialTouchpadAxis = Vector2.zero;
            }

            if (!hammerCocked && !firingHammer)
            {

                if (previousTouchpadAxis == Vector2.zero && touchpadAxis != Vector2.zero)
                {
                    initialTouchpadAxis = touchpadAxis;
                }
                if (previousTouchpadAxis != Vector2.zero && touchpadAxis != Vector2.zero)
                {
                    if (touchpadAxis.y > initialTouchpadAxis.y)
                    {
                        if (transform.localEulerAngles.x == 0)
                        {
                            initialTouchpadAxis = touchpadAxis;
                            initialHammerRot = transform.localEulerAngles.x;
                        }
                    }
                }
                if (initialHammerRot == -1
                    && ((previousTouchpadAxis == Vector2.zero && touchpadAxis != Vector2.zero)
                    || (previousTriggerAxis < 0.01f && triggerAxis > 0.01f)))
                {
                    initialHammerRot = transform.localEulerAngles.x;
                }

                if ((previousTouchpadAxis == Vector2.zero && touchpadAxis == Vector2.zero) &&
                    (previousTriggerAxis < 0.01f && triggerAxis < 0.01f))
                {
                    initialHammerRot = -1;
                }
            }
            if (!hammerCocked)
            {
                float yDelta = Mathf.Clamp(touchpadAxis.y - initialTouchpadAxis.y, -1, 1);

                if (!hammerCocked)
                {
                    touchPadDelta = yDelta * 2.5f;
                }
                yDelta -= triggerAxis;
                yDelta = -yDelta * 120;

                bool slidePullingHammer = (1 - slideAxis) * maxRotation > yDelta + (initialHammerRot != -1 ? initialHammerRot : 0);

                if (slidePullingHammer && !firingHammer)
                {
                    PoseHammer((1 - slideAxis) * maxRotation);
                }
                else if (((touchpadAxis != Vector2.zero && previousTouchpadAxis != Vector2.zero)
                    || (previousTriggerAxis > 0.01f && triggerAxis > 0.01f)) && !firingHammer)
                {
                    PoseHammer(yDelta + (initialHammerRot != -1 ? initialHammerRot : 0));
                }
                else
                {
                    Rest();

                    if (firingHammer && transform.localEulerAngles.x == 0)
                    {
                        firingHammer = false;

                        initialTouchpadAxis = touchpadAxis;
                        initialHammerRot = transform.localEulerAngles.x;

                        _Fire();
                    }
                }
            }

            previousTouchpadAxis = touchpadAxis;
            previousTriggerAxis = triggerAxis;
        }
    }
}