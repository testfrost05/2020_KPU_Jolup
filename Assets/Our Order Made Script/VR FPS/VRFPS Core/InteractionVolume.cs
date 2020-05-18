using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    public class InteractionVolume : MonoBehaviour
    {
        [ReadOnly] [SerializeField] protected Hand hand;
        public Hand Hand { get { return hand; } }

        protected Collider interactionTrigger;

        [SerializeField] GameObject highlight; //하이라이트 - 총을 컨트롤러로 잡을 수 있을 때 무기에 색변화를 줘서 보여줌

        public bool ActiveHighlight //하이라이트 활성
        {
            set
            {
                if (highlight == null) //하이라이트 없으면 그냥 리턴
                    return;

                if (highlight.activeSelf != value) //하이라이트 활성화 변수가 다를 경우
                {
                    if (value == true) 
                    {
                        if (!(restrained || (!overlap && hand)))
                            highlight.SetActive(true); 
                    }
                    else
                        highlight.SetActive(false);
                }
            }
        }

        public GameObject Highlight 
        {
            set { highlight = value; }
            get { return highlight; }
        }

    
        [SerializeField] protected int priority = 1; //우선순위
        [SerializeField] protected int movementPriority; 

        public int Priority { get { return priority; } }
        public int MovementPriority { get { return movementPriority; } }

        public delegate void OverlapEvent(Hand hand); //오버렙 

        public OverlapEvent _OnEnterOverlap;
        public OverlapEvent _OnOverlapInteraction;
        public OverlapEvent _OnExitOverlap;

        public delegate void Interaction();

        public Interaction _StartInteraction;
        public Interaction _EndInteraction;

       
        [SerializeField] protected bool overlap; 
        public bool Overlap { get { return overlap; } }


        
        public bool restrained;

        public enum InteractionMode // 키 누르고 때고
        {
            Press,
            PressDown,
            PressUp,
            SecondPressUp,
            None
        }

        [ReadOnly] [SerializeField] protected int releaseCount;

        protected enum Input //컨트롤러 키 종류
        {
            Grip,
            Trigger,
            Touchpad,
            ApplicationMenu
        }

        [SerializeField] protected OVRInput.Button startInteractingInputID;
        [SerializeField] protected OVRInput.Button stopInteractingInputID;

        public OVRInput.Button StartInputID { get { return startInteractingInputID; } } //무슨키로 잡을건지 x로 잡을건지 손잡이쪽 버튼으로 잡을건지
        public OVRInput.Button EndInputID { get { return stopInteractingInputID; } } //무슨키로 놓을건지

        [SerializeField] protected InteractionMode startInteractionMode = InteractionMode.PressDown; // 어떻게 눌렀을때 잡을건지 설정한 키를 눌렀을때 
                                                                                                     // 혹은 놓았을때 등등..
        [SerializeField] protected InteractionMode endInteractionMode = InteractionMode.PressUp; // 어떻게 눌렀을때 놓을건지

        public enum EarlyExitCondition 
        {
            none,
            onExitTrigger,
            onExitSphereBounds,
            onExitBoxBounds
        }

        [SerializeField]
        protected EarlyExitCondition earlyExitCondition;

        [SerializeField] protected float SphereBoundsRadius;
        [SerializeField] protected Vector3 boxBounds;

        [Space(10)]

        [Header("Hand Pose Blend Settings")]

        [SerializeField] public Transform handRoot; //손 모양 변경

        [SerializeField] public Vector3 rightPosePositionOffset; // 오른손 위치 변경
        [SerializeField] public Vector3 rightPoseRotationOffset; // 오른손 로테이션 변경

        [SerializeField] public Vector3 leftPosePositionOffset; // 왼손
        [SerializeField] public Vector3 leftPoseRotationOffset; // 왼속

        [SerializeField] protected float poseTime = 0.2f;
        public float PoseTime { get { return poseTime; } set { poseTime = value; } }

        protected virtual void Start()
        {

            if (interactionTrigger == null) interactionTrigger = GetComponent<Collider>(); 

            if (handRoot == null) handRoot = transform;  

            gameObject.tag = "Interactable";
        }

        private float RightPoseValue;
        private float LeftPoseValue;

        private int RightPolarity;
        private int LeftPolarity;

        protected virtual void Update()
        {
            EndInput();

        }

        protected virtual void FixedUpdate()
        {
            EarlyExit();
        }

        protected bool handInTrigger;

        protected virtual void EarlyExit()
        {
            if (!hand)
                return;

            if (handInTrigger)
                return;

            switch (earlyExitCondition)
            {
                case EarlyExitCondition.none:
                    break; //earlyexitcondition이 none일경우 그냥 넘어감

                case EarlyExitCondition.onExitTrigger: //onExitTrigger일경우 바로 상호작용 불가
                    StopInteraction();
                    break;

                case EarlyExitCondition.onExitSphereBounds:

                    if (Vector3.Distance(transform.position, hand.transform.position) > SphereBoundsRadius)
                        StopInteraction();
                    //onExitSphereBounds일경우 설정한 SphereBoundsRadius보다 넘어가면 상호작용 불가
                    break;

                case EarlyExitCondition.onExitBoxBounds:

                    Vector3 relativePosition = transform.InverseTransformPoint(hand.transform.position);

                    relativePosition.x *= transform.lossyScale.x;
                    relativePosition.y *= transform.lossyScale.y;
                    relativePosition.z *= transform.lossyScale.z;

                    if (relativePosition.x > boxBounds.x
                     || relativePosition.y > boxBounds.y
                     || relativePosition.z > boxBounds.z)
                        StopInteraction();

                    if (relativePosition.x < -boxBounds.x
                     || relativePosition.y < -boxBounds.y
                     || relativePosition.z < -boxBounds.z)
                        StopInteraction();
                    //onExitBoxBounds일경우 설정한 boxBounds를 넘어가면 상호작용 불가
                    break;

                default:
                    break;
            }
        }

        public bool AttemptInteraction(Hand hand)
        {
            if (restrained)
                return false;

            if (!overlap && this.hand)
                return false;

            if (hand == this.hand)
                return false;

            if (DetectInput(hand, startInteractionMode, startInteractingInputID))
            {
                if (this.hand != null)
                    if (overlap && hand && this.hand != hand)
                    {
                        if (_OnOverlapInteraction != null)
                            _OnOverlapInteraction(this.hand);

                        StopInteraction();
                    }

                StartInteraction(hand);
                return true;
            }

            return false;
        }

        protected void EndInput()
        {
            if (!hand)
                return;

            if (DetectInput(hand, InteractionMode.PressUp, stopInteractingInputID)) //
                releaseCount++;

            if (DetectInput(hand, endInteractionMode, stopInteractingInputID)) // Dectectinput함수를 써서 값이 리턴되면 상호작용 불가능
                StopInteraction();
        }

        bool DetectInput(Hand hand, InteractionMode input, OVRInput.Button id) //입력키 감지
        {
            if (!hand)
                return false;

            if (id == null)
                return false;

            switch (input)
            {
                case InteractionMode.Press:
                    return OVRInput.Get(id, hand.inputSource); //상호작용 설정키가  press일때 오큘러스 키입력을 get

                case InteractionMode.PressDown:
                    return OVRInput.GetDown(id, hand.inputSource);  //상호작용 설정키가  pressdown일때 오큘러스 키입력을 getdown

                case InteractionMode.PressUp:
                    return OVRInput.GetUp(id, hand.inputSource); //상호작용 설정키가  pressup일때 오큘러스 키입력을 getup

                case InteractionMode.SecondPressUp:
                    return OVRInput.GetUp(id, hand.inputSource) && releaseCount == 2;
                    //상호작용 설정키가  secoondpressup일때 오큘러스 키입력을 getup으로 하되 카운터가 2여지만 가능
                default:
                    break;
            }

            return false;
        }

        public void ForceStartInteraction(Hand newHand)
        {
            releaseCount++;
            StartInteraction(newHand);
        }

        protected void StartInteraction(Hand newHand) //상호 작용 가능하게 하는 함수
        {
           

            handInTrigger = true;

            hand = newHand;

            hand.InteractingVolume = this;

            if (highlight)
                highlight.SetActive(false);

            if (_StartInteraction != null)
                _StartInteraction();

    
        }

    

        public void StopInteraction() //상호 작용 불가능하게 하는 함수
        {
          

            handInTrigger = false;

            if (_EndInteraction != null)
                _EndInteraction();

            releaseCount = 0;

            if (!hand)
                return;

            if (hand.InteractingVolume)
                if (hand.InteractingVolume == this)
                    hand.InteractingVolume = null;

            
            hand = null;
        }

   
        void OnTriggerExit(Collider other) 
        {
            if (hand)
                if (other.gameObject == hand.gameObject)
                    handInTrigger = false;
        }
    }
}