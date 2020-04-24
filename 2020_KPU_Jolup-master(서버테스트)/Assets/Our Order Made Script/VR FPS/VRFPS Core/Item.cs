using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    public class Item : MonoBehaviour
    {
        public List<string> tags = new List<string>();

        [HideInInspector] public int itemOffsetPressed = 0;
        [HideInInspector] public GameObject controller;

        protected ItemSFX_Manager audioMngr; //아이템 오디오 매니저
        protected AttachmentManager attachmentMngr; //부착품 매니저

        protected Rigidbody rb;
        protected Collider col;

        public Rigidbody Rb
        {
            get
            {
                return rb;
            }
        }

        public Collider Col
        {
            get
            {
                return col;
            }
        }

        [SerializeField] protected List<Collider> colliders = new List<Collider>();

        [SerializeField] protected VectorHistoryAverage velocityHistory;
        public Vector3 Velocity
        {
            get
            {
                return velocityHistory._ReleaseVelocity;
            }
        }

        [SerializeField] protected InteractionVolume primaryGrip; //처음 잡는 부분
        [SerializeField] protected InteractionVolume secondaryGrip; //2번 째로 잡을수 있는 부분

        public InteractionVolume PrimaryGrip
        {
            get
            {
                return primaryGrip;
            }

        }

        protected InteractionVolume SecondaryGrip
        {
            get
            {
                return secondaryGrip;
            }
        }

        [SerializeField] protected Slot slot; //슬롯

        public Slot Slot
        {
            get
            {
                return slot;
            }
        }

        public Hand PrimaryHand
        {
            get
            {
                return primaryGrip ? primaryGrip.Hand : null;
            }

        }
        public Hand SecondaryHand
        {
            get
            {
                return secondaryGrip ? secondaryGrip.Hand : null;
            }
        }

        [SerializeField] protected InteractionVolume harnessInput;
        protected Slot harnessedSlot;

        public GameObject PrimaryHighlight //잡을 수 있을 떄 표현
        {
            get
            {
                return primaryGrip.Highlight;
            }
        }

        bool restrained;

        public bool Restrained 
        {
            get
            {
                return restrained;
            }

            set
            {
                primaryGrip.restrained = value;

                if (secondaryGrip)
                {
                    secondaryGrip.restrained = value;
                }

                restrained = value;
            }
        }

        public enum PoseType //잡는 타입
        {
            Static,
            Position,
            PositionAndRotation,
            TwoHandAligned,
            TwoHandAlignedSecondaryStatic,
        }

        [SerializeField] protected PoseType poseType = PoseType.PositionAndRotation;

        [SerializeField] protected ConfigurableJoint configurableJoint;
        protected ConfigurableJoint physicsJoint;

        [System.Serializable]
        public struct PhysicsSettings //물리 세팅
        {
            public bool isKinematic; 
            public bool useGravity;
            public bool colliderEnabled;
            public bool isTrigger;
            public bool parent;
        }

        [SerializeField] protected PhysicsSettings onAttach = 
            new PhysicsSettings { isKinematic = true, useGravity = false, colliderEnabled = true, isTrigger = false, parent = true };

        [SerializeField] protected PhysicsSettings onDetach = 
            new PhysicsSettings { isKinematic = false, useGravity = true, colliderEnabled = true, isTrigger = false, parent = false };

        public PhysicsSettings OnAttach
        {
            get
            {
                return onAttach;
            }
        }
        public PhysicsSettings OnDetach
        {
            get
            {
                return onDetach;
            }
        }

        [SerializeField] protected Vector3 positionOffset;
        [SerializeField] protected Vector3 rotationOffset; 

        [SerializeField] protected Vector3 alignedRotationOffset;

        public Vector3 PositionOffset
        {
            get
            {
                return positionOffset;
            }
            set
            {
                positionOffset = value;
            }
        }

        public Vector3 RotationOffset
        {
            get
            {
                return rotationOffset;
            }
            set
            {
                rotationOffset = value;
            }
        }


        //그렙 했을 때 포지션이랑 로테이션 바뀌는 속도 설정
        protected float posGrabTime;
        protected float rotatGrabTime;

        protected Vector3 grabLocalPos;
        protected Quaternion grabLocalRotat;
        protected Vector3 grabPos;
        protected Quaternion grabRotat;

        [SerializeField] protected float setPositionSpeed = 1; 
        [SerializeField] protected float setRotationSpeed = 1;
        [SerializeField] protected float setTwoHandRotationSpeed = 5;

        [SerializeField] protected AnimationCurve setPositionCurve;
        [SerializeField] protected AnimationCurve setRotationCurve;

       
        [SerializeField] protected bool reverseHandAlignment;

        [SerializeField] protected bool storable = true;

        public bool Storable
        {
            get
            {
                return storable;
            }
        }

        [SerializeField] protected int size;
        public int Size
        {
            get
            {
                return size;
            }
        }

        [SerializeField] protected bool stackable;
        public bool Stackable
        {
            get
            {
                return stackable;
            }
        }

        [SerializeField] protected string stackableID;
        public string StackableID
        {
            get
            {
                return stackableID;
            }
        }

        [Range(-1, 1)] [SerializeField] protected float autoDropDot = 0.5f;

       
        protected virtual void Awake()
        {
            col = GetComponent<Collider>();
            rb = GetComponent<Rigidbody>();

            Collider[] tempColliders = GetComponentsInChildren<Collider>();

            foreach (Collider col in tempColliders)
            {
                colliders.Add(col);
            }

            virtualStock = GetComponent<VirtualStock>();
        }

        protected virtual void Start()
        {
            TwitchExtension.SetLayerRecursivly(gameObject, "Item"); //"아이템" 레이러로 설정

            if (rb)
            {
                rb.maxAngularVelocity = 10f;
            }

            if (!attachmentMngr)
            {
                attachmentMngr = GetComponent<AttachmentManager>();
            }
            if (!attachmentMngr && GetComponentInChildren<AttachmentSocket>())
            {
                gameObject.AddComponent<AttachmentManager>();
            }

            if (!audioMngr)
            {
                audioMngr = GetComponent<ItemSFX_Manager>();
            }

            velocityHistory.InitializeHistory();

            if (primaryGrip)
            {
                primaryGrip._StartInteraction += PrimaryGrasp;
                primaryGrip._EndInteraction += PrimaryDrop;

                primaryGrip._OnOverlapInteraction += PreOverlap;
            }

            if (secondaryGrip)
            {
                secondaryGrip._StartInteraction += SecondaryGrasp;
                secondaryGrip._EndInteraction += SecondaryDrop;
                secondaryGrip._OnEnterOverlap += AutoGrabSecondary;
            }

            if (harnessInput)
                harnessInput._EndInteraction += HarnessToSlot;

            if(configurableJoint)
            {
                onAttach.isKinematic = true;
                onAttach.useGravity = false;
                onAttach.colliderEnabled = true;
                onAttach.isTrigger = false;
                onAttach.parent = true;

                onDetach.isKinematic = false;
                onDetach.useGravity = true;
                onDetach.colliderEnabled = true;
                onDetach.isTrigger = false;
                onDetach.parent = false;
            }
        }

        protected virtual void FixedUpdate()
        {
            AutoDrop();
        }

        protected virtual void Update()
        {
            Hand tempHand = PrimaryHand ?? SecondaryHand;

            if (tempHand)
            {
                velocityHistory.VelocityStep(transform);
            }
        }

        public void Attach(Hand newHand)
        {
            primaryGrip.ForceStartInteraction(newHand);
        }

        protected virtual void PrimaryGrasp() //우선 잡기 - 처음으로 잡는 거
        {
            if (!PrimaryHand)
            {
                return;
            }

            PrimaryHand.StoredItem = this;

            TwitchExtension.SetItemLayerRecursivly(gameObject, "HeldItem");

            SetInitialgrabRotatAndPosition(PrimaryHand, true, onAttach.parent, true);

            SwapStackAfterOverlap();

            DetachSlotLight(slot);

            if (audioMngr)
            {
                audioMngr.GetAudioSources(5, PrimaryHand.audioSourceContainer.gameObject);
            }
            if (audioMngr)
            {
                audioMngr.PlayRandomAudioClip(audioMngr.grabSounds, PrimaryGrip.transform.position);
            }
        }

        protected virtual void PrimaryDrop() //처음으로 잡았던거 떨굴때
        {
            if (!PrimaryHand)
            {
                return;
            }

            DetachSlotLight(PrimaryHand);

            if (SecondaryHand) //만약 세컨드 핸드가 있음
            {
                Hand tempHand = SecondaryHand;

                // 포지션이랑 로테이션 변경
                if (poseType == PoseType.PositionAndRotation) 
                {                                            
                    transform.SetParent(null);
                    SecondaryGrip.StopInteraction();
                    StartCoroutine(DelayedGrab(this, tempHand));
                }
                else
                {
                    SetInitialgrabRotatAndPosition(tempHand, false, onAttach.parent, false);
                }

                return;
            }

            DropItem();

            PrimaryHand.Offset.localPosition = Vector3.zero;
            PrimaryHand.Offset.localRotation = Quaternion.identity;

            if (audioMngr)
            {
                audioMngr.PlayRandomAudioClip(audioMngr.dropSounds, PrimaryGrip.transform.position);
            }
            if (audioMngr)
            {
                audioMngr.RemoveAudioSources(PrimaryHand.audioSourceContainer.gameObject);
            }
        }

        protected virtual void SecondaryGrasp() //2번쨰로 잡는거 
        {
            if (!SecondaryHand)
                return;

            SecondaryHand.StoredItem = this;

            if (PrimaryHand)
            {
                if (poseType == PoseType.TwoHandAligned)
                    SetInitialgrabRotatAndPosition(PrimaryHand, false, false, false);
            }
            else
            {
                TwitchExtension.SetItemLayerRecursivly(gameObject, "HeldItem");
                SetInitialgrabRotatAndPosition(SecondaryHand, false, true, true);
            }

            DetachSlotLight(slot);

            if (audioMngr)
            {
                audioMngr.GetAudioSources(5, SecondaryHand.audioSourceContainer.gameObject);
            }
            if (audioMngr)
            {
                audioMngr.PlayRandomAudioClip(audioMngr.grabSounds, PrimaryGrip.transform.position);
            }
        }

        protected virtual void SecondaryDrop() //2번쨰로 잡는거 놓을 때
        {
            if (!SecondaryHand)
            {
                return;
            }

            DetachSlotLight(SecondaryHand);

            if (PrimaryHand) //첫번째로 잡는거 있으면 로테이션과 포지션 변경
            {
                SetInitialgrabRotatAndPosition(PrimaryHand, false, false, false);
            }

            else
            {
                DropItem();
            }

            SecondaryHand.Offset.localPosition = Vector3.zero;
            SecondaryHand.Offset.localRotation = Quaternion.identity;

            if (audioMngr)
            {
                audioMngr.PlayRandomAudioClip(audioMngr.dropSounds, PrimaryGrip.transform.position);
            }
            if (audioMngr)
            {
                audioMngr.RemoveAudioSources(SecondaryHand.audioSourceContainer.gameObject);
            }
        }

        Hand overlapSwapSlot;
        bool dontDestroyOnOverlap;
        [HideInInspector] public bool dropStack = true;

        void PreOverlap(Hand hand) //겹쳐 잡기
        {
            dontDestroyOnOverlap = true;

            if (!stackable)
            {
                return;
            }

            overlapSwapSlot = hand;
            dropStack = false;
        }


        //잡기 시작 포지션과 로테이트 설정
        protected virtual void SetInitialgrabRotatAndPosition(Slot slot, bool orientate, bool setParent, bool setPhysics)
        {
            SetInitialgrabRotatAndPosition(slot, orientate, setParent, setPhysics, PositionOffset);
        }

        protected virtual void SetInitialgrabRotatAndPosition(Slot slot, bool orientate, bool setParent, bool setPhysics, Vector3 positionOffset)
        {
            Transform grip = slot.Offset;

            if (setPhysics)
            {
                SetPhysics(onAttach);
            }

            if (orientate)
            {
                grip.position = transform.TransformPoint(-positionOffset);
                grip.rotation = transform.rotation;
            }

            if (setParent)
            {
                transform.SetParent(grip, true);
            }

            posGrabTime = Time.time;
            rotatGrabTime = Time.time;

            grabRotat = grip.rotation;
            grabPos = grip.localPosition;

            grabLocalPos = grip.localPosition;
            grabLocalRotat = grip.localRotation;

            SetPhysicsJoint(slot.OffsetRigidbody);
        }

        //물리 설정
        public void SetPhysicsJoint(Rigidbody rb)
        {
            if (configurableJoint)
            {
                if (rb)
                {
                    if (!physicsJoint)
                    {
                        physicsJoint = gameObject.AddComponent<ConfigurableJoint>();
                    }
                }
            }

            if (physicsJoint)
            {
                SetKinematic(false);
                physicsJoint.xDrive = configurableJoint.xDrive;
                physicsJoint.yDrive = configurableJoint.yDrive;
                physicsJoint.zDrive = configurableJoint.zDrive;
                physicsJoint.angularYZDrive = configurableJoint.angularYZDrive;
                physicsJoint.angularXDrive = configurableJoint.angularXDrive;
                physicsJoint.connectedBody = rb;
            }
        }

        public void SetKinematic(bool kinematic)
        {
            if (!rb)
            {
                return;
            }

            if (rb.isKinematic != kinematic)
            {
                rb.isKinematic = kinematic;
            }
        }

        public void SetKinematic()
        {
            SetKinematic(true);
        }

        public void SetPhysics(PhysicsSettings settings)
        {
            if (!rb)
            {
                return;
            }

            if (rb.isKinematic != settings.isKinematic)
            {
                rb.isKinematic = settings.isKinematic;
            }
            if (rb.isKinematic != settings.useGravity)
            {
                rb.useGravity = settings.useGravity;
            }

            if (!col)
            {
                return;
            }

            if (col.enabled != settings.colliderEnabled)
            {
                col.enabled = settings.colliderEnabled;
            }

            if (col.isTrigger != settings.isTrigger)
            {
                col.isTrigger = settings.isTrigger;
            }
        }

        public void SetPhysics(bool kinematic, bool gravity, bool collider)
        {
            SetPhysics(kinematic, gravity, collider, col ? col.isTrigger : false);
        }

        public void SetPhysics(bool kinematic, bool gravity, bool collider, bool isTrigger)
        {
            if (!rb)
            {
                return;
            }

            if (rb.isKinematic != kinematic)
            {
                rb.isKinematic = kinematic;
            }
            if (rb.isKinematic != gravity)
            {
                rb.useGravity = gravity;
            }

            if (!col)
            {
                return;
            }

            if (col.enabled != collider)
            {
                col.enabled = collider;
            }

            if (col.isTrigger != isTrigger)
            {
                col.isTrigger = isTrigger;
            }
        }

        public void IgnoreCollision(Collider collider, bool ignore) //충돌 무시
        {
            Physics.IgnoreCollision(collider, Col, ignore);

            foreach (Collider col in colliders)
            {
                if (col == null)
                {
                    colliders.Remove(col);

                }
                else if (!col.isTrigger)
                {
                    Physics.IgnoreCollision(collider, col, ignore);
                }
            }
        }

        IEnumerator DelayedGrab(Item item, Hand newHand) //그렙 딜레이
        {
            yield return new WaitForEndOfFrame();

            item.Attach(newHand);
        }

        public void Detach()
        {
            if (primaryGrip)
            {
                primaryGrip.StopInteraction();
            }

            if (secondaryGrip)
            {
                secondaryGrip.StopInteraction();
            }
        }

        public void DetachWithOutStoring()
        {
            storable = false;
            Detach();
            storable = true;
        }

        protected virtual void DropItem() //아이템 드랍
        {
            bool stored = false;

            if (physicsJoint && !dontDestroyOnOverlap)
            {
                Destroy(physicsJoint);
            }

            else
            {
                dontDestroyOnOverlap = false;
            }

            if (storable)
            {
                Slot inventorySlot = (PrimaryHand ?? SecondaryHand).GetClosestValidSlot(this)
                    ?? (harnessedSlot ? harnessedSlot.HasItem ? null : harnessedSlot : null);

                if (inventorySlot)
                {
                    if (!inventorySlot.HasItem) //아이템 가지고 있는지
                    {
                        StoreOnSlot(inventorySlot);

                        if (dropStack && PrimaryHand) 
                        {
                            for (int i = 0; i < PrimaryHand.stackedItems.Count; i++)
                            {
                                inventorySlot.AddToStack(PrimaryHand.stackedItems[i]);
                            }

                            PrimaryHand.stackedItems.Clear();
                        }

                        return;
                    }
                    else
                    {
                        //가지고 있는 아이템 인벤토리랑 교체

                        Hand tempHand = PrimaryHand ?? SecondaryHand;
                        Item invItem = inventorySlot.StoredItem;

                        Item[] tempStack = new Item[inventorySlot.stackedItems.Count];
                        inventorySlot.stackedItems.CopyTo(tempStack);
                        inventorySlot.stackedItems.Clear();

                        invItem.DetachSlot();

                        StoreOnSlot(inventorySlot);

                        if (dropStack)
                        {
                            for (int i = 0; i < tempHand.stackedItems.Count; i++)
                            {
                                inventorySlot.AddToStack(tempHand.stackedItems[i]);
                            }

                            tempHand.stackedItems.Clear();
                        }

                        StartCoroutine(DelayedGrab(invItem, tempHand));

                        if (dropStack)
                        {
                            for (int x = 0; x < tempStack.Length; x++)
                                tempHand.AddToStack(tempStack[x]);
                        }
                    }

                    stored = true;
                }
            }

            if (!stored)
            {
                transform.SetParent(null);
                SetPhysics(onDetach);
                StartCoroutine(velocityHistory.ReleaseVelocity(rb));
                TwitchExtension.SetItemLayerRecursivly(gameObject, "Item");
                DropStack(PrimaryHand);
            }
        }

        public virtual void StoreOnSlot(Slot slot) //슬롯에 저장
        {
            this.slot = slot;

            if (slot == null)
            {
                return;
            }

            if (physicsJoint)
            {
                Destroy(physicsJoint);
            }

            slot.StoredItem = this;
            TwitchExtension.SetItemLayerRecursivly(gameObject, "InventoryItem");
            SetInitialgrabRotatAndPosition(slot, true, true, true);

            SetKinematic();
        }

        public virtual void DetachSlotLight(Slot slot)
        {
            if (slot)
                slot.UnstoreItemLight(this);

            this.slot = null;
        }

        public virtual void DetachSlot()
        {
            if (slot)
            {
                DetachSlotLight(slot);

                transform.SetParent(null, true);
                SetPhysics(onDetach);
                TwitchExtension.SetItemLayerRecursivly(gameObject, "Item");
            }
        }

        void HarnessToSlot()
        {
            if (slot)
                harnessedSlot = harnessedSlot ? null : slot;
        }

        protected virtual void SwapStackAfterOverlap() //오버렙 중 스택 교체
        {
            Slot tempSlot = overlapSwapSlot ?? slot; 

            
            if (tempSlot)
            {
                Item[] stackedItems = new Item[tempSlot.stackedItems.Count];

                tempSlot.stackedItems.CopyTo(stackedItems);

                tempSlot.stackedItems.Clear();

                if (stackedItems != null)
                    for (int x = 0; x < stackedItems.Length; x++)
                    {
                        PrimaryHand.AddToStack(stackedItems[x]);
                    }
            }

            dropStack = true;
            overlapSwapSlot = null;
        }

        void DropStack(Hand hand) //스택 떨구기
        {
            if (!dropStack)
            {
                return;
            }

            if (!hand)
            {
                return;
            }

            for (int i = 0; i < hand.stackedItems.Count; i++)
            {
                hand.stackedItems[i].transform.SetParent(null);
                hand.stackedItems[i].SetPhysics(OnDetach);
                hand.stackedItems[i].Restrained = false;
            }

            hand.stackedItems.Clear();
        }

        protected virtual void AutoGrab(Hand hand, InteractionVolume iv) //오토그렙
        {
            if (!hand || !iv)
            {
                return;
            }

            if (!hand.AutoGrab)
            {
                return;
            }

            if (hand.AutoGrab && !iv.Hand && !hand.IsInteracting)
            {
                iv.ForceStartInteraction(hand);
            }
        }

        protected virtual void AutoGrabPrimary(Hand hand) //오토그렙으로 처음 잡기
        {
            if (!SecondaryGrip)
            {
                return;
            }

            if (!SecondaryHand)
            {
                return;
            }
            Vector3 forward2 = (SecondaryHand.transform.position - SecondaryHand.Sibling.transform.position).normalized * (reverseHandAlignment ? -1 : 1);

            if (Vector3.Dot(forward2, (SecondaryHand.Sibling.transform.rotation *
                Quaternion.Inverse(Quaternion.Euler(rotationOffset))) * Vector3.forward) > autoDropDot)
            {
                AutoGrab(hand, PrimaryGrip);
            }
        }

        protected virtual void AutoGrabSecondary(Hand hand) //오토 그렙으로 2번째 잡기
        {
            if (!PrimaryHand)
            {
                return;
            }

            Vector3 forward2 = (PrimaryHand.Sibling.transform.position - PrimaryHand.transform.position).normalized
                * (reverseHandAlignment ? -1 : 1);

            if (Vector3.Dot(forward2, (PrimaryHand.transform.rotation * Quaternion.Inverse(Quaternion.Euler(rotationOffset)))
                * Vector3.forward) > autoDropDot)
            {
                AutoGrab(hand, SecondaryGrip);
            }
        }

        protected virtual void AutoDrop() //오토 드랍
        {
            if (!SecondaryGrip)
            {
                return;
            }

            if (!(SecondaryHand && PrimaryHand))
            {
                return;
            }

            if (!SecondaryHand.AutoDrop)
            {
                return;
            }

            Vector3 forward2 = (SecondaryHand.transform.position - PrimaryHand.transform.position).normalized
                * (reverseHandAlignment ? -1 : 1);

            if (Vector3.Dot(forward2, (PrimaryHand.transform.rotation * Quaternion.Inverse(Quaternion.Euler(rotationOffset)))
                * Vector3.forward) < autoDropDot)
            {
                SecondaryGrip.StopInteraction();
            }
        }

        protected VirtualStock virtualStock;
        Vector3 velocity;

        public virtual void StartVirutalStock()
        {
            transform.parent = null;
            SetInitialgrabRotatAndPosition(PrimaryHand, true, true, false, PositionOffset
                + (Vector3.forward * virtualStock.StockOffset));
        }

        public virtual void StopVirtualStock()
        {
           transform.parent = null;
           SetInitialgrabRotatAndPosition(PrimaryHand ?? SecondaryHand, true, true, false);
        }

        public virtual void Pose() //포즈 타입 설정
        {
            Slot poseSlot = PrimaryHand ?? SecondaryHand ?? slot;

            if (poseSlot == null)
            {
                return;
            }

            Transform gripOffset = poseSlot.Offset;

            bool hasBothHands = false;

            switch (poseType)
            {
                case PoseType.Static: //고정

                    break;
                case PoseType.Position: //위치만 이동
                    gripOffset.localPosition = Vector3.Lerp(grabLocalPos,
                                    Vector3.zero,
                                    setRotationCurve.Evaluate((Time.time - posGrabTime) * setPositionSpeed));

                    break;
                case PoseType.PositionAndRotation: //위치랑 로테이션 딩동

                    gripOffset.localPosition = Vector3.Lerp(grabLocalPos,
                                                            Vector3.zero,
                                                            setRotationCurve.Evaluate((Time.time - posGrabTime) * setPositionSpeed));

                    gripOffset.localRotation = Quaternion.Lerp(grabLocalRotat,
                                                                Quaternion.Inverse(Quaternion.Euler(rotationOffset)),
                                                                setRotationCurve.Evaluate((Time.time - rotatGrabTime)
                                                                * setRotationSpeed));
                    break;
                case PoseType.TwoHandAligned: //두 손으로 잡을 때 두손 위치를 계산하여 이동

                    if (PrimaryHand || Slot)
                    {
                        Vector3 primaryPoint = gripOffset.parent.position;

                        bool abort = true;

                        if (virtualStock)
                        {
                            if (virtualStock.IsTheWeaponShouldered(primaryPoint))
                            {
                                gripOffset.position = Vector3.SmoothDamp(gripOffset.position,
                                    virtualStock.ShoulderedPoint(primaryPoint), ref velocity, 0.1f);

                                abort = false;
                            }
                        }

                        if(abort)
                        {
                            gripOffset.localPosition = Vector3.Lerp(grabLocalPos,
                                                                    Vector3.zero,
                                                                    setPositionCurve.Evaluate((Time.time - posGrabTime) * setPositionSpeed));
                        }
                    }

                    if (primaryGrip && secondaryGrip) //양손 잡고 있는지
                    {
                        if (PrimaryHand && SecondaryHand)
                        {
                            hasBothHands = true;
                        }
                    }

                    if (hasBothHands) //양손 잡고 있다면
                    {
                        Vector3 primaryPoint = PrimaryHand.Offset.position;
                        Vector3 secondaryPoint = SecondaryHand.Offset.position;

                        if (virtualStock)
                        {
                            if (virtualStock.IsTheWeaponShouldered(primaryPoint))
                            {
                                primaryPoint = virtualStock.ShoulderedPoint(primaryPoint);
                                secondaryPoint = virtualStock.ForwardPoint(secondaryPoint);
                            }
                        }

                        Vector3 forward = (secondaryPoint - primaryPoint).normalized * (reverseHandAlignment ? -1 : 1);

                        gripOffset.rotation = Quaternion.Lerp(grabRotat,
                                                                Quaternion.LookRotation(forward, PrimaryHand.transform.forward) * Quaternion.Euler(alignedRotationOffset), //rotationOffset
                                                                setRotationCurve.Evaluate((Time.time - rotatGrabTime) * setTwoHandRotationSpeed));
                    }
                    else if (PrimaryHand || Slot)
                    {
                        gripOffset.localRotation = Quaternion.Lerp(grabLocalRotat,
                                                                    Quaternion.Inverse(Quaternion.Euler(rotationOffset)),
                                                                    setRotationCurve.Evaluate((Time.time - rotatGrabTime) * setRotationSpeed));
                    }

                    break;

                case PoseType.TwoHandAlignedSecondaryStatic: //양손 잡고 위치 고정

                    break;
            }
        }

        public delegate void LocalInputEvent();

        public bool LocalInputDown(LocalInputEvent localInputEvent, OVRInput.Button button) //오큘러스 키 눌렀을때
        {
            if (button == null)
            {
                return false;
            }

            if (!PrimaryHand)
            {
                return false;
            }

            if (OVRInput.GetDown(button, PrimaryHand.inputSource))
            {
                if (localInputEvent != null)
                {
                    localInputEvent();
                }
                return true;
            }

            return false;
        }

        public bool LocalInputUp(LocalInputEvent localInputEvent, OVRInput.Button button) //오큘러스 키 버튼 땠을떄
        {
            if (button == null)
                return false;

            if (!PrimaryHand)
                return false;

            if (OVRInput.GetUp(button, PrimaryHand.inputSource))
            {
                if (localInputEvent != null)
                {
                    localInputEvent();
                }
                return true;
            }

            return false;
        }

        public enum TouchPadDirection //조이스틱 버튼
        {
            left,
            right,
            up,
            down,
            center,
            dontMatter,
            ignore
        }

        public bool TouchPadInput(LocalInputEvent touchpadEvent, TouchPadDirection direction)
        {
            return TouchPadInput(touchpadEvent, direction, PrimaryHand ? PrimaryHand.TouchpadAxis : Vector2.zero);
        }
        

        public bool TouchPadInput(LocalInputEvent touchpadEvent, TouchPadDirection direction, Vector2 touchpadAxis)
        {
            if (touchpadEvent == null)
            {
                return false;
            }

            switch (direction)
            {
                case TouchPadDirection.left:
                    if (touchpadAxis.y > -0.75f && touchpadAxis.y < 0.75f)
                        if (touchpadAxis.x < -0.5f)
                        {
                            touchpadEvent();
                            return true;
                        }
                    break;
                case TouchPadDirection.right:
                    if (touchpadAxis.y > -0.75f && touchpadAxis.y < 0.75f)
                        if (touchpadAxis.x > 0.5f)
                        {
                            touchpadEvent();
                            return true;
                        }
                    break;
                case TouchPadDirection.up:
                    if (touchpadAxis.x > -0.75f && touchpadAxis.x < 0.75f)
                        if (touchpadAxis.y > 0.5f)
                        {
                            touchpadEvent();
                            return true;
                        }
                    break;
                case TouchPadDirection.down:
                    if (touchpadAxis.x > -0.75f && touchpadAxis.x < 0.75f)
                        if (touchpadAxis.y < -0.5f)
                        {
                            touchpadEvent();
                            return true;
                        }
                    break;
                case TouchPadDirection.center:
                    if (Vector2.Distance(touchpadAxis, Vector2.zero) < 0.75f)
                    {
                        touchpadEvent();
                        return true;
                    }
                    break;
                case TouchPadDirection.dontMatter:
                    touchpadEvent();
                    return true;
            }

            return false;
        }

        public bool HasTag(string tag)
        {
            for (int i = 0; i < tags.Count; i++)
            {
                Debug.Log(tags[i] + " : " + tag);
                if (tags[i] == tag)
                    return true;
            }

            return false;
        }
    }
}