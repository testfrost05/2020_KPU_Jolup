using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Valve.VR;

namespace VrFps
{
    public class InteractionVolume : MonoBehaviour
    {
        [ReadOnly] [SerializeField] protected Hand hand;
        public Hand Hand { get { return hand; } }

        [SerializeField] GameObject highlight;

        public bool HighlightIsActive
        {
            set
            {
                if (highlight == null)
                    return;

                if (highlight.activeSelf != value)
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

        [Tooltip("As you increase the priority the required distance to interact with this volume is shortened while other volumes are also inside the hands interaction sphere")]
        [SerializeField] protected int priority = 1;
        [Tooltip("If auto select movement hand is enabled the hand holding the item with the highest movement priority is set to move the character controller")]
        [SerializeField] protected int movementPriority;

        public int Priority { get { return priority; } }
        public int MovementPriority { get { return movementPriority; } }

        public delegate void OverlapEvent(Hand hand);

        public OverlapEvent _OnEnterOverlap;
        public OverlapEvent _OnOverlapInteraction;
        public OverlapEvent _OnExitOverlap;

        public delegate void Interaction();

        public Interaction _StartInteraction;
        public Interaction _EndInteraction;

        [Tooltip("If overlap is set to true another hand can override the current interacting hand")]
        [SerializeField] protected bool overlap;
        public bool Overlap { get { return overlap; } }


        [Tooltip("This volume cannot be interacted with while restrained is true")]
        public bool restrained;

        public enum InteractionMode
        {
            Press,
            PressDown,
            PressUp,
            SecondPressUp,
            None
        }

        [ReadOnly] [SerializeField] protected int releaseCount;

        protected enum Input
        {
            Grip,
            Trigger,
            Touchpad,
            ApplicationMenu
        }

        [SerializeField] protected SteamVR_Action_Boolean startInteractingInputID;
        [SerializeField] protected SteamVR_Action_Boolean stopInteractingInputID;

        public SteamVR_Action_Boolean StartInputID { get { return startInteractingInputID; } }
        public SteamVR_Action_Boolean EndInputID { get { return stopInteractingInputID; } }

        [SerializeField] protected InteractionMode startInteractionMode = InteractionMode.PressDown;
        [SerializeField] protected InteractionMode endInteractionMode = InteractionMode.PressUp;

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

        [SerializeField] public SteamVR_Skeleton_Poser RightHandPoser;
        [SerializeField] public SteamVR_Skeleton_Poser LeftHandPoser;

        [SerializeField] public Transform handRoot;

        [SerializeField] public Vector3 rightPosePositionOffset;
        [SerializeField] public Vector3 rightPoseRotationOffset;

        [SerializeField] public Vector3 leftPosePositionOffset;
        [SerializeField] public Vector3 leftPoseRotationOffset;

        [SerializeField] protected float poseTime = 0.2f;
        public float PoseTime { get { return poseTime; } set { poseTime = value; } }

        protected virtual void Start()
        {
            SteamVR_Skeleton_Poser[] tempHandPosers = GetComponents<SteamVR_Skeleton_Poser>();

            if (tempHandPosers.Length >= 2)
            {
                if (!RightHandPoser) RightHandPoser = tempHandPosers[0];
                if (!LeftHandPoser) LeftHandPoser = tempHandPosers[1];
            }

            if (handRoot == null) handRoot = transform;

            gameObject.tag = "Interactable";

            CreateHighlight();
        }

        public void CreateHighlight()
        {
            if (highlight) return;

            MeshFilter currentMeshFilter = GetComponent<MeshFilter>();

            bool child = false;

            if (!currentMeshFilter)
            {
                currentMeshFilter = GetComponentInChildren<MeshFilter>();
                child = true;
            }

            if (!currentMeshFilter) return;

            if (child && currentMeshFilter.GetComponent<InteractionVolume>()) return;

            highlight = new GameObject(name + " Highlight");
            MeshRenderer newRenderer = highlight.AddComponent<MeshRenderer>();

#if UNITY_EDITOR
            newRenderer.material = (Material)AssetDatabase.LoadAssetAtPath("Assets/SteamVR/InteractionSystem/Core/Materials/HoverHighlight.mat", typeof(Material));
#endif

            MeshFilter newFilter = highlight.AddComponent<MeshFilter>();
            newFilter.mesh = currentMeshFilter.sharedMesh;

            newFilter.transform.SetParent(currentMeshFilter.transform, true);
            highlight.transform.localScale = Vector3.one;
            newFilter.transform.localPosition = Vector3.zero;
            newFilter.transform.localEulerAngles = Vector3.zero;

            highlight.SetActive(false);
        }

        public float RightPoseValue;
        public float LeftPoseValue;

        public int RightPolarity;
        public int LeftPolarity;

        protected virtual void Update()
        {
            EndInput();
            AnimateHandPose();
            AnimateHandToInteraction();
        }

        protected void AnimateHandPose()
        {
            if (!RightHandPoser || !LeftHandPoser) return;

            RightHandPoser.SetBlendingBehaviourValue("Interact", RightPoseValue);
            LeftHandPoser.SetBlendingBehaviourValue("Interact", LeftPoseValue);

            RightPoseValue = Mathf.Clamp01(RightPoseValue + Time.deltaTime / poseTime * RightPolarity);
            LeftPoseValue = Mathf.Clamp01(LeftPoseValue + Time.deltaTime / poseTime * LeftPolarity);
        }

        protected void AnimateHandToInteraction()
        {
            if (!RightHandPoser || !LeftHandPoser) return;

            if (!hand) return;

            if (!hand.HandSkeletonRoot) return;

            if (hand.StoredItem)
                if (Vector3.Distance(hand.Offset.localPosition, Vector3.zero) > 0.1f) return;

            if (hand.HandSkeletonRoot.parent != handRoot)
                hand.HandSkeletonRoot.SetParent(handRoot);

            bool leftHand = hand.inputSource == SteamVR_Input_Sources.LeftHand;

            Vector3 tempPosePositionOffset = leftHand ? leftPosePositionOffset : rightPosePositionOffset;
            Vector3 tempPoseRotationOffset = leftHand ? leftPoseRotationOffset : rightPoseRotationOffset;

            hand.HandSkeletonRoot.transform.localPosition = Vector3.Lerp(hand.HandSkeletonRoot.transform.localPosition, tempPosePositionOffset, Time.deltaTime / poseTime * 3);
            hand.HandSkeletonRoot.transform.localRotation = Quaternion.Lerp(hand.HandSkeletonRoot.transform.localRotation, Quaternion.Euler(tempPoseRotationOffset), Time.deltaTime / poseTime * 3);

            if (!handRoot) return;
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
                    break;

                case EarlyExitCondition.onExitTrigger:
                    StopInteraction();
                    break;

                case EarlyExitCondition.onExitSphereBounds:

                    if (Vector3.Distance(transform.position, hand.transform.position) > SphereBoundsRadius)
                        StopInteraction();

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
                //If there is a current interactions, stop it
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

            if (DetectInput(hand, InteractionMode.PressUp, stopInteractingInputID))
                releaseCount++;

            if (DetectInput(hand, endInteractionMode, stopInteractingInputID))
                StopInteraction();
        }

        bool DetectInput(Hand hand, InteractionMode input, SteamVR_Action_Boolean id)
        {
            if (!hand)
                return false;

            if (id == null)
                return false;

            switch (input)
            {
                case InteractionMode.Press:
                    return VrFpsInput.Input(id, hand);

                case InteractionMode.PressDown:
                    return VrFpsInput.InputDown(id, hand);

                case InteractionMode.PressUp:
                    return VrFpsInput.InputUp(id, hand);

                case InteractionMode.SecondPressUp:
                    return VrFpsInput.InputUp(id, hand) && releaseCount == 2;

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

        protected void StartInteraction(Hand newHand)
        {
            //Debug.Log("Start Interaction" + name + " : " + hand.name);

            handInTrigger = true;

            hand = newHand;

            hand.InteractingVolume = this;

            if (highlight)
                highlight.SetActive(false);

            if (_StartInteraction != null)
                _StartInteraction();

            if (!hand) return;

            bool leftHand = hand.inputSource == SteamVR_Input_Sources.LeftHand;

            SteamVR_Skeleton_Poser tempPoser = leftHand ? LeftHandPoser : RightHandPoser;

            if (leftHand)
            {
                LeftPoseValue = 0;
                LeftPolarity = 1;
            }
            else
            {
                RightPoseValue = 0;
                RightPolarity = 1;
            }

            if (tempPoser && hand.HandSkeleton)
            {
                tempPoser.SetBlendingBehaviourValue("Interact", leftHand ? LeftPoseValue : RightPoseValue);
                StartCoroutine(DelayPose(hand.HandSkeleton, tempPoser));
            }
        }

        SteamVR_Skeleton_Pose newPose;

        public void StopInteraction()
        {
            //Debug.Log("End Interaction " + name + " : " + hand.name);	

            handInTrigger = false;

            if (_EndInteraction != null)
                _EndInteraction();

            releaseCount = 0;

            if (!hand)
                return;

            if (hand.InteractingVolume)
                if (hand.InteractingVolume == this)
                    hand.InteractingVolume = null;

            //newPose = ScriptableObject.CreateInstance<SteamVR_Skeleton_Pose>();
            //newPose.name = "Get Rekt Son";
            //newPose.leftHand.bonePositions = hand.HandSkeleton.GetBonePositions();
            //newPose.leftHand.boneRotations = hand.HandSkeleton.GetBoneRotations();
            //newPose.rightHand.bonePositions = hand.HandSkeleton.GetBonePositions();
            //newPose.rightHand.boneRotations = hand.HandSkeleton.GetBoneRotations();
            //hand.HandPoser.skeletonMainPose = newPose;
            //hand.HandPoser.skeletonMainPose.leftHand.bonePositions = StartInteractingHandPoser.skeletonAdditionalPoses[0].leftHand.bonePositions;

            bool leftHand = hand.inputSource == SteamVR_Input_Sources.LeftHand;

            SteamVR_Skeleton_Poser tempPoser = leftHand ? LeftHandPoser : RightHandPoser;

            if (leftHand)
            {
                LeftPoseValue = 1;
                LeftPolarity = -1;
            }
            else
            {
                RightPoseValue = 1;
                RightPolarity = -1;
            }

            if (tempPoser && hand.HandSkeleton)
            {
                tempPoser.SetBlendingBehaviourValue("Interact", leftHand ? LeftPoseValue : RightPoseValue);
                StartCoroutine(DelayPose(hand.HandSkeleton, tempPoser));
            }

            if (hand.HandSkeletonRoot)
                hand.HandSkeletonRoot.SetParent(hand.transform);

            hand = null;
        }

        IEnumerator DelayPose(SteamVR_Behaviour_Skeleton skeleton, SteamVR_Skeleton_Poser poser)
        {
            yield return new WaitForEndOfFrame();

            skeleton.BlendToPoser(poser);
        }

        void OnTriggerExit(Collider other)
        {
            if (hand)
                if (other.gameObject == hand.gameObject)
                    handInTrigger = false;
        }
    }
}