using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    public class Slide : MonoBehaviour //장전 슬라이드
    {
        public bool Restrain
        {
            set
            {
                if (interactionVolume)
                {
                    interactionVolume.restrained = value;
                }
            }
        }

        [SerializeField] protected InteractionVolume interactionVolume;

        public delegate void ReachedStart(); //시작점에 도달
        public delegate void ReachedEnd(); //끝에 도달
        public delegate void FullCycle();

        public ReachedEnd _OnReachedEnd;
        public ReachedStart _OnReachedStart;
        public FullCycle _OnFullCycle;

        public Transform startPosition; //시작점
        public Transform endPosition; //끝점

        public Vector3 StartPosition
        {
            get
            {
                return startPosition ? startPosition.position: Vector3.zero;
            }
        }
        public Vector3 EndPosition {
            get
            {
                return endPosition ? endPosition.position : Vector3.zero;
            }
        }

        [ReadOnly] public Transform slideObject; //슬라이드 오브젝트

        protected float sliderOffset;
        protected float handOffset;

        [ReadOnly] public float slidePosition; //슬라이드 위치

        protected float sliderLength;
        [HideInInspector] public float maxSliderDistance = 1;
        [HideInInspector] public float minSliderDistance = 0;
        protected bool onReachedEndHysteresis;
        [SerializeField] protected float endHysteresis = 0.1f;

        protected bool onReachedStartHysteresis;
        [SerializeField] protected float startHysteresis = 0.1f;

        [SerializeField] protected bool hasReachedEnd; //시작과 끝에 도달했는지 아닌지
        [SerializeField] protected bool hasReachedStart = true;

        [SerializeField] protected bool continuous;

        [SerializeField] [ReadOnly] protected Transform interactionPoint;//상호작용 지점
        public Transform InteractionPoint
        {
            get
            {
                return interactionPoint;
            }
        }

        protected virtual void Start()
        {
            if (!startPosition || !endPosition)
            {
                return;
            }

            sliderLength = Vector3.Distance(startPosition.position, endPosition.position); 

            if (slideObject)
            {
                Vector3 diff = (endPosition.position - startPosition.position);
                diff.Normalize();

                sliderOffset = Vector3.Dot(diff, (endPosition.position - slideObject.position) / sliderLength);
                handOffset = Vector3.Dot(diff, (endPosition.position - Vector3.one) / sliderLength);

                slidePosition = Mathf.Clamp(Vector3.Dot(diff, (endPosition.position - Vector3.one) / sliderLength) + sliderOffset - handOffset, minSliderDistance, maxSliderDistance);
            }

            StartCoroutine(MoveSlide());

            velocitySampleSize = 3;

            if (interactionVolume)
            {
                interactionVolume._StartInteraction += GrabSlide;
                interactionVolume._EndInteraction += DetachSlide;
            }

            if (!slideObject) slideObject = transform;
        }

        public void ForceStop() //멈춤
        {
            if (interactionVolume)
            {
                interactionVolume.StopInteraction();
            }
        }
        public void ForceStart(Hand hand) //힘 시작
        {
            if (interactionVolume)
            {
                interactionVolume.ForceStartInteraction(hand);
            }
        }

        public void OnEnable()
        {
            StartCoroutine(MoveSlide());
        }

        public virtual void GrabSlide() //잡는 부분 설정
        {
            if (interactionVolume.Hand)
                GrabSlide(interactionVolume.Hand.transform);
        }

        public virtual void GrabSlide(Transform newInteractPoint)
        {
            if (!slideObject)
            {
                return;
            }

                
            interactionPoint = newInteractPoint;

            Vector3 diff = (endPosition.position - startPosition.position);
            diff.Normalize();

            sliderOffset = Vector3.Dot(diff, (endPosition.position - slideObject.position) / sliderLength);
            handOffset = Vector3.Dot(diff, (endPosition.position - interactionPoint.transform.position) / sliderLength);
        }

        public virtual void DetachSlide()
        {
            interactionPoint = null;
        }

        protected float prevNormal;

        protected virtual void Update()
        {
            if (prevNormal != slidePosition)
            {
                prevNormal = slidePosition;

                if (slidePosition == 1)
                {
                    OnReachedStart();
                }
                if (slidePosition == 0)
                {
                    OnReachedEnd();
                }
            }

            if (slideObject)
            {
                if (slidePosition >= 0)
                {
                    if (slidePosition <= 1 - startHysteresis && !onReachedStartHysteresis)
                    {
                        onReachedStartHysteresis = true;
                    }
                    if (slidePosition >= 0 + endHysteresis && !onReachedEndHysteresis)
                    {
                        onReachedEndHysteresis = true;
                    }
                }
            }
            VelocityStep();
        }

        protected int velocitySampleSize = 3;
        protected float[] velocityHistory;
        protected int velocityHistoryStep = 1;

        protected float previousPosition;

        public void VelocityStep() //속도
        {
            if (velocityHistory == null)
            {
                velocityHistory = new float[velocitySampleSize];
            }

            velocityHistoryStep++;

            if (velocityHistoryStep >= velocityHistory.Length)
            {
                velocityHistoryStep = 0;
            }

            velocityHistory[velocityHistoryStep] = (slidePosition - previousPosition) / Time.deltaTime;
            previousPosition = slidePosition;
        }

        public float AverageVelocity() //평균 속도
        {
            float total = 0;

            foreach (float velHist in velocityHistory)
            {
                total += velHist;
            }

            return total / velocityHistory.Length;
        }

        protected virtual IEnumerator MoveSlide() //슬라이더 움직임
        {
            while (true)
            {
                if (!slideObject || !interactionPoint)
                {
                    yield return null;
                    continue;
                }

                Vector3 diff = (endPosition.position - startPosition.position);
                diff.Normalize();
                slidePosition = Mathf.Clamp(Vector3.Dot(diff, (endPosition.position - interactionPoint.transform.position) / sliderLength) + sliderOffset - handOffset, minSliderDistance, maxSliderDistance);
                Vector3 desiredPosition = Vector3.Lerp(endPosition.position, startPosition.position, slidePosition);
                slideObject.position = desiredPosition;
                PoseSlide();

                if (continuous)
                {
                    if ((previousPosition == 0 && slidePosition == 0) || (previousPosition == 1 && slidePosition == 1))
                    {
                        GrabSlide(interactionPoint);
                    }
                }
                yield return null;
            }
        }

        protected virtual void PoseSlide() { }

        public void SetSlidePositionToTransform(Transform setTo) //슬라이드 위치 설정
        {
            Vector3 diff = (endPosition.position - startPosition.position);
            diff.Normalize();
            SetSlidePosition(Vector3.Dot(diff, (endPosition.position - setTo.position) / sliderLength));
        }

        public void SetSlidePosition(float newNormal) //슬라이드 위치 설정
        {
            newNormal = Mathf.Clamp(newNormal, minSliderDistance, maxSliderDistance);
            slidePosition = newNormal;
            Vector3 desiredPosition = Vector3.Lerp(endPosition.position, startPosition.position, slidePosition);

            if (slideObject)
            {
                slideObject.position = desiredPosition;
            }
        }

        protected virtual void OnFullCycle()
        {
            if (_OnFullCycle != null)
            {
                _OnFullCycle();
            }
        }

        protected virtual void OnReachedStart() //시작점 위치에 도달하면 
        {
            if (!onReachedStartHysteresis)
            {
                return;
            }

            onReachedStartHysteresis = false;

            if (_OnReachedStart != null)
            {
                _OnReachedStart();
            }

            if (hasReachedStart)
            {
                return;
            }

            hasReachedStart = true;

            if (hasReachedEnd)
            {
                OnFullCycle();
            }

            hasReachedEnd = false;
        }

        protected virtual void OnReachedEnd() //끝점에 도착하면
        {
            if (!onReachedEndHysteresis)
            {
                return;
            }

            onReachedEndHysteresis = false;

            if (_OnReachedEnd != null)
            {
                _OnReachedEnd();
            }

            if (hasReachedEnd)
            {
                return;
            }

            hasReachedEnd = true;
            hasReachedStart = false;
        }


        protected IEnumerator slideBack;
        protected IEnumerator slideForward;

        public IEnumerator AnimateSlide(float[] speed) //반자동 총일 경우 알아서 장전 슬라이드 움직임
        {
            if (speed[0] <= 0 || speed[1] <= 0) //속도를 0이하로 하면 안함
                yield break;

            float startTime = Time.time;

            slideBack = AnimateSlideBack(speed[0]);
            slideForward = AnimateSlideForward(speed[1]);

            yield return slideBack;
            yield return slideForward;

            yield break;
        }

        public IEnumerator AnimateSlideBack(float speed) 
        {
            if (speed <= 0) 
                yield break;

            float startTime = Time.time;
            float initialDistanceNormal = slidePosition;

            while (slidePosition != 0)
            {
                SetSlidePosition(initialDistanceNormal - ((Time.time - startTime) * speed));
                yield return null;
            }
        }

        public IEnumerator AnimateSlideForward(float speed) //
        {
            if (speed <= 0)
                yield break;

            float startTime = Time.time;
            float initialDistanceNormal = slidePosition;

            while (slidePosition != 1)
            {
                SetSlidePosition(initialDistanceNormal + ((Time.time - startTime) * speed));
                yield return null;
            }
        }

        protected virtual void ResetSlider() //슬라이더 초기화
        {
            hasReachedStart = true;
            hasReachedEnd = false;
        }
    }
}