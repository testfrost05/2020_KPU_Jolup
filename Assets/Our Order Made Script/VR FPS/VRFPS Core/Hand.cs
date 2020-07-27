using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Photon.Pun;

namespace VrFps
{
    [PunRPC]
    public class Hand : Slot
    {
        protected Transform handSkeletonRoot;

        public Transform HandSkeletonRoot { get { return handSkeletonRoot; } }

        [SerializeField] protected SteamVR_Behaviour_Skeleton handSkeleton;

        public SteamVR_Behaviour_Skeleton HandSkeleton { get { return handSkeleton; } }

        [HideInInspector] public Transform audioSourceContainer;

        public SteamVR_Input_Sources inputSource;

        protected Hand sibling;
        public Hand Sibling { get { return sibling; } }

        protected CharacterControllerMovement charController;
        public CharacterControllerMovement CharController { get { return charController; } }
        protected Collider playerCollider;
        public Collider PlayerCollider { get { return playerCollider; } }
        public Vector3 CharControllerVelocity { get { return charController ? charController.velocityHistory._ReleaseVelocity : Vector3.zero; } }

        [SerializeField] protected MeshRenderer interactionSphere;
        [SerializeField] protected Material interactionSphereHighlight;
        [SerializeField] protected SphereCollider interactionTrigger;

        protected Material defaultMat;

        protected CapsuleGrab capsuleGrab;
        protected RayGrab rayGrab;

        [SerializeField] protected bool hapticFeedback = true;
        [SerializeField] protected bool tomatoePresence = true;
        [SerializeField] protected bool showController = true;
        [SerializeField] protected bool showInteractionSphere = true;
        [SerializeField] protected bool Highlight = true;
        [SerializeField] protected bool autoGrab = true;
        [SerializeField] protected bool autoDrop = true;
        [SerializeField] protected bool dropGrab = true;
        [SerializeField] protected bool pointGrab = true;

        public bool AutoGrab { get { return autoGrab; } }
        public bool AutoDrop { get { return autoDrop; } }

        [SerializeField] [ReadOnly] protected InteractionVolume interactingVolume;
        public InteractionVolume InteractingVolume
        {
            get { return interactingVolume; }

            set
            {
                if (tomatoePresence)
                    SetVisibility(!value);

                interactingVolume = value;

                HapticPulse(0.1f, 0.5f);
            }
        }

        protected FixedJoint fixedJoint;

        public bool IsInteracting { get { return interactingVolume; } }

        protected virtual void Start()
        {
            Hand[] hands = transform.parent.GetComponentsInChildren<Hand>();

            foreach (Hand hand in hands)
                if (hand != this)
                    sibling = hand;

            AudioSource tempAudioSource = GetComponentInChildren<AudioSource>();

            if (tempAudioSource)
                audioSourceContainer = tempAudioSource.transform;

            charController = transform.parent.GetComponentInChildren<CharacterControllerMovement>();

            if (!handSkeleton) handSkeleton = GetComponentInChildren<SteamVR_Behaviour_Skeleton>();
            handSkeletonRoot = handSkeleton ? handSkeleton.transform : null;

            gameObject.layer = LayerMask.NameToLayer("ItemDetection");
            rayGrab = GetComponent<RayGrab>();
            capsuleGrab = GetComponent<CapsuleGrab>();

            interactionTrigger = GetComponent<SphereCollider>();
            defaultMat = interactionSphere.sharedMaterial;

            offsetRigidbody = offset.GetComponent<Rigidbody>();
            offsetRigidbody.maxAngularVelocity = Mathf.Infinity;
            playerCollider = charController.GetComponent<Collider>();

            if (handSkeleton)
                handSkeleton.BlendToPoser(handSkeleton.fallbackPoser);
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
        }

        void ManipulateStack()
        {
            if (VrFpsInput.TouchPadInput(null, VrFpsInput.TouchPadDirection.dontMatter, this))
                if (touchpadAxis.axis.y > 0)
                    PopFromStack();
                else
                {
                    int collected = 0;

                    foreach (InteractionVolume iv in potentialInteractionVolumes)
                    {
                        Item tempItem = iv.GetComponent<Item>();

                        if (tempItem)
                            if (!tempItem.Restrained)
                                if (tempItem.Stackable)
                                    if (AddToStack(tempItem))
                                        collected++;

                        if (collected >= addToStackAmount)
                            break;
                    }
                }
        }

        protected virtual void Update()
        {
            PoseStoredItem();

            if (interactingVolume)
            {
                ManipulateStack();

                if (rayGrab)
                    rayGrab.SetRayGrabActive(false);

                if (activeHighlight)
                {
                    activeHighlight.HighlightIsActive = false;
                    activeHighlight = null;
                }
                return;
            }
            else
            {
                if (HandSkeletonRoot)
                    if (storedItem)
                    {
                        if (Vector3.Distance(storedItem.PrimaryHand.Offset.localPosition, Vector3.zero) < 0.1f)
                        {
                            HandSkeletonRoot.transform.localPosition = Vector3.Lerp(HandSkeletonRoot.transform.localPosition, Vector3.zero, Time.deltaTime / 0.2f);
                            HandSkeletonRoot.transform.localRotation = Quaternion.Lerp(HandSkeletonRoot.transform.localRotation, Quaternion.identity, Time.deltaTime / 0.2f);
                        }
                    }
                    else
                    {
                        HandSkeletonRoot.transform.localPosition = Vector3.Lerp(HandSkeletonRoot.transform.localPosition, Vector3.zero, Time.deltaTime / 0.2f);
                        HandSkeletonRoot.transform.localRotation = Quaternion.Lerp(HandSkeletonRoot.transform.localRotation, Quaternion.identity, Time.deltaTime / 0.2f);
                    }
            }

            if (pointGrab)
                if (rayGrab)
                {
                    if (potentialInteractionVolumes.Count == 0)
                    {
                        if (VrFpsInput.Input(touchpadInput, this))
                        {
                            InteractionVolume tempRayIV = rayGrab.RaycastGrab();

                            if (tempRayIV)
                                tempRayIV.AttemptInteraction(this);

                            return;
                        }
                    }

                    rayGrab.ClearRayIV();
                    rayGrab.SetRayGrabActive(false);
                }

            if (dropGrab)
                if (capsuleGrab)
                {
                    if (potentialInteractionVolumes.Count == 0)
                    {
                        InteractionVolume tempCapsuleIV = SendInteractionInput(capsuleGrab.CapsuleGrabInteractions());

                        if (tempCapsuleIV)
                            SetInteractionSphereMaterial(interactionSphereHighlight);
                        else
                            SetInteractionSphereMaterial(defaultMat);

                        return;
                    }
                }

            potentialInteractionVolumes.RemoveAll(item => item == null);

            InteractionVolume tempIV = null;

            foreach (List<InteractionVolume> inputGroup in inputGroups.Values)
            {
                inputGroup.RemoveAll(item => item == null);
                InteractionVolume inputGroupTempIV = SendInteractionInput(inputGroup);

                if (!tempIV) tempIV = inputGroupTempIV;
            }

            if (tempIV)
            {
                SetActiveHighlight(tempIV);
                SetInteractionSphereMaterial(interactionSphereHighlight);
            }
            else SetInteractionSphereMaterial(defaultMat);
        }

        protected override void SetVisibility(bool visible)
        {
            if (interactionSphere)
                interactionSphere.enabled = visible && showInteractionSphere;

            if (!tomatoePresence && showController)
                return;

            SteamVR_RenderModel renderModel = GetComponentInChildren<SteamVR_RenderModel>();

            if (renderModel)
            {
                MeshRenderer[] meshes = renderModel.GetComponentsInChildren<MeshRenderer>();

                if (meshes != null)
                    foreach (MeshRenderer mesh in meshes)
                        mesh.enabled = visible;
            }
        }

        InteractionVolume activeHighlight;

        void SetActiveHighlight(InteractionVolume newActiveHighlight)
        {
            if (activeHighlight)
                activeHighlight.HighlightIsActive = false;

            activeHighlight = newActiveHighlight;

            if (Highlight)
                activeHighlight.HighlightIsActive = true;
        }

        void SetInteractionSphereMaterial(Material newMat)
        {
            if (interactionSphere.sharedMaterial != newMat)
                interactionSphere.sharedMaterial = newMat;
        }

        bool ValidIV(InteractionVolume iv) { if (iv == null) return false; return !(iv.restrained || (!iv.Overlap && iv.Hand)); }

        InteractionVolume SendInteractionInput(List<InteractionVolume> interactionsToSort)
        {
            InteractionVolume[] interactions = new InteractionVolume[interactionsToSort.Count];
            interactionsToSort.CopyTo(interactions);

            return SendInteractionInput(interactions);
        }

      
        InteractionVolume SendInteractionInput(InteractionVolume[] interactions)
        {
            if (interactions.Length == 0)
                return null;

            InteractionVolume prioritizedIV = null;
            float highestPriority = 0;

            for (int b = 0; b < interactions.Length; b++)
            {
                InteractionVolume currentIV = interactions[b];

                if (currentIV)
                {
                    float distance = Vector3.Distance(currentIV.transform.position, interactionSphere.transform.position);
                    float priority = currentIV.Priority / distance;

                    if ((priority > highestPriority && ValidIV(currentIV)))
                    {
                        prioritizedIV = currentIV;
                        highestPriority = priority;
                    }
                }
            }

            if (prioritizedIV)
                prioritizedIV.AttemptInteraction(this);

            return prioritizedIV;
        }

        public virtual Slot GetClosestValidSlot(Item item)
        {
            float closestDistance = Mathf.Infinity;
            Slot closestValidSlot = null;

            for (int i = 0; i < potentialSlots.Count; i++)
            {
                Slot potentialSlot = potentialSlots[i];

                if (!potentialSlot.ValidItem(item))
                    continue;

                float distance = Vector3.Distance(Offset.position, potentialSlot.transform.position);

                if (closestValidSlot)
                {
                    if (!closestValidSlot.HasItem && potentialSlot.HasItem) 
                        continue;

                    if (closestValidSlot.HasItem && !potentialSlot.HasItem) 
                    {                                                      
                        closestDistance = distance;
                        closestValidSlot = potentialSlot;
                        continue;
                    }
                }

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestValidSlot = potentialSlot;
                }
            }

            return closestValidSlot;
        }

        [SerializeField] protected List<Slot> potentialSlots = new List<Slot>();

        [SerializeField] protected List<InteractionVolume> potentialInteractionVolumes = new List<InteractionVolume>();

        [SerializeField] protected SteamVR_Action_Boolean triggerInput;
        [SerializeField] protected SteamVR_Action_Boolean touchpadInput;

        [SerializeField] protected SteamVR_Action_Single triggerAxis;
        [SerializeField] protected SteamVR_Action_Vector2 touchpadAxis;

        public SteamVR_Action_Boolean TriggerInput { get { return triggerInput; } }
        public SteamVR_Action_Boolean TouchpadInput { get { return touchpadInput; } }

        public float TriggerRotation { get { return triggerAxis.GetAxis(inputSource); } }
        public Vector2 TouchpadAxis { get { return touchpadAxis.GetAxis(inputSource); } }

        Dictionary<SteamVR_Action_Boolean, List<InteractionVolume>> inputGroups = new Dictionary<SteamVR_Action_Boolean, List<InteractionVolume>>();

        void AddInteractables(Collider other)
        {
            if (other.gameObject.tag == "Slot")
            {
                Slot[] tempSlots = other.GetComponents<Slot>();

                foreach (Slot tempSlot in tempSlots)
                {
                    if (tempSlot)
                        if (!potentialSlots.Contains(tempSlot))
                            potentialSlots.Add(tempSlot);
                }
            }

            if (other.gameObject.tag == "Interactable")
            {
                InteractionVolume[] tempInteractionVolumes = other.GetComponents<InteractionVolume>();

                foreach (InteractionVolume tempInteractionVolume in tempInteractionVolumes)
                    if (tempInteractionVolume)
                        if (!potentialInteractionVolumes.Contains(tempInteractionVolume))
                        {
                            if (tempInteractionVolume._OnEnterOverlap != null)
                                tempInteractionVolume._OnEnterOverlap(this);

                            potentialInteractionVolumes.Add(tempInteractionVolume);

                            SteamVR_Action_Boolean tempInput = tempInteractionVolume.StartInputID;

                            if (tempInput != null)
                            {
                                if (!inputGroups.ContainsKey(tempInput))
                                    inputGroups.Add(tempInput, new List<InteractionVolume>());

                                inputGroups[tempInput].Add(tempInteractionVolume);
                            }
                        }
            }
        }

        void RemoveInteractables(Collider other)
        {
            if (other.gameObject.tag == "Slot")
            {
                Slot[] tempSlots = other.GetComponents<Slot>();

                foreach (Slot tempSlot in tempSlots)
                {
                    if (tempSlot)
                        if (potentialSlots.Contains(tempSlot))
                            potentialSlots.Remove(tempSlot);
                }
            }

            if (other.gameObject.tag == "Interactable")
            {
                InteractionVolume[] tempInteractionVolumes = other.GetComponents<InteractionVolume>();

                foreach (InteractionVolume tempInteractionVolume in tempInteractionVolumes)
                    if (potentialInteractionVolumes.Contains(tempInteractionVolume))
                    {
                        tempInteractionVolume.HighlightIsActive = false;

                        if (tempInteractionVolume._OnExitOverlap != null)
                            tempInteractionVolume._OnExitOverlap(this);

                        potentialInteractionVolumes.Remove(tempInteractionVolume);

                        SteamVR_Action_Boolean tempInput = tempInteractionVolume.StartInputID;

                        if (tempInput != null)
                            if (inputGroups.ContainsKey(tempInput))
                                inputGroups[tempInput].Remove(tempInteractionVolume);
                    }
            }
        }

        protected Slot highlightSlot;

        protected void OnTriggerStay(Collider other)
        {
            if (HasItem)
            {
                Slot tempHighlightSlot = GetClosestValidSlot(storedItem);

                if (tempHighlightSlot)
                {
                    if (highlightSlot)
                    {
                        if (highlightSlot != tempHighlightSlot)
                        {
                            highlightSlot._Unhighlight();
                        }
                    }

                    tempHighlightSlot._Highlight();
                    highlightSlot = tempHighlightSlot;
                }
                else
                {
                    if (highlightSlot)
                        highlightSlot._Unhighlight();
                }
            }
            else
            {
                if (highlightSlot)
                    highlightSlot._Unhighlight();
            }
        }

        protected void OnTriggerEnter(Collider other)
        {
            AddInteractables(other);
        }

        protected void OnTriggerExit(Collider other)
        {
            RemoveInteractables(other);

            if (highlightSlot)
                if (other.gameObject == highlightSlot.gameObject)
                {
                    highlightSlot._Unhighlight();
                    highlightSlot = null;
                }
        }

        IEnumerator hapticPulse;

        public void HapticPulse(float length, float strength)
        {
            if (!hapticFeedback)
                return;

            if (hapticAction != null)
                hapticAction.Execute(0, length, 150, strength, inputSource);
        }

        [SerializeField] protected SteamVR_Action_Vibration hapticAction;

        protected virtual void PopFromStack()
        {
            if (!storedItem)
                return;

            if (!storedItem.Stackable)
                return;

            Item tempStoredItem = storedItem;

            Slot tempSlot = ClosestStackableSlot(tempStoredItem);

            tempStoredItem.dropStack = false;
            tempStoredItem.DetachWithOutStoring();
            storedItem.DetachSlot();
            tempStoredItem.dropStack = true;

            if (tempSlot)
                tempSlot.AddToStack(tempStoredItem);
            else if (sibling.StackableStoredItem)
                if (Vector3.Distance(transform.position, sibling.transform.position) <= 0.2f)
                    sibling.AddToStack(tempStoredItem);

            GrabFromStack();
        }

 
        public virtual void GrabFromStack()
        {
            if (!StackIsEmpty)
            {
                Item tempStackedItem = stackedItems[stackedItems.Count - 1];

                tempStackedItem.Restrained = false;
                tempStackedItem.transform.SetParent(null);
                tempStackedItem.Attach(this);
                stackedItems.RemoveAt(stackedItems.Count - 1);
            }
        }


        Slot ClosestStackableSlot(Item tempItem)
        {
            Slot closestSlot = null;
            float closestDistance = Mathf.Infinity;

            potentialSlots.RemoveAll(item => item == null);

            for (int i = 0; i < potentialSlots.Count; i++)
            {

                Slot tempSlot = potentialSlots[i];

                if (!tempSlot.ValidItem(tempItem))
                    continue;

                if (tempSlot.HasItem)
                {
                    if (!tempSlot.StackableStoredItem)
                        continue;

                    if (tempSlot.StackIsFull)
                        continue;
                }

                float distance = Vector3.Distance(offset.position, tempSlot.transform.position);

                if (closestSlot)
                {
                    if (closestSlot.HasItem && !tempSlot.HasItem) 
                        continue;

                    if (!closestSlot.HasItem && tempSlot.HasItem)
                    {
                        closestDistance = distance;
                        closestSlot = tempSlot;
                        continue;
                    }
                }

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestSlot = tempSlot;
                }
            }

            return closestSlot;
        }

        [SerializeField] int addToStackAmount = 1;


        public void IncreaseInteractionSphereSize(float increment) { ResizeInteractionSphere((interactionTrigger.radius) + increment); }

        public void DecreaseInteractionSphereSize(float decrement) { ResizeInteractionSphere((interactionTrigger.radius) - decrement); }

        public void ResizeInteractionSphere(float newSize)
        {
            float newSphereScale = newSize * 2;
            interactionSphere.transform.localScale = new Vector3(newSphereScale, newSphereScale, newSphereScale);
            interactionTrigger.radius = newSize;
        }

        public void RepositionInteractionSphere(Vector3 newOffset)
        {
            interactionSphere.transform.localPosition = newOffset;
            interactionTrigger.center = newOffset;
        }

        public void ToggleInteractionSpherePosition()
        {
            RepositionInteractionSphere(interactionSphere.transform.localPosition.z == -0.05f ? Vector3.zero : new Vector3(0, 0, -0.05f));
        }

        public virtual void ToggleHighlight() { Highlight = !Highlight; }

        public virtual void ToggleInteractionSphereVisibility() { showInteractionSphere = !showInteractionSphere; }

        public virtual void ToggleControllerVisibility() { showController = !showController; }

        public virtual void ToggleHideOnInteract() { tomatoePresence = !tomatoePresence; }

        public virtual void ToggleHaptics() { hapticFeedback = !hapticFeedback; }

        public virtual void ToggleAutoGrab() { autoGrab = !autoGrab; }

        public virtual void ToggleAutoDrop() { autoDrop = !autoDrop; }

        public virtual void ToggleDropGrab() { dropGrab = !dropGrab; }

        public virtual void TogglePointGrab() { pointGrab = !pointGrab; }

        public virtual void CycleDeviceIndex()
        {
            SteamVR_TrackedObject trackedObj = GetComponent<SteamVR_TrackedObject>();

            int index = (int)trackedObj.index;

            if (index == 4)
                index = 1;
            else
                index++;

            trackedObj.SetDeviceIndex(index);
        }

        void PoseStoredItem()
        {
            if (storedItem)
            {
                if (poseItem)
                    storedItem.Pose();
            }
        }
    }
}