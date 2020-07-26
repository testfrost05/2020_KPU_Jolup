using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace VrFps
{
    [PunRPC]
    public class Trigger : MonoBehaviour //방아쇠 
    {
        public delegate void TriggerEvent();

        //트리거 이벤트 당기기, 누르고 있기, 풀기
        public TriggerEvent _TriggerPulled;
        public TriggerEvent _TriggerHeld;
        public TriggerEvent _TriggerReleased;

        protected bool releasedTrigger;

        public bool ReleasedTrigger
        {
            get
            {
                return releasedTrigger;
            }
            set
            {
                releasedTrigger = value;
            }
        }

        [SerializeField] [Range(0, 1)] protected float triggerRelease = 0.2f;
        [SerializeField] [Range(0, 1)] protected float triggerPull = 0.9f;

        [SerializeField] [Range(0, 1)] protected float doubleActionTriggerRelease = 0.2f;
        [SerializeField] [Range(0, 1)] protected float doubleActionTriggerPull = 0.9f;

        public float TriggerAxis(float input) //트리거 축
        {
            float tempTriggerAxis = releasedTrigger ? input : 0;

            tempTriggerAxis = TwitchExtension.NormalizeInput(0, triggerPull - triggerRelease, tempTriggerAxis - triggerRelease);

            return tempTriggerAxis;
        }

        protected enum PoseType //방아쇠를 어떻게 움직을 건가
        {
            position, //위치조정
            rotation // 로테이션 조정
        }

        [SerializeField] protected PoseType poseType = PoseType.rotation;

        protected enum Axis //어떤걸 바꿀건가
        {
            x, //x값
            y, //y값
            z, //z값
            none
        }

        [SerializeField] protected Axis poseAxis;
        [SerializeField] protected float poseInputScale;
        Vector3 initialPose;

        [SerializeField] protected Hammer hammer; //권총일 경우 해머 

        void Start()
        {
            initialPose = poseType == PoseType.position ? transform.localPosition : transform.localEulerAngles;
        }

        public virtual void PoseTrigger(float input) //총을 쏠때 트리거 움직임
        {
            Vector3 desiredVector = initialPose;

            float hammerDelta = hammer ? hammer.touchPadDelta * poseInputScale : 0;

            if (poseInputScale != 0)
            {
                switch (poseAxis) //Axis 선택에 따른 변경 
                {
                    case Axis.x:
                        desiredVector.x = (input * poseInputScale) + initialPose.x - hammerDelta;
                        break;
                    case Axis.y:
                        desiredVector.y = (-input * poseInputScale) + initialPose.y + hammerDelta;
                        break;
                    case Axis.z:
                        desiredVector.z = (input * poseInputScale) + initialPose.z - hammerDelta;
                        break;
                    case Axis.none:

                        break;
                }
            }

            if (poseType == PoseType.position)
            {
                transform.localPosition = desiredVector;
            }
            else
            {
                transform.localEulerAngles = desiredVector;
            }

            float tempTriggerPull = hammer ? (hammer.Cocked ? doubleActionTriggerPull : triggerPull) : triggerPull;

            if (input >= tempTriggerPull)
            {
                if (releasedTrigger)
                {
                    _TriggerPulled();
                    releasedTrigger = false;
                }

                _TriggerHeld();
            }

            float tempTriggerRelease = hammer ? (hammer.Cocked ? doubleActionTriggerRelease : triggerRelease) : triggerRelease;

            if (input <= tempTriggerRelease && !releasedTrigger)
            {
                _TriggerReleased();
                releasedTrigger = true;
            }
        }
    }
}