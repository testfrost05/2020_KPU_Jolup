using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Photon.Pun;
namespace VrFps
{
    public class CharacterControllerMovement : MonoBehaviourPunCallbacks
    {
        [SerializeField] protected SteamVR_Action_Vector2 movementAxis;

        public bool linearMovement = false;

        public Hand currentHand;
        public Hand defaultHand;

        [SerializeField]
        protected CharacterController characterController;

        public CharacterController CharacterController { get { return characterController; } }

        public float maxHeight = 2;

        public float movementSpeed;
        public float fallingMovementSpeedScale;
        public float sprintScale;

        public float gravity = -1;

        public Transform rig;
        public Transform body;
        public Transform head;

        Vector3 prevPos;

        public LayerMask door;

        public float sprintingDeadzone = 0.1f;

        public VectorHistoryAverage velocityHistory = new VectorHistoryAverage();

        void Start()
        {




            gameObject.tag = "Player";
            ; gameObject.layer = LayerMask.NameToLayer("Player");
            velocityHistory.InitializeHistory();
            prevPos = transform.position;
        }

        bool sprinting;

        float fallingTime;

        void LateUpdate()
        {
       
          
            UpdatePosition();
        }

        void FixedUpdate()
        {
           
            velocityHistory.VelocityStep(transform);
        }

        float updateTime;

        float headYRotation;

        public bool autoSetMovementHand;

        public bool climbing;

        public float climbingHeight = 0;
        public Vector3 climbingPos;
        public Vector3 climbablePos;
        public Transform climbable;
        public Hand climbingHand;

        float climbingTime;

        public Hand ClimbingHand
        {
            set
            {
                climbing = value;
                climbingHand = value;
                climbingPos = Vector3.zero;

                if (climbingHand == null)
                {
                    RaycastHit hit;

                    if (Physics.Raycast(head.transform.position, Vector3.down, out hit, head.transform.localPosition.y))
                    {
                        characterController.height = hit.distance;
                        characterController.center = new Vector3(0, -hit.distance / 2, 0);
                    }
                    else
                    {
                        characterController.center = new Vector3(0, -head.localPosition.y / 2, 0);
                        characterController.height = head.localPosition.y;
                    }
                }
            }
        }

        public virtual void SetHand(Hand hand)
        {
            /*if (!autoSetMovementHand)
			    return;

		    if (!defaultHand.InteractingVolume)
		    {
			    currentHand = defaultHand;
			    return;
		    }

		    if (!hand.InteractingVolume && hand.Sibling.InteractingVolume)
		    {
			    currentHand = hand;
			    return;
		    }
		    else if (hand.InteractingVolume && !hand.Sibling.InteractingVolume)
		    {
			    currentHand = hand.Sibling;
			    return;
		    }
		    else if (hand.InteractingVolume && hand.Sibling.InteractingVolume)
		    {
			    if (hand.InteractingVolume == hand.Sibling.InteractingVolume)
			    {
				    currentHand = hand.StoredItem.SecondaryHand;
				    return;
			    }

			    float handPriority = hand.InteractingVolume.MovementPriority;
			    float siblingPriority = hand.Sibling.InteractingVolume.MovementPriority;

			    if (handPriority == siblingPriority)
				    currentHand = defaultHand;
			    else
				    currentHand = handPriority > siblingPriority ? hand : hand.Sibling;
		    }*/
        }

        public delegate void MovementEvent();
        public MovementEvent _StartMoving;
        public MovementEvent _StopMoving;

        void UpdatePosition()
        {
            if (!currentHand)
                return;

            if (movementAxis == null)
                return;

            float sprintInput = sprinting ? sprintScale : 1;

            
            Vector3 movement = ((new Vector3(currentHand.transform.forward.x, 0, currentHand.transform.forward.z).normalized
                                    * (linearMovement ? (movementAxis.GetAxis(currentHand.inputSource).y != 0 ? 1 : 0) : movementAxis.GetAxis(currentHand.inputSource).y))
                              + (new Vector3(currentHand.transform.right.x, 0, currentHand.transform.right.z).normalized
                                    * (linearMovement ? (movementAxis.GetAxis(currentHand.inputSource).x != 0 ? 1 : 0) : movementAxis.GetAxis(currentHand.inputSource).x)))
                                * movementSpeed * sprintInput * (Time.deltaTime) * (fallingTime > 0.1f ? fallingMovementSpeedScale : 1);

            RaycastHit hit;
            Collider[] collider = Physics.OverlapSphere(head.position, characterController.radius * 2f, door);

            if (collider.Length > 0)
                if (collider[0])
                    if (Physics.Linecast(head.position, collider[0].transform.position, out hit, door))
                    {
                        if (Vector3.Dot(hit.normal, movement) < 0)
                            movement = Vector3.ProjectOnPlane(movement, hit.normal);

                        if (Vector3.Dot(hit.normal, (prevPos - head.position).normalized) > 0)
                            movement += Vector3.Project((prevPos - head.position), hit.normal);
                    }

            if (!characterController.isGrounded && !climbing)
                fallingTime += (Time.deltaTime);
            else
                fallingTime = 0;

            if (climbingHand && climbingPos == Vector3.zero)
            {
                climbingPos = climbingHand.transform.position;

                if (climbable)
                    climbablePos = climbable.transform.position;
            }

            movement.y = Mathf.Clamp((climbing ? 0 : -gravity) * Mathf.Pow(fallingTime + 1, 2), Mathf.NegativeInfinity, -2f) * (Time.deltaTime);

            characterController.Move(climbing ? (climbingPos - climbingHand.transform.position) + (climbable.transform.position - climbablePos) : movement);

            rig.position += transform.position - prevPos;
            transform.position = head.position;

            prevPos = transform.position;

            characterController.height = climbing ? climbingHeight : Mathf.Lerp(characterController.height, head.localPosition.y, Time.time - climbingTime);
            characterController.center = new Vector3(0, climbing ? 0 : Mathf.Lerp(characterController.center.y, -(head.localPosition.y) / 2, Time.time - climbingTime), 0);

            body.position = head.position;

            climbingPos = climbing ? climbingHand.transform.position : Vector3.zero;
            climbablePos = climbing ? climbable.transform.position : Vector3.zero;
            climbingTime = climbing ? Time.time : climbingTime;

            float tempDot = Vector3.Dot(head.forward, Vector3.down);

            if (tempDot > 0.8f)
            {
                if (headYRotation != -1)
                {
                    if (Mathf.Abs(headYRotation - head.rotation.eulerAngles.y) > 60 && tempDot < 0.95f)
                        headYRotation = head.rotation.eulerAngles.y;

                    body.rotation = Quaternion.Euler(0, headYRotation, 0);
                }
                else
                    headYRotation = head.rotation.eulerAngles.y;
            }
            else
            {
                body.rotation = Quaternion.Euler(0, head.rotation.eulerAngles.y, 0);
                headYRotation = head.rotation.eulerAngles.y;
            }

            if (!currentHand)
                return;

            if (VrFpsInput.TouchPadInput(null, VrFpsInput.TouchPadDirection.dontMatter, defaultHand))
                sprinting = true;
            
            if (Mathf.Abs(movementAxis.GetAxis(currentHand.inputSource).x) <= sprintingDeadzone
             && Mathf.Abs(movementAxis.GetAxis(currentHand.inputSource).y) <= sprintingDeadzone)
                sprinting = false;
            

            updateTime = Time.time;
        }
    }
}
