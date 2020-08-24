using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

namespace VrFps
{

    public class Item : MonoBehaviour
    {
        public List<string> tags = new List<string>();

        [HideInInspector] public int itemOffsetPressed = 0;
        [HideInInspector] public int itemFlippedOffsetPressed = 0;
        [HideInInspector] public GameObject controller;

        protected ItemSFX_Manager audioManager;
        protected AttachmentManager attachmentMngr;

        public Rigidbody rb;
        protected Collider col;

        public Rigidbody Rb { get { return rb; } }
        public Collider Col { get { return col; } }

        [SerializeField] protected List<Collider> colliders = new List<Collider>();

        [SerializeField] protected VectorHistoryAverage velocityHistory;
        public Vector3 Velocity { get { return velocityHistory._ReleaseVelocity; } }

        [SerializeField] protected InteractionVolume primaryGrip;
        [SerializeField] protected InteractionVolume secondaryGrip;

        public InteractionVolume PrimaryGrip { get { return primaryGrip; } }
        protected InteractionVolume SecondaryGrip { get { return secondaryGrip; } }

        [SerializeField] protected Slot slot;
        public Slot Slot { get { return slot; } }

        public Hand PrimaryHand { get { return primaryGrip ? primaryGrip.Hand : null; } }
        public Hand SecondaryHand { get { return secondaryGrip ? secondaryGrip.Hand : null; } }

        [SerializeField] protected InteractionVolume harnessInput;
        protected Slot harnessedSlot;

        public GameObject PrimaryHighlight { get { return primaryGrip.Highlight; } }

        bool restrained;

        public bool Restrained
        {
            get { return restrained; }

            set
            {
                primaryGrip.restrained = value;

                if (secondaryGrip)
                    secondaryGrip.restrained = value;

                restrained = value;
            }
        }

        public enum PoseType
        {
            Static,
            Position,
            PositionAndRotation,
            TwoHandAligned,
            TwoHandAlignedSecondaryStatic,
            VelocityOneHand,
            VelocityTwoHand,
            VelocityStatic
        }

        [SerializeField] protected PoseType poseType = PoseType.PositionAndRotation;

        public PoseType _PoseType { get { return poseType; } }

        [SerializeField] protected ConfigurableJoint configurableJoint;
        protected ConfigurableJoint physicsJoint;

        [System.Serializable]
        public struct PhysicsSettings
        {
            public bool isKinematic;
            public bool useGravity;
            public bool colliderEnabled;
            public bool isTrigger;
            public bool parent;
        }

        //Physics settings for equiping and dropping this item
        [SerializeField] protected PhysicsSettings onAttach = new PhysicsSettings { isKinematic = true, useGravity = false, colliderEnabled = true, isTrigger = false, parent = true };
        [SerializeField] protected PhysicsSettings onDetach = new PhysicsSettings { isKinematic = false, useGravity = true, colliderEnabled = true, isTrigger = false, parent = false };

        public PhysicsSettings OnAttach { get { return onAttach; } }
        public PhysicsSettings OnDetach { get { return onDetach; } }

        [SerializeField] public Vector3 positionOffset; //Use this to fix the position of this object when being held
        [SerializeField] public Vector3 rotationOffset; //Use this to fix the rotation of this object when being held

        [SerializeField] public Vector3 flippedPositionOffset;
        [SerializeField] public Vector3 flippedRotationOffset;

        //[SerializeField] protected Vector3 secondaryPositionOffset; //Use this to fix the position when holding this object with only the secondary hand
        //public Vector3 SecondaryPositionOffset { get { return secondaryPositionOffset; } }

        [SerializeField] protected Vector3 twoHandRotationOffset;

        public Vector3 PositionOffset { get { return flippedOffset ? flippedPositionOffset : positionOffset; } set { positionOffset = value; } }
        public Vector3 RotationOffset { get { return flippedOffset ? flippedRotationOffset : rotationOffset; } set { rotationOffset = value; } }

        protected float positionGrabTime;
        protected float rotationGrabTime;

        protected Vector3 grabLocalPosition;
        protected Quaternion grabLocalRotation;
        protected Vector3 grabPosition;
        protected Quaternion grabRotation;

        [SerializeField] protected float setPositionSpeed = 1;
        [SerializeField] protected float setRotationSpeed = 1;
        [SerializeField] protected float setTwoHandRotationSpeed = 5;

        [SerializeField] protected AnimationCurve setPositionCurve;
        [SerializeField] protected AnimationCurve setRotationCurve;

        [Tooltip("Use this if the primary grip is infront of the secondary grip (like an RPG) to reverse the aligned direction when holding with both hands")]
        [SerializeField] protected bool reverseHandAlignment;

        [SerializeField] protected bool storable = true;

        [SerializeField] protected int size;
        public int Size { get { return size; } }

        [SerializeField] protected bool stackable;
        public bool Stackable { get { return stackable; } }

        [SerializeField] protected string stackableID;
        public string StackableID { get { return stackableID; } }

        [Range(-1, 1)] [SerializeField] protected float autoDropDot = 0.5f;

        public SteamVR_Skeleton_Poser autoAttachSecondaryToPrimary;

        protected virtual void Awake()
        {
            if (!col) col = GetComponent<Collider>();
            rb = GetComponent<Rigidbody>();

            Collider[] tempColliders = GetComponentsInChildren<Collider>();

            foreach (Collider col in tempColliders)
                colliders.Add(col);

            virtualStock = GetComponent<VirtualStock>();
        }

        protected virtual void Start()
        {
            flippedOffset = false;

            if (!attachmentMngr) attachmentMngr = GetComponent<AttachmentManager>();
            if (!attachmentMngr && GetComponentInChildren<AttachmentSocket>()) attachmentMngr = gameObject.AddComponent<AttachmentManager>();

            if (attachmentMngr)
            {
                attachmentMngr._OnAttach += AddAttachment;
                attachmentMngr._OnDetach += RemoveAttachment;
            }

            TwitchExtension.SetLayerRecursivly(gameObject, "Item");

            if (rb) rb.maxAngularVelocity = 10f;

            if (!audioManager) audioManager = GetComponent<ItemSFX_Manager>();

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

            if (configurableJoint)
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

            if (VelocityPoseType())
            {
                rb.maxAngularVelocity = Mathf.Infinity;
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
                if (PrimaryHand)
                    LocalInputDown(FlipOffset, flipOffsetInput);
            }
        }

        protected virtual void LateUpdate()
        {
            Hand tempHand = PrimaryHand ?? SecondaryHand;

            if (tempHand)
            {
                velocityHistory.VelocityStep(transform);

            }
        }

        public void Attach(Hand newHand) { primaryGrip.ForceStartInteraction(newHand); }

        protected virtual void PrimaryGrasp()
        {
            if (!PrimaryHand)
                return;

            PrimaryHand.StoredItem = this;

            TwitchExtension.SetItemLayerRecursivly(gameObject, "HeldItem");

            SetInitialGrabRotationAndPosition(PrimaryHand, !VelocityPoseType(), onAttach.parent, true);

            SwapStackAfterOverlap();

            DetachSlotLight(slot);

            if (audioManager) audioManager.GetAudioSources(5, PrimaryHand.audioSourceContainer.gameObject);
            if (audioManager) audioManager.PlayRandomAudioClip(audioManager.grabSounds, PrimaryGrip.transform.position);

            SetOffsetPhysicsForPosing(PrimaryHand);

            if (SecondaryHand)
                initialTwoHandDirection = PrimaryHand.GlobalOffset.InverseTransformDirection((PrimaryHand.GlobalOffset.position - SecondaryHand.GlobalOffset.position)).normalized;
        }

        Vector3 staticPositionOffset;
        Vector3 staticRotationOffset;

        protected virtual void PrimaryDrop()
        {
            if (!PrimaryHand)
                return;

            DetachSlotLight(PrimaryHand);

            ResetOffsetPhysics(PrimaryHand, false);

            if (SecondaryHand)
            {
                Hand tempHand = SecondaryHand;

                staticPositionOffset = SecondaryHand.Offset.InverseTransformPoint(transform.position);
                staticRotationOffset = (Quaternion.Inverse(SecondaryHand.Offset.rotation) * transform.rotation).eulerAngles;

                if (poseType == PoseType.PositionAndRotation || poseType == PoseType.VelocityOneHand || poseType == PoseType.VelocityStatic) //If a two handed weapon is being held by both hands and is dropped by the primary hand
                {                                             //switch the secondary hand to the primary
                    transform.SetParent(null);
                    SecondaryGrip.StopInteraction();
                    StartCoroutine(DelayedGrab(this, tempHand));
                }
                else
                {
                    ResetOffsetPhysics(SecondaryHand, true);

                    SetInitialGrabRotationAndPosition(tempHand, false, onAttach.parent, false);

                    SetOffsetPhysicsForPosing(SecondaryHand);

                    PrimaryHand.Offset.localPosition = Vector3.zero;
                    PrimaryHand.Offset.localRotation = Quaternion.identity;
                }

                return;
            }

            DropItem();

            PrimaryHand.Offset.localPosition = Vector3.zero;
            PrimaryHand.Offset.localRotation = Quaternion.identity;

            if (audioManager) audioManager.PlayRandomAudioClip(audioManager.dropSounds, PrimaryGrip.transform.position);

            if (audioManager) audioManager.RemoveAudioSources(PrimaryHand.audioSourceContainer.gameObject);
        }

        protected Vector3 initialTwoHandDirection;

        protected virtual void SecondaryGrasp()
        {
            if (!SecondaryHand)
                return;

            SecondaryHand.StoredItem = this;

            if (PrimaryHand)
            {
                if (poseType == PoseType.TwoHandAligned)
                    SetInitialGrabRotationAndPosition(PrimaryHand, false, false, false);

                initialTwoHandDirection = PrimaryHand.GlobalOffset.InverseTransformDirection((PrimaryHand.GlobalOffset.position - SecondaryHand.GlobalOffset.position)).normalized;
            }
            else
            {
                staticPositionOffset = SecondaryHand.Offset.InverseTransformPoint(transform.position);
                staticRotationOffset = (Quaternion.Inverse(SecondaryHand.Offset.rotation) * transform.rotation).eulerAngles;

                TwitchExtension.SetItemLayerRecursivly(gameObject, "HeldItem");
                SetInitialGrabRotationAndPosition(SecondaryHand, false, true, true);

                SetOffsetPhysicsForPosing(SecondaryHand);
            }

            DetachSlotLight(slot);

            if (audioManager) audioManager.GetAudioSources(5, SecondaryHand.audioSourceContainer.gameObject);

            if (audioManager) audioManager.PlayRandomAudioClip(audioManager.grabSounds, PrimaryGrip.transform.position);
        }

        protected virtual void SecondaryDrop()
        {
            if (!SecondaryHand)
                return;

            DetachSlotLight(SecondaryHand);

            ResetOffsetPhysics(SecondaryHand, true);

            if (PrimaryHand)
                SetInitialGrabRotationAndPosition(PrimaryHand, false, false, false);
            else
                DropItem();

            SecondaryHand.Offset.localPosition = Vector3.zero;
            SecondaryHand.Offset.localRotation = Quaternion.identity;

            if (audioManager) audioManager.PlayRandomAudioClip(audioManager.dropSounds, PrimaryGrip.transform.position);

            if (audioManager) audioManager.RemoveAudioSources(SecondaryHand.audioSourceContainer.gameObject);
        }

        protected bool VelocityPoseType()
        {
            if (poseType == PoseType.VelocityTwoHand || poseType == PoseType.VelocityOneHand || poseType == PoseType.VelocityStatic)
                return true;

            return false;
        }

        protected virtual void SetOffsetPhysicsForPosing(Hand hand)
        {
            if (VelocityPoseType())
            {
                rb.isKinematic = false;
                rb.useGravity = false;

                transform.parent = hand.transform.root;

                //hand.Offset.parent = hand.transform.root;
                //hand.OffsetRigidbody.isKinematic = false;
                //hand.OffsetRigidbody.useGravity = false;
                //Destroy(rb);
            }
        }

        void ResetOffsetPhysics(Hand hand, bool resetOrientation)
        {
            if (VelocityPoseType())
            {
                rb.isKinematic = false;
                rb.useGravity = false;

                //transform.parent = hand.GlobalOffset;
                //hand.OffsetRigidbody.isKinematic = true;
                //hand.OffsetRigidbody.useGravity = false;
                //hand.Offset.parent = hand.GlobalOffset;

                if (resetOrientation)
                {
                    //hand.Offset.localPosition = Vector3.zero;
                    //hand.Offset.localRotation = Quaternion.identity;
                }
            }
        }

        Hand overlapSwapSlot;
        bool dontDestroyOnOverlap;
        [HideInInspector] public bool dropStack = true; //Drop the stacked items along with the stored one

        void PreOverlap(Hand hand)
        {
            dontDestroyOnOverlap = true;

            if (!stackable)
                return;

            overlapSwapSlot = hand;
            dropStack = false;
        }

        protected virtual void SetInitialGrabRotationAndPosition(Slot slot, bool orientate, bool setParent, bool setPhysics)
        {
            SetInitialGrabRotationAndPosition(slot, orientate, setParent, setPhysics, PositionOffset);
        }

        protected bool flippedOffset = false;
        public bool useFlippedOffset;

        public SteamVR_Action_Boolean flipOffsetInput;

        void FlipOffset()
        {
            bool tempUseFlipped = useFlippedOffset;

            useFlippedOffset = false;
            flippedOffset = !flippedOffset;
            transform.parent = null;
            SetInitialGrabRotationAndPosition(PrimaryHand, true, onAttach.parent, false, PositionOffset);
            SetOffsetPhysicsForPosing(PrimaryHand);
            useFlippedOffset = tempUseFlipped;
        }

        protected virtual void SetInitialGrabRotationAndPosition(Slot slot, bool orientate, bool setParent, bool setPhysics, Vector3 positionOffset)
        {
            bool previousFlipped = flippedOffset;

            if (useFlippedOffset)
                flippedOffset = PrimaryHand && !SecondaryHand ? Vector3.Dot(PrimaryHand.GlobalOffset.up, transform.up) < 0 : false;

            if (flippedOffset != previousFlipped)
                positionOffset = PositionOffset;

            Transform grip = slot.Offset;

            if (setPhysics)
                SetPhysics(onAttach);

            if (orientate)
            {
                grip.position = transform.TransformPoint(-positionOffset);
                grip.rotation = transform.rotation;
            }

            if (setParent)
                transform.SetParent(grip, true);

            positionGrabTime = Time.time;
            rotationGrabTime = Time.time;

            grabRotation = grip.rotation;
            grabPosition = grip.localPosition;

            grabLocalPosition = grip.localPosition;
            grabLocalRotation = grip.localRotation;

            SetPhysicsJoint(slot.OffsetRigidbody);
        }

        public void SetPhysicsJoint(Rigidbody rb)
        {
            if (configurableJoint)
                if (rb)
                    if (!physicsJoint)
                        physicsJoint = gameObject.AddComponent<ConfigurableJoint>();

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
                return;

            if (rb.isKinematic != kinematic)
                rb.isKinematic = kinematic;
        }

        public void SetKinematic() { SetKinematic(true); }

        public void SetPhysics(PhysicsSettings settings)
        {
            if (!rb)
                return;

            if (rb.isKinematic != settings.isKinematic)
                rb.isKinematic = settings.isKinematic;
            if (rb.isKinematic != settings.useGravity)
                rb.useGravity = settings.useGravity;

            if (!col)
                return;

            if (col.enabled != settings.colliderEnabled)
                col.enabled = settings.colliderEnabled;
            if (col.isTrigger != settings.isTrigger)
                col.isTrigger = settings.isTrigger;
        }

        public void SetPhysics(bool kinematic, bool gravity, bool collider)
        {
            SetPhysics(kinematic, gravity, collider, col ? col.isTrigger : false);
        }

        public void SetPhysics(bool kinematic, bool gravity, bool collider, bool isTrigger)
        {
            if (!rb)
                return;

            if (rb.isKinematic != kinematic)
                rb.isKinematic = kinematic;
            if (rb.isKinematic != gravity)
                rb.useGravity = gravity;

            if (!col)
                return;

            if (col.enabled != collider)
                col.enabled = collider;
            if (col.isTrigger != isTrigger)
                col.isTrigger = isTrigger;
        }

        public void IgnoreCollision(Collider collider, bool ignore)
        {
            Physics.IgnoreCollision(collider, Col, ignore);

            foreach (Collider col in colliders)
            {
                if (col == null)
                {
                    colliders.Remove(col);
                }
                else if (!col.isTrigger)
                    Physics.IgnoreCollision(collider, col, ignore);
            }
        }

        IEnumerator DelayedGrab(Item item, Hand newHand)
        {
            if (PrimaryHand.HandSkeleton)
            {
                bool leftHand = PrimaryHand.inputSource == SteamVR_Input_Sources.LeftHand;
                var tempPoser = leftHand ? primaryGrip.RightHandPoser : primaryGrip.LeftHandPoser;

                if (leftHand)
                {
                    primaryGrip.RightHandPoser = autoAttachSecondaryToPrimary;
                }
                else
                {
                    primaryGrip.LeftHandPoser = autoAttachSecondaryToPrimary;
                }
            }

            yield return new WaitForEndOfFrame();

            item.Attach(newHand);

            yield return new WaitForSeconds(PrimaryGrip.PoseTime + 0.01f);

            if (PrimaryHand)
                if (PrimaryHand.HandSkeleton)
                {
                    bool leftHand = PrimaryHand.inputSource == SteamVR_Input_Sources.LeftHand;
                    var tempPoser = leftHand ? primaryGrip.RightHandPoser : primaryGrip.LeftHandPoser;

                    if (leftHand)
                    {
                        primaryGrip.RightHandPoser = tempPoser;
                        primaryGrip.RightHandPoser.SetBlendingBehaviourValue("Interact", 1);
                    }
                    else
                    {
                        primaryGrip.LeftHandPoser = tempPoser;
                        primaryGrip.LeftHandPoser.SetBlendingBehaviourValue("Interact", 1);
                    }
                }
        }

        public void Detach()
        {
            if (primaryGrip)
                primaryGrip.StopInteraction();

            if (secondaryGrip)
                secondaryGrip.StopInteraction();
        }

        public void DetachWithOutStoring()
        {
            storable = false;
            Detach();
            storable = true;
        }

        protected virtual void ReaddRigidbody()
        {
            if (VelocityPoseType() == true)
                if (!rb)
                {
                    rb = gameObject.AddComponent<Rigidbody>();
                }
        }

        protected virtual void DropItem()
        {
            //ReaddRigidbody();

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
                Slot inventorySlot = (PrimaryHand ?? SecondaryHand).GetClosestValidSlot(this) ?? (harnessedSlot ? harnessedSlot.HasItem ? null : harnessedSlot : null);

                if (inventorySlot)
                {
                    if (!inventorySlot.HasItem) //Store the item if the slot is empty
                    {
                        StoreOnSlot(inventorySlot);

                        if (dropStack && PrimaryHand) //Move any stacked items to the slot
                        {
                            for (int i = 0; i < PrimaryHand.stackedItems.Count; i++)
                                inventorySlot.AddToStack(PrimaryHand.stackedItems[i]);

                            PrimaryHand.stackedItems.Clear();
                        }

                        return;
                    }
                    else
                    {
                        //Swap inventory item and the currently held item

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
                                inventorySlot.AddToStack(tempHand.stackedItems[i]);

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
                velocityHistory.ReleaseVelocity(rb);
                TwitchExtension.SetItemLayerRecursivly(gameObject, "Item");
                DropStack(PrimaryHand);
            }
        }

        protected virtual void ItemDroppedEvent()
        {

        }

        public virtual void StoreOnSlot(Slot slot)
        {
            this.slot = slot;

            if (slot == null)
                return;

            if (physicsJoint)
            {
                Destroy(physicsJoint);
            }

            slot.StoredItem = this;
            TwitchExtension.SetItemLayerRecursivly(gameObject, "InventoryItem");
            SetInitialGrabRotationAndPosition(slot, true, true, true);

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

        protected virtual void SwapStackAfterOverlap()
        {
            Slot tempSlot = overlapSwapSlot ?? slot; //Stacked items should only be simple onehanded objects.

            //Move the stacked items from the slot/other hand to this hand
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

        void DropStack(Hand hand)
        {
            if (!dropStack)
                return;

            if (!hand)
                return;

            for (int i = 0; i < hand.stackedItems.Count; i++)
            {
                hand.stackedItems[i].transform.SetParent(null);
                hand.stackedItems[i].SetPhysics(OnDetach);
                hand.stackedItems[i].Restrained = false;
            }

            hand.stackedItems.Clear();
        }

        protected virtual void AutoGrab(Hand hand, InteractionVolume iv)
        {
            if (!hand || !iv)
                return;

            if (!hand.AutoGrab)
                return;

            if (hand.AutoGrab && !iv.Hand && !hand.IsInteracting)
            {
                iv.ForceStartInteraction(hand);
            }
        }

        protected virtual void AutoGrabPrimary(Hand hand)
        {
            if (!SecondaryGrip)
                return;

            if (!SecondaryHand)
                return;

            Vector3 forward2 = (SecondaryHand.transform.position - SecondaryHand.Sibling.transform.position).normalized * (reverseHandAlignment ? -1 : 1);

            if (Vector3.Dot(forward2, (SecondaryHand.Sibling.transform.rotation * Quaternion.Inverse(Quaternion.Euler(RotationOffset))) * Vector3.forward) > autoDropDot)
                AutoGrab(hand, PrimaryGrip);
        }

        protected virtual void AutoGrabSecondary(Hand hand)
        {
            if (!PrimaryHand)
                return;

            Vector3 forward2 = (PrimaryHand.Sibling.transform.position - PrimaryHand.transform.position).normalized * (reverseHandAlignment ? -1 : 1);

            if (Vector3.Dot(forward2, (PrimaryHand.transform.rotation * Quaternion.Inverse(Quaternion.Euler(RotationOffset))) * Vector3.forward) > autoDropDot)
                AutoGrab(hand, SecondaryGrip);
        }

        protected virtual void AutoDrop()
        {
            if (!SecondaryGrip)
                return;

            if (!(SecondaryHand && PrimaryHand))
                return;

            if (!SecondaryHand.AutoDrop)
                return;

            Vector3 forward2 = (SecondaryHand.transform.position - PrimaryHand.transform.position).normalized * (reverseHandAlignment ? -1 : 1);

            if (Vector3.Dot(forward2, (PrimaryHand.transform.rotation * Quaternion.Inverse(Quaternion.Euler(RotationOffset))) * Vector3.forward) < autoDropDot)
            {
                SecondaryGrip.StopInteraction();
            }
        }

        protected virtual void AddAttachment(Attachment attachment)
        {
            attachment.gameObject.layer = gameObject.layer;

        }

        protected virtual void RemoveAttachment(Attachment attachment)
        {

        }

        protected VirtualStock virtualStock;
        Vector3 velocity;

        public virtual void StartVirutalStock()
        {
            transform.parent = null;
            SetInitialGrabRotationAndPosition(PrimaryHand, true, true, false, PositionOffset + (Vector3.forward * virtualStock.StockOffset));
        }

        public virtual void StopVirtualStock()
        {
            transform.parent = null;
            SetInitialGrabRotationAndPosition(PrimaryHand ?? SecondaryHand, true, true, false);
        }

        public virtual void Pose()
        {
            Slot poseSlot = PrimaryHand ?? SecondaryHand ?? slot;

            if (poseSlot == null)
                return;

            Transform gripOffset = poseSlot.Offset;

            bool hasBothHands = false;

            switch (poseType)
            {
                case PoseType.Static:

                    break;
                case PoseType.Position:
                    gripOffset.localPosition = Vector3.Lerp(grabLocalPosition,
                                    Vector3.zero,
                                    setRotationCurve.Evaluate((Time.time - positionGrabTime) * setPositionSpeed));

                    break;
                case PoseType.PositionAndRotation:

                    gripOffset.localPosition = Vector3.Lerp(grabLocalPosition,
                                                            Vector3.zero,
                                                            setRotationCurve.Evaluate((Time.time - positionGrabTime) * setPositionSpeed));

                    gripOffset.localRotation = Quaternion.Lerp(grabLocalRotation,
                                                                Quaternion.Inverse(Quaternion.Euler(RotationOffset)),
                                                                setRotationCurve.Evaluate((Time.time - rotationGrabTime) * setRotationSpeed));
                    break;
                case PoseType.TwoHandAligned:

                    if (PrimaryHand || Slot)
                    {
                        Vector3 primaryPoint = gripOffset.parent.position;

                        bool abort = true;

                        if (virtualStock)
                            if (virtualStock.IsTheWeaponShouldered(primaryPoint))
                            {
                                gripOffset.position = Vector3.SmoothDamp(gripOffset.position, virtualStock.ShoulderedPoint(primaryPoint), ref velocity, 0.1f);

                                abort = false;
                            }

                        if (abort)
                        {
                            gripOffset.localPosition = Vector3.Lerp(grabLocalPosition,
                                                                    Vector3.zero,
                                                                    setPositionCurve.Evaluate((Time.time - positionGrabTime) * setPositionSpeed));
                        }
                    }

                    if (primaryGrip && secondaryGrip)
                        if (PrimaryHand && SecondaryHand)
                            hasBothHands = true;

                    if (hasBothHands)
                    {
                        Vector3 primaryPoint = PrimaryHand.Offset.position;
                        Vector3 secondaryPoint = SecondaryHand.Offset.position;

                        if (virtualStock)
                            if (virtualStock.IsTheWeaponShouldered(primaryPoint))
                            {
                                primaryPoint = virtualStock.ShoulderedPoint(primaryPoint);
                                secondaryPoint = virtualStock.ForwardPoint(secondaryPoint);
                            }

                        Vector3 forward = (secondaryPoint - primaryPoint).normalized * (reverseHandAlignment ? -1 : 1);

                        gripOffset.rotation = Quaternion.Lerp(grabRotation,
                                                                Quaternion.LookRotation(forward, PrimaryHand.transform.forward) * Quaternion.Euler(twoHandRotationOffset), //rotationOffset
                                                                setRotationCurve.Evaluate((Time.time - rotationGrabTime) * setTwoHandRotationSpeed));
                    }
                    else if (PrimaryHand || Slot)
                    {
                        gripOffset.localRotation = Quaternion.Lerp(grabLocalRotation,
                                                                    Quaternion.Inverse(Quaternion.Euler(RotationOffset)),
                                                                    setRotationCurve.Evaluate((Time.time - rotationGrabTime) * setRotationSpeed));
                    }

                    break;

                case PoseType.TwoHandAlignedSecondaryStatic:

                    break;

                case PoseType.VelocityTwoHand:

                    if (slot && !PrimaryHand)
                    {
                        gripOffset.localPosition = Vector3.Lerp(grabLocalPosition,
                                        Vector3.zero,
                                        setRotationCurve.Evaluate((Time.time - positionGrabTime) * setPositionSpeed));

                        gripOffset.localRotation = Quaternion.Lerp(grabLocalRotation,
                                                                    Quaternion.Inverse(Quaternion.Euler(RotationOffset)),
                                                                    setRotationCurve.Evaluate((Time.time - rotationGrabTime) * setRotationSpeed));
                    }

                    Hand tempHand = (PrimaryHand ?? SecondaryHand);

                    Vector3 tempRotationOffset = SecondaryHand && !PrimaryHand ? staticRotationOffset : RotationOffset;
                    Vector3 tempPositionOffset = SecondaryHand && !PrimaryHand ? staticPositionOffset : PositionOffset;

                    if (tempHand)
                    {
                        Rigidbody tempRb = rb;        //tempHand.OffsetRigidbody

                        Quaternion desiredRotation = SecondaryHand && PrimaryHand ? Quaternion.LookRotation(-(PrimaryHand.Offset.position - SecondaryHand.Offset.position).normalized, PrimaryHand.Offset.up)
                                                                                  * Quaternion.Inverse(Quaternion.Euler(twoHandRotationOffset))
                                                                                  : tempHand.Offset.rotation * (PrimaryHand ? Quaternion.Inverse(Quaternion.Euler(tempRotationOffset)) : Quaternion.Euler(tempRotationOffset));

                        tempRb.velocity = (tempHand.Offset.TransformPoint(tempPositionOffset) - transform.position) * Time.deltaTime * setPositionSpeed;

                        tempRb.angularVelocity = VectorHistoryAverage.GetAngularVelocityAngleAxis(transform.rotation, desiredRotation) * (SecondaryHand ? setTwoHandRotationSpeed : setRotationSpeed);
                    }

                    break;

                case PoseType.VelocityOneHand:

                    if (slot && !PrimaryHand)
                    {
                        gripOffset.localPosition = Vector3.Lerp(grabLocalPosition,
                                        Vector3.zero,
                                        setRotationCurve.Evaluate((Time.time - positionGrabTime) * setPositionSpeed));

                        gripOffset.localRotation = Quaternion.Lerp(grabLocalRotation,
                                                                    Quaternion.Inverse(Quaternion.Euler(RotationOffset)),
                                                                    setRotationCurve.Evaluate((Time.time - rotationGrabTime) * setRotationSpeed));
                    }

                    if (PrimaryHand)
                    {
                        Quaternion desiredRotation = PrimaryHand.Offset.rotation * (PrimaryHand ? Quaternion.Inverse(Quaternion.Euler(RotationOffset)) : Quaternion.identity);

                        rb.velocity = (PrimaryHand.Offset.TransformPoint(PositionOffset) - transform.position) * Time.deltaTime * setPositionSpeed;

                        rb.angularVelocity = VectorHistoryAverage.GetAngularVelocityAngleAxis(transform.rotation, desiredRotation) * (SecondaryHand ? setTwoHandRotationSpeed : setRotationSpeed);
                    }

                    break;

                case PoseType.VelocityStatic:

                    if (slot && !PrimaryHand)
                    {
                        gripOffset.localPosition = Vector3.Lerp(grabLocalPosition,
                                        Vector3.zero,
                                        setRotationCurve.Evaluate((Time.time - positionGrabTime) * setPositionSpeed));

                        gripOffset.localRotation = Quaternion.Lerp(grabLocalRotation,
                                                                    Quaternion.Inverse(Quaternion.Euler(RotationOffset)),
                                                                    setRotationCurve.Evaluate((Time.time - rotationGrabTime) * setRotationSpeed));
                    }

                    if (PrimaryHand)
                    {
                        Quaternion desiredRotation = SecondaryHand && PrimaryHand ? PrimaryHand.GlobalOffset.rotation * Quaternion.FromToRotation(initialTwoHandDirection, PrimaryHand.GlobalOffset.InverseTransformDirection(PrimaryHand.GlobalOffset.position - SecondaryHand.GlobalOffset.position))
                                                                                  : PrimaryHand.GlobalOffset.rotation;

                        Vector3 positionDelta = (PrimaryHand.GlobalOffset.TransformPoint(PositionOffset) - transform.position) * setPositionSpeed;

                        rb.velocity = (PrimaryHand.GlobalOffset.position - transform.position) * Time.deltaTime * setPositionSpeed;

                        rb.angularVelocity = VectorHistoryAverage.GetAngularVelocityAngleAxis(transform.rotation, desiredRotation) * (SecondaryHand ? setTwoHandRotationSpeed : setRotationSpeed);
                    }

                    break;
            }
        }

        public bool LocalInput(VrFpsInput.LocalInputEvent localInputEvent, SteamVR_Action_Boolean button) { return VrFpsInput.Input(localInputEvent, button, PrimaryHand); }

        public bool LocalInputDown(VrFpsInput.LocalInputEvent localInputEvent, SteamVR_Action_Boolean button) { return VrFpsInput.InputDown(localInputEvent, button, PrimaryHand); }

        public bool LocalInputUp(VrFpsInput.LocalInputEvent localInputEvent, SteamVR_Action_Boolean button) { return VrFpsInput.InputUp(localInputEvent, button, PrimaryHand); }

        public bool TouchPadInput(VrFpsInput.LocalInputEvent touchpadEvent, VrFpsInput.TouchPadDirection direction) { return VrFpsInput.TouchPadInput(touchpadEvent, direction, PrimaryHand); }

        public bool HasTag(string tag)
        {
            for (int i = 0; i < tags.Count; i++)
            {
                //Debug.Log(tags[i] + " : " + tag);
                if (tags[i] == tag)
                    return true;
            }

            return false;
        }
    }
}