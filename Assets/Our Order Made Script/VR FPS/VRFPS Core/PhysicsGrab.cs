using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    public class PhysicsGrab : MonoBehaviour
    {
        [SerializeField] protected VectorHistoryAverage velocityHistory;

        protected Rigidbody anchorRb;
        protected InteractionVolume iv;
        protected Rigidbody rb;
        protected Collider col;

        public Collider Col { get { return col; } }

        public InteractionVolume IV { get { return iv; } }
        public Rigidbody RB { get { return rb; } }

        [SerializeField] protected List<PhysicsGrab> connectedBody = new List<PhysicsGrab>();

        protected Transform InitialAttachPoint;

        public Vector3 AttachPosition { get { return InitialAttachPoint ? InitialAttachPoint.position : iv.Hand ? iv.Hand.transform.position : Vector3.zero; } }
        [SerializeField] protected AnimationCurve forceCurve;
        [SerializeField] protected float force = 3;

        [SerializeField] protected Vector2 grabDrag;
        [SerializeField] protected Vector2 dropDrag;

        public bool priorityFixedJoint;
        public float breakPullForceDistance = 1.25f;

        protected FixedJoint fixedJoint;
        public FixedJoint FixedJoint { get { return fixedJoint; } }

        [SerializeField] protected bool parentOnFixedJoint;

        void Start()
        {
            TwitchExtension.SetLayerRecursivly(gameObject, "Item");

            iv = GetComponent<InteractionVolume>();
            iv._StartInteraction += Grab;
            iv._EndInteraction += Release;

            velocityHistory.InitializeHistory();
            col = GetComponent<Collider>();
            rb = GetComponent<Rigidbody>();
            anchorRb = transform.parent.GetComponent<Rigidbody>();

            foreach(PhysicsGrab connectedBodyCol in connectedBody)
            {
                Physics.IgnoreCollision(col, connectedBodyCol.GetComponent<Collider>(), true);
            }
        }

        void Grab()
        {
            bool alreadyGrabbingBody = false;

            PhysicsGrab grabbedBody = null;

            for (int i = 0; i < connectedBody.Count; i++)
            {
                PhysicsGrab tempIV = connectedBody[i];
                if (tempIV.IV.Hand != null)
                {
                    grabbedBody = tempIV;
                    alreadyGrabbingBody = true;
                    break;
                }
            }

            if (alreadyGrabbingBody || anchorRb.isKinematic)
            {
                if (priorityFixedJoint && !anchorRb.isKinematic)
                {
                    grabbedBody.FixedJointToForcePull();
                    SetFixedJoint();
                }
                else
                {
                    SetPullForcePoint(Vector3.zero);
                }
            }
            else if (!anchorRb.isKinematic && !alreadyGrabbingBody)
            {
                SetFixedJoint();
            }

            Physics.IgnoreCollision(col, iv.Hand.PlayerCollider);
        }

        void Release()
        {
            bool otherConnectedBody = false;

            for (int i = 0; i < connectedBody.Count; i++)
            {
                PhysicsGrab tempIV = connectedBody[i];

                if (tempIV.IV.Hand)
                {
                    if (!anchorRb.isKinematic)
                    {
                        if(fixedJoint)
                            if (fixedJoint.connectedBody != tempIV.IV.Hand.OffsetRigidbody)
                                tempIV.ForcePullToFixedJoint();

                        otherConnectedBody = true;
                    }

                    break;
                }
            }

            bool connected = fixedJoint ? fixedJoint.connectedBody == IV.Hand.OffsetRigidbody : false;

            rb.drag = dropDrag.x;
            rb.angularDrag = dropDrag.y;

            if (fixedJoint)
            {
                fixedJoint.connectedBody = null;
                Destroy(fixedJoint);
            }

            DestroyAttachPoint();

            if (connected && !otherConnectedBody)
            {
                velocityHistory.ReleaseVelocity(rb);

                if (parentOnFixedJoint)
                    transform.parent = null;
            }

            Physics.IgnoreCollision(col, iv.Hand.PlayerCollider, false);
        }

        void FixedUpdate()
        {
            if (iv.Hand && rb)
            {
                velocityHistory.VelocityStep(transform);

                if (InitialAttachPoint && fixedJoint ? fixedJoint.connectedBody != iv.Hand.OffsetRigidbody : true)
                {
                    float tempDistance = 
                        Vector3.Distance(iv.Hand.transform.position
                        , InitialAttachPoint ? InitialAttachPoint.position : iv.Hand.transform.position);

                    Vector3 positionDelta = (iv.Hand.transform.position - InitialAttachPoint.position) * forceCurve.Evaluate(tempDistance) * force;
                    rb.AddForceAtPosition(positionDelta, InitialAttachPoint.position, ForceMode.VelocityChange);

                    if (tempDistance > breakPullForceDistance)
                        IV.StopInteraction();
                }
            }
        }

        public bool ConnectedTo(PhysicsGrab otherBody)
        {
            return connectedBody.Contains(otherBody);
        }

        public void RemoveConnectedBody(PhysicsGrab physGrab)
        {
            if (connectedBody.Contains(physGrab))
            {
                connectedBody.Remove(physGrab);
                Physics.IgnoreCollision(col, physGrab.Col, false);
            }
        }

        public void RemoveConnectedBodies()
        {
            connectedBody.Clear();
        }

        public Transform indicator;

        public void SetPullForcePoint(Vector3 position)
        {
            InitialAttachPoint = new GameObject(string.Format("[{0}] InitialAttachPoint", this.gameObject.name)).transform;
            InitialAttachPoint.position = position == Vector3.zero ? iv.Hand.transform.position : position;
            InitialAttachPoint.rotation = iv.Hand.transform.rotation;
            InitialAttachPoint.localScale = Vector3.one * 0.25f;
            InitialAttachPoint.SetParent(transform, true);


            rb.drag = grabDrag.x;
            rb.angularDrag = grabDrag.y;
        }

        public void DestroyAttachPoint()
        {
            if (InitialAttachPoint != null)
            {
                if (InitialAttachPoint.childCount == 0)
                {
                    Destroy(InitialAttachPoint.gameObject);
                }
                else
                {
                    Invoke("DestroyAttachPoint", Time.fixedDeltaTime);
                }
            }
        }

        public void SetFixedJoint()
        {
            fixedJoint = gameObject.AddComponent<FixedJoint>();
            fixedJoint.connectedBody = iv.Hand.OffsetRigidbody;

            if (parentOnFixedJoint)
                transform.parent = iv.Hand.Offset;
        }

        public void FixedJointToForcePull()
        {
            if (fixedJoint.connectedBody == null) return;

            fixedJoint.connectedBody = null;
            Destroy(fixedJoint);
            SetPullForcePoint(iv.Hand.transform.position);
        }

        public void ForcePullToFixedJoint()
        {
            if (AttachPosition == Vector3.zero) return;

            if (InitialAttachPoint)
            {
                InitialAttachPoint.SetParent(null);
                transform.parent = InitialAttachPoint;

                InitialAttachPoint.position = IV.Hand.Offset.position;
                InitialAttachPoint.rotation = IV.Hand.Offset.rotation;

                transform.parent = null;

                SetFixedJoint();
            }
        }
    }
}