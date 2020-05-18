using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    public class Hand : Slot 
    {
        protected Transform handSkeletonRoot; //손 뼈대

        public Transform HandSkeletonRoot
        {
            get
            {
                return handSkeletonRoot;
            }
        }

        [HideInInspector] public Transform audioSourceContainer;

        public OVRInput.Controller inputSource; //오큘러스 컨트롤러 입력

        protected Hand sibling;
        public Hand Sibling
        {
            get
            {
                return sibling;
            }
        }

        protected CharacterControllerMovement charController; //캐릭터 컨트롤러
        public CharacterControllerMovement CharController
        {
            get
            {
                return charController;
            }
        }

        protected Collider playerCollider; //캐릭터 콜리더
        public Collider PlayerCollider
        {
            get
            {
                return playerCollider;
            }
        }
        public Vector3 CharControllerVelocity //캐릭터 컨트롤 속도
        {
            get
            {
                return charController ? charController.velocityHistory._ReleaseVelocity : Vector3.zero;
            }
        }

        [SerializeField] protected MeshRenderer interactionSphere;
        [SerializeField] protected Material interactionSphereHighlight;
        [SerializeField] protected SphereCollider interactionTrigger;

        protected Material defaultMat;

        protected CapsuleGrab capsuleGrab; //그랩
        protected RayGrab rayGrab; //원거리 그랩

        [SerializeField] protected bool hapticFeedback = true;
        [SerializeField] protected bool tomatoePresence = true;
        [SerializeField] protected bool showController = true;
        [SerializeField] protected bool showInteractionSphere = true;
        [SerializeField] protected bool Highlight = true;
        [SerializeField] protected bool autoGrab = true;
        [SerializeField] protected bool autoDrop = true;
        [SerializeField] protected bool dropGrab = true;
        [SerializeField] protected bool pointGrab = true;

        //자동으로 잡고 놓고
        public bool AutoGrab
        {
            get
            {
                return autoGrab;
            }
        }
        public bool AutoDrop {
            get
            {
                return autoDrop;
            }
        }

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

        public bool IsInteracting
        {
            get
            {
                return interactingVolume;
            }
        }

        protected virtual void Start()
        {
            Hand[] hands = transform.parent.GetComponentsInChildren<Hand>();

            foreach (Hand hand in hands)
                if (hand != this)
                    sibling = hand;

            AudioSource tempAudioSource = GetComponentInChildren<AudioSource>();

            if (tempAudioSource)
            {
                audioSourceContainer = tempAudioSource.transform;
            }

            charController = transform.parent.GetComponentInChildren<CharacterControllerMovement>();

            gameObject.layer = LayerMask.NameToLayer("ItemDetection");
            rayGrab = GetComponent<RayGrab>();
            capsuleGrab = GetComponent<CapsuleGrab>();

            interactionTrigger = GetComponent<SphereCollider>();
            defaultMat = interactionSphere.sharedMaterial;

            offsetRigidbody = offset.GetComponent<Rigidbody>();
            playerCollider = charController.GetComponent<Collider>();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

        }

        protected virtual void Update() //업데이트
        {
            PoseStoredItem();

            if (interactingVolume)
            {
                if (OVRInput.GetDown(touchpadInput, inputSource)) //오큘러스 콘트럴러 입력 값
                {
                    if (OVRInput.Get(touchpadAxis, inputSource).y > 0)
                    {
                        PopFromStack();
                    }
                }

                else
                {
                    int collected = 0;

                    foreach (InteractionVolume iv in potentialInteractionVolumes) 
                    {
                        Item tempItem = iv.GetComponent<Item>();

                        if (tempItem)
                        {
                            if (!tempItem.Restrained)
                            {
                                if (tempItem.Stackable)
                                {
                                    if (AddToStack(tempItem))
                                    {
                                        collected++;
                                    }
                                }
                            }
                        }
                        if (collected >= addToStackAmount)
                        {
                            break;
                        }
                    }
                }

                if (rayGrab)//원거리 그렙
                {
                    rayGrab.SetRayGrabActive(false);
                }

                if (activeHighlight) //하이라이트 활성
                {
                    activeHighlight.ActiveHighlight = false;
                    activeHighlight = null;
                }
                return;
            }
            else 
            {

            }

            if (pointGrab) //그렙
            {
                if (rayGrab)
                {
                    if (potentialInteractionVolumes.Count == 0)
                    {
                        if (OVRInput.Get(touchpadInput, inputSource)) //입력키 값 누르면 그렙
                        {
                            InteractionVolume tempRayIV = rayGrab.RaycastGrab();

                            if (tempRayIV)
                            {
                                tempRayIV.AttemptInteraction(this);
                            }
                            return;
                        }
                    }

                    rayGrab.ClearRayIV();
                    rayGrab.SetRayGrabActive(false);
                }
            }

            if (dropGrab) //떨어트리기
            {
                if (capsuleGrab)
                {
                    if (potentialInteractionVolumes.Count == 0) 
                    {
                        InteractionVolume tempCapsuleIV = SendInteractionInput(capsuleGrab.CapsuleGrabInteractions());

                        if (tempCapsuleIV)
                        {
                            SetInteractionSphereMaterial(interactionSphereHighlight);
                        }
                        else
                        {
                            SetInteractionSphereMaterial(defaultMat);
                        }
                        return;
                    }
                }
            }

            potentialInteractionVolumes.RemoveAll(item => item == null);

            InteractionVolume tempIV = null;

            foreach (List<InteractionVolume> inputGroup in inputGroups.Values) //소리 입력
            {
                inputGroup.RemoveAll(item => item == null);
                InteractionVolume inputGroupTempIV = SendInteractionInput(inputGroup);
                if (!tempIV)
                {
                    tempIV = inputGroupTempIV;
                }
            }

            if (tempIV)
            {
                SetActiveHighlight(tempIV);
                SetInteractionSphereMaterial(interactionSphereHighlight);
            }
            else
            {
                SetInteractionSphereMaterial(defaultMat);
            }
        }

        protected override void SetVisibility(bool visible) //랜더링 세트
        {
            if (interactionSphere)
            {
                interactionSphere.enabled = visible && showInteractionSphere;
            }

            if (!tomatoePresence && showController)
            {
                return;
            }

            GameObject renderModel = null;

            if (renderModel)
            {
                MeshRenderer[] meshes = renderModel.GetComponentsInChildren<MeshRenderer>();

                if (meshes != null)
                {
                    foreach (MeshRenderer mesh in meshes)
                    {
                        mesh.enabled = visible;
                    }
                }
            }
        }

        InteractionVolume activeHighlight;

        void SetActiveHighlight(InteractionVolume newActiveHighlight) //잡을수 있는 물건이 있으면 잡을수있다고 표현되는걸 활성화
        {
            if (activeHighlight)
            {
                activeHighlight.ActiveHighlight = false;
            }

            activeHighlight = newActiveHighlight;

            if (Highlight)
            {
                activeHighlight.ActiveHighlight = true;
            }
        }

        void SetInteractionSphereMaterial(Material newMat)
        {
            if (interactionSphere.sharedMaterial != newMat)
            {
                interactionSphere.sharedMaterial = newMat;
                }
        }

        bool ValidIV(InteractionVolume iv) { if (iv == null) return false; return !(iv.restrained || (!iv.Overlap && iv.Hand)); }

        InteractionVolume SendInteractionInput(List<InteractionVolume> interactionsToSort) //상호 작용 입력
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

                    if (priority > highestPriority && ValidIV(currentIV))
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

        public virtual Slot GetClosestValidSlot(Item item) //가장 근처의 유효한 슬롯 찾기
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


        //오큘러스 버튼이랑 위치 가져옴
        [SerializeField] protected OVRInput.Button triggerInput;
        [SerializeField] protected OVRInput.Button touchpadInput;

        [SerializeField] protected OVRInput.Axis1D triggerAxis;
        [SerializeField] protected OVRInput.Axis2D touchpadAxis;

        public OVRInput.Button TriggerInput
        {
            get
            {
                return triggerInput;
            }
        }
        public OVRInput.Button TouchpadInput
        {
            get
            {
                return touchpadInput;
            }
        }
     
        public float TriggerRotation
        {
            get
            {
                return OVRInput.Get(triggerAxis, inputSource);
            }
        }

        public Vector2 TouchpadAxis
        {
            get
            {
                return OVRInput.Get(touchpadAxis, inputSource);
            }
        }

        Dictionary<OVRInput.Button, List<InteractionVolume>> inputGroups = new Dictionary<OVRInput.Button, List<InteractionVolume>>();

        void AddInteractables(Collider other) //상호작용 가능한것 추가
        {
            if(other.gameObject.tag == "Slot")
            {
                Slot[] tempSlots = other.GetComponents<Slot>();

                foreach (Slot tempSlot in tempSlots)
                {
                    if (tempSlot)
                        if (!potentialSlots.Contains(tempSlot))
                            potentialSlots.Add(tempSlot);
                }
            }

            if (other.gameObject.tag == "Interactable") //다른 게임 오브젝트들중 태그로 "Interactable"
            {
                InteractionVolume[] tempInteractionVolumes = other.GetComponents<InteractionVolume>();

                foreach (InteractionVolume tempInteractionVolume in tempInteractionVolumes)
                    if (tempInteractionVolume)
                        if (!potentialInteractionVolumes.Contains(tempInteractionVolume))
                        {
                            if (tempInteractionVolume._OnEnterOverlap != null)
                                tempInteractionVolume._OnEnterOverlap(this);

                            potentialInteractionVolumes.Add(tempInteractionVolume);

                            OVRInput.Button tempInput = tempInteractionVolume.StartInputID;

                            if (tempInput != null)
                            {
                                if (!inputGroups.ContainsKey(tempInput))
                                    inputGroups.Add(tempInput, new List<InteractionVolume>());

                                inputGroups[tempInput].Add(tempInteractionVolume);
                            }
                        }
            }
        }

        void RemoveInteractables(Collider other) //상호가능한것 제거
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
                        tempInteractionVolume.ActiveHighlight = false;

                        if (tempInteractionVolume._OnExitOverlap != null)
                            tempInteractionVolume._OnExitOverlap(this);

                        potentialInteractionVolumes.Remove(tempInteractionVolume);

                        OVRInput.Button tempInput = tempInteractionVolume.StartInputID;

                        if(tempInput != null)
                            if(inputGroups.ContainsKey(tempInput))
                                inputGroups[tempInput].Remove(tempInteractionVolume);
                    }
            }
        }

        protected Slot highlightSlot;

        protected void OnTriggerStay(Collider other) //온트리거 관련
        {
            bool hasStorableItem = HasItem ? StoredItem ? StoredItem.Storable : false : false;

            if (hasStorableItem)
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
            {
                return;
            }
        }

        protected virtual void PopFromStack() //스택에서 꺼냄
        {
            if (!storedItem)
            {
                return;
            }

            if (!storedItem.Stackable)
            {
                return;
            }

            Item tempStoredItem = storedItem;

            Slot tempSlot = ClosestStackableSlot(tempStoredItem);

            tempStoredItem.dropStack = false;
            tempStoredItem.DetachWithOutStoring();
            storedItem.DetachSlot();
            tempStoredItem.dropStack = true;

            if (tempSlot)
            {
                tempSlot.AddToStack(tempStoredItem);
            }
            else if (sibling.StackableStoredItem)
            {
                if (Vector3.Distance(transform.position, sibling.transform.position) <= 0.2f)
                {
                    sibling.AddToStack(tempStoredItem);
                }
            }

            GrabFromStack();
        }

        public virtual void GrabFromStack() //스택에서 그렙
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

        Slot ClosestStackableSlot(Item tempItem) //최근접 스택이 가능한 슬롯
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

       
        //상호작용 크기 조절
        public void IncreaseInteractionSphereSize(float increment)
        {
            ResizeInteractionSphere((interactionTrigger.radius) + increment);
        }

        public void DecreaseInteractionSphereSize(float decrement)
        {
            ResizeInteractionSphere((interactionTrigger.radius) - decrement);
        }

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