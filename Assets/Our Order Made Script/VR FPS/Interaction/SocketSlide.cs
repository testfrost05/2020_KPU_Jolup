using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Photon.Pun;

namespace VrFps
{
    [PunRPC]
    public class SocketSlide : Slide
    {
        [SerializeField] protected SteamVR_Action_Boolean ejectInput;
        public SteamVR_Action_Boolean EjectInput { get { return ejectInput; } }

        [SerializeField] protected VrFpsInput.TouchPadDirection ejectTouchpadDirection;
        public VrFpsInput.TouchPadDirection EjectTouchpadDirection { get { return ejectTouchpadDirection; } }

        Vector3 initialSize;
        Vector3 initialCenter;

        public delegate void OnStartSlide(Item item); //탄창 넣는거
        public OnStartSlide _OnStartSlide;

        public delegate void Detached(); //탄창 빼는거
        public Detached _OnDetach;

        public delegate void OnLoad(Item item); //장전
        public OnLoad _OnLoad;

        public delegate void OnGrab(); //잡고 있는거
        public OnGrab _OnGrab;

        [SerializeField] protected string acceptedTag;

        [ReadOnly] [SerializeField] protected Slot potentialSlot;
        [ReadOnly] [SerializeField] protected Item potentialSlider;

        protected Item lastItem;
        protected Hand lastHand;

        [ReadOnly] [SerializeField] protected Hand savedHand; 

        [SerializeField] protected TwitchExtension.DotAxis primaryAttachDotAxis = TwitchExtension.DotAxis.up;
        [SerializeField] protected TwitchExtension.DotAxis secondaryAttachDotAxis = TwitchExtension.DotAxis.right;
        [Range(-1, 1)] [SerializeField] protected float primaryAttachDotThreshold = 0.75f;
        [Range(-1, 1)] [SerializeField] protected float secondaryAttachDotThreshold = 0.75f;
        [SerializeField] protected float attachDistanceThreshold = 0.1f;
        [SerializeField] protected float detachDistanceThreshold = 0.2f;
        [SerializeField] protected float detachSliderPositionThreshold = 0.9f;

        protected bool offsetInitialAttach = true;

        [SerializeField] protected bool loadFromSlot = false;

        BoxCollider triggerCollider;

        [SerializeField] protected Collider[] itemColliders = new Collider[0];
        [SerializeField] protected float ejectSpeed = 5; // 뺴는 속도
        [SerializeField] protected float ejectAutoGrabDistance = 0.1f; 

        public float EjectSpeed
        {
            get
            {
                return
                    ejectSpeed;
            }
        }

        [SerializeField] protected Vector3 loadedCenter;
        [SerializeField] protected Vector3 loadedSize;

        [SerializeField] protected bool resetSlideOnLoad;

        protected override void Start()
        {
            gameObject.layer = LayerMask.NameToLayer("ItemDetection"); //레이어 마스크 설정
            triggerCollider = GetComponent<Collider>() as BoxCollider; //콜리더

            initialSize = triggerCollider.size;
            initialCenter = triggerCollider.center;
           
            //탄창 끼거나 뺄때
            if (slideObject) // 탄창을 낄때
            {
                SetColliderLoaded();

                potentialSlider = slideObject.GetComponent<Item>();
                potentialSlot = potentialSlider.PrimaryHand ?? potentialSlider.SecondaryHand ?? potentialSlider.Slot;
            }
            else //탄창을 뺼떄
            {
                Item tempItem = GetComponentInChildren<Item>();

                if (tempItem) 
                {
                    SetColliderLoaded();
                    slideObject = tempItem.transform;
                    potentialSlider = tempItem;
                    potentialSlot = potentialSlider.PrimaryHand ?? potentialSlider.SecondaryHand ?? potentialSlider.Slot;
                }
            }

            if (potentialSlider)
            {
                StartSliding();
                LoadSlider();
            }

            if (!slideObject)
            {
                interactionVolume.restrained = true;
            }

            Transform tempSlide = slideObject;

            base.Start();

            slideObject = tempSlide;

            _OnReachedEnd += LoadSlider;
            _OnReachedStart += DetachSlider;
        }

        public override void GrabSlide() //잡고 탄창에 끼거나 뺼떄
        {
            if (_OnGrab != null)
            {
                _OnGrab();
            }

            if (interactionVolume)
            {
                potentialSlot = interactionVolume.Hand;
                savedHand = savedHand ? savedHand : interactionVolume.Hand;
            }

            if (eject != null)
            {
                StopCoroutine(eject);
                eject = null;
            }

            base.GrabSlide();
        }

        void SetColliderLoaded()
        {
            triggerCollider.center = loadedCenter;
            triggerCollider.size = loadedSize;
        }

        void SetColliderInitial()
        {
           triggerCollider.center = initialCenter;
           triggerCollider.size = initialSize;
        }

        protected override void Update()
        {
            if (slideObject)
            {
                base.Update();
            }

            AttachPotentialSlider();

            DetachSliderByDistance();

            AutoDropLastSlider();
        }
        
        protected virtual void AutoDropLastSlider()  //버튼 누르면 탄창 알아서 빠짐
        {
            if (lastHand && lastItem)
            {
                if (interactionVolume.StartInputID != null)
                    if (VrFpsInput.InputUp(interactionVolume.StartInputID, lastHand))
                    {
                        if (lastHand.StoredItem == lastItem)
                        {
                            lastItem.Detach();
                        }

                        lastHand = null;
                        lastItem = null;
                    }
            }
        }

        protected virtual IEnumerator OnTriggerEnter(Collider other) //온트리거
        {
            if (other.isTrigger)
            {
                yield break;
            }

            if (slideObject)
            {
                yield break;
            }

            if (other.gameObject.tag != "Interactable")
            {
                yield break;
            }

            Item tempAttachedItem = other.transform.GetComponent<Item>();

            if (tempAttachedItem)
            {
                if (tempAttachedItem.HasTag(acceptedTag))
                {
                    if (tempAttachedItem.PrimaryHand || tempAttachedItem.SecondaryHand || (tempAttachedItem.Slot && loadFromSlot))
                    {
                        potentialSlider = tempAttachedItem;
                        potentialSlot = potentialSlider.PrimaryHand ?? potentialSlider.SecondaryHand ?? potentialSlider.Slot;
                    }
                }
            }
        }

        protected Hand slidingHand;

        protected virtual void AttachPotentialSlider()
        {
            if (!potentialSlider)
            {
                return;
            }

            if (!potentialSlot)
            {
                return;
            }

            if (slideObject)
            {
                return;
            }

            float distance = Vector3.Distance(startPosition.position, potentialSlider.transform.position);

            if (distance < attachDistanceThreshold)
            {
                Vector3 tempPrimaryDesiredRotation = TwitchExtension.ReturnAxis(primaryAttachDotAxis, transform);
                Vector3 tempPrimaryCurrentRotation = TwitchExtension.ReturnAxis(primaryAttachDotAxis, potentialSlider.transform);

                Vector3 tempSecondaryDesiredRotation = TwitchExtension.ReturnAxis(secondaryAttachDotAxis, transform);
                Vector3 tempSecondaryCurrentRotation = TwitchExtension.ReturnAxis(secondaryAttachDotAxis, potentialSlider.transform);

                if (Vector3.Dot(tempPrimaryCurrentRotation, tempPrimaryDesiredRotation) > primaryAttachDotThreshold
                && Vector3.Dot(tempSecondaryDesiredRotation, tempSecondaryCurrentRotation) > secondaryAttachDotThreshold)
                {
                    StartSliding();
                }
            }
        }

        IEnumerator SnapPose(Hand tempHand)
        {
            if (!tempHand)
            {
                yield break;
            }

        }

       

        protected virtual void StartSliding() //탄창을 끼기 시작할때 함수
        {
            if (potentialSlot) StartCoroutine(SnapPose(potentialSlot as Hand));

            interactionVolume.restrained = false;

            potentialSlider.Restrained = true;

            potentialSlider.dropStack = false;
            potentialSlider.DetachWithOutStoring();

            if (potentialSlot)
            {
                potentialSlot.UnstoreItem();
            }
           
            potentialSlider.dropStack = true;

            potentialSlider.SetPhysics(true, false, true, true);

            potentialSlider.transform.SetParent(endPosition, true);

            if (_OnStartSlide != null)
            {
                _OnStartSlide(potentialSlider);
            }

            slideObject = potentialSlider.transform;
            slideObject.rotation = startPosition.rotation;

            if (potentialSlot)
            {
                Hand tempHand = potentialSlot.GetType() == typeof(Hand) ? potentialSlot as Hand : null;

                if (tempHand && !interactionPoint)
                {
                    if (interactionVolume)
                    {
                        interactionVolume.ForceStartInteraction(tempHand);
                    }
                    else
                    {
                        GrabSlide(tempHand.transform);
                    }
                    potentialSlot.StoredItem = null;
                }
                else
                {
                    GrabSlide(potentialSlot.transform);
                }

            }

            if (offsetInitialAttach)
            {
                sliderOffset = 1f;
            }

            interactionVolume.handRoot = slideObject;
        }

        protected virtual void LoadSlider() //탄창을 꼈을 때
        {
            SetColliderLoaded();

            slideObject.localPosition = Vector3.zero;
            slideObject.transform.rotation = endPosition.rotation;

            if (interactionVolume)
            {
                interactionVolume.StopInteraction();
            }
            else
            {
                DetachSlide();
            }
            if (potentialSlot)
            {
                if (potentialSlot.GetType() == typeof(Hand))
                {
                    (potentialSlot as Hand).GrabFromStack();
                }
            }

            potentialSlot = null;

            if (_OnLoad != null)
            {
                _OnLoad(potentialSlider);
            }

            if (resetSlideOnLoad)
            {
                ClearSlider();
            }
            else if (potentialSlider.PrimaryGrip.Highlight)
            {
                interactionVolume.Highlight = potentialSlider.PrimaryGrip.Highlight;
            }
        }

        protected void DetachSliderByDistance()
        {
            if (potentialSlot)
            {
                float distance = Vector3.Distance(startPosition.position, potentialSlot.transform.position);

                if (distance >= detachDistanceThreshold && slidePosition >= detachSliderPositionThreshold)
                {
                    DetachSlider();
                }
            }
        }

        protected void DetachSlider() 
        {
            if (!slideObject)
            {
                return;
            }

            potentialSlider.Restrained = false;
            potentialSlider.transform.SetParent(null);

            if (interactionVolume)
            {
                interactionVolume.StopInteraction();
            }
            else
            {
                DetachSlide();
            }

            if (AutoEquipRemovedSlide() == false)
            {
                if (AutoEquipEjectedSlide(savedHand) == false)
                {
                    potentialSlider.SetPhysics(potentialSlider.OnDetach);
                }
            }
            ClearSlider();

            interactionVolume.restrained = true;
            interactionVolume.HighlightIsActive = false;
        }

        public void ClearSlider() //탄창을 뻈을떄
        {
            slideObject = null;
            potentialSlider = null;
            potentialSlot = null;
            interactionPoint = null;

            ResetSlider();
            SetColliderInitial();
            interactionVolume.Highlight = null;
        }

        protected override void ResetSlider() //리셋
        {
            onReachedEndHysteresis = true;
            onReachedStartHysteresis = false;
            base.ResetSlider();
        }

        bool AutoEquipRemovedSlide() 
        {
            bool interactionVolumeHand = interactionVolume ? interactionVolume.Hand : false;

            if (interactionVolumeHand)
            {
                potentialSlider.Attach(interactionVolume.Hand);
                SetForAutoDrop(interactionVolume.Hand);
            }
            else if (potentialSlot)
            {
                if (potentialSlot.GetType() == typeof(Hand))
                {
                    Hand tempHand = potentialSlot as Hand;
                    StartCoroutine(SnapPose(tempHand, potentialSlider));
                    potentialSlider.Attach(tempHand);
                    SetForAutoDrop(tempHand);
                }
                else if (!potentialSlot.HasItem)
                {
                    potentialSlider.StoreOnSlot(potentialSlot);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        IEnumerator SnapPose(Hand hand, Item item)
        {
            if (!hand)
            {
                yield break;
            }
        }

        void SetForAutoDrop(Hand hand) //오토 드랍 셋트
        {
            if(interactionVolume.StartInputID != null)
                if (VrFpsInput.Input(interactionVolume.StartInputID, hand))
                {
                    lastHand = hand;
                    lastItem = potentialSlider;
                }
        }

        bool AutoEquipEjectedSlide(Hand hand) 
        {
            if (!hand)
            {
                return false;
            }
            if (ValidSlot(hand))
            {
                return true;
            }
            if (ValidSlot(hand.Sibling))
            {
                return true;
            }
            return false;
        }

        bool ValidSlot(Hand hand)
        {
            if (interactionVolume.StartInputID == null)
            {
                return false;
            }

            if (Vector3.Distance(startPosition.position, hand.transform.position) <= ejectAutoGrabDistance)
            {
                if (!hand.HasItem && !hand.IsInteracting
                && VrFpsInput.Input(interactionVolume.StartInputID, hand))
                {
                    potentialSlider.Attach(hand);
                    lastHand = hand;
                    lastItem = potentialSlider;
                    return true;
                }
            }
            return false;
        }

        IEnumerator eject;

        public virtual void EjectSlider()
        {
            if (eject == null)
            {
                StartCoroutine(eject = EjectSliderCoroutine());
            }
        }

        protected virtual IEnumerator EjectSliderCoroutine()
        {
            if (!potentialSlider)
            {
                yield break;
            }
            if (interactionVolume.Hand)
            {
                yield break;
            }
            Item tempSlider = potentialSlider;

            float initialTime = Time.time;

            do
            {
                SetSlidePosition((Time.time - initialTime) * ejectSpeed);
                yield return new WaitForFixedUpdate();
            }
            while ((Time.time - initialTime) * ejectSpeed < 1);

            for (int i = 0; i < itemColliders.Length; i++)
            {
                Physics.IgnoreCollision(tempSlider.Col, itemColliders[i]);
            }

            _OnReachedStart();
            eject = null;

            yield return new WaitForSeconds(0.333f);

            for (int i = 0; i < itemColliders.Length; i++)
            {
                Physics.IgnoreCollision(tempSlider.Col, itemColliders[i], false);
            }
        }
    }
}