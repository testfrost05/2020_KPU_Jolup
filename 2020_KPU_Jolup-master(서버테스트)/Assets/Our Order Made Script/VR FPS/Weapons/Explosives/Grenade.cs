using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    public class Grenade : ExplosiveItem
    {
        [SerializeField] protected SocketSlide pinSlide; //안전핀 뽑는건 소켓슬라이드 스크립트 사용

        [SerializeField] protected Collider handleCol; //안전손잡이 콜리더
        [SerializeField] protected Rigidbody handleRb; //안전손잡이 리기드바디
        [SerializeField] protected float handleRotateSpeed; //안전손잡이 로테이션 속도
        [SerializeField] protected float handleEjectForce; //안전손잡이 날라가는 힘
        public Transform pin;
        public Transform ring;

        [SerializeField] protected float forceThreshold = -1;

        [SerializeField] protected TouchPadDirection ejectHandleTouchpadDirection; //안전손잡이 제거 버튼
        [SerializeField] protected OVRInput.Button ejectHandleInput; //안전손잡이 제거 입력

        protected override void Start()
        {
            base.Start();
            pinSlide = GetComponentInChildren<SocketSlide>();
            explosive = GetComponent<Explosive>();
            pinSlide._OnReachedStart += Arm;

            IgnoreCollision(pin.GetComponent<Collider>(), true);

            if (handleCol)
            {
                Physics.IgnoreCollision(col, handleCol, true);
            }
        }

        protected override void PrimaryDrop()
        {
            base.PrimaryDrop();

            if (armed)
            {
                StartCoroutine(EjectHandle());
            }
        }

        protected override void Update()
        {
            base.Update();

            if (!PrimaryHand)
            {
                return;
            }
            if (ejectHandleInput != null)
            {
                LocalInputUp(EjectHandleWrapper, ejectHandleInput);
            }
            if (LocalInputUp(null, PrimaryHand.TouchpadInput))
            {
                TouchPadInput(EjectHandleWrapper, ejectHandleTouchpadDirection);
            }
        }

        void EjectHandleWrapper()
        {
            if (armed)
            {
                StartCoroutine(EjectHandle());
            }
        }

        public override void Arm()
        {
            armed = true;
        }

        void OnCollisionEnter(Collision col)
        {
            if (col.relativeVelocity.magnitude > forceThreshold && armed)
            {
                Explode();
            }
        }

        IEnumerator EjectHandle() //안전손잡이 제거 함수
        {
            if (handleRb)
            {
                float angle = 0;

                while (angle < 90) //각도가 90도 이하일경우
                {
                    angle += handleRotateSpeed * Time.deltaTime;
                    handleRb.transform.Rotate(Vector3.right, -handleRotateSpeed * Time.deltaTime);
                    yield return null;
                }

                handleRb.isKinematic = false;
                handleRb.useGravity = true;
                handleCol.transform.parent = null;
                handleCol.isTrigger = false;

                handleRb.AddForceAtPosition(transform.up * handleEjectForce, transform.position, ForceMode.Impulse);
            }

            yield return ExplodeRoutine();
        }
    }
}