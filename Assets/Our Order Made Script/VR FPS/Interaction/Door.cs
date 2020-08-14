using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    public class Door : Item
    {
        protected HingeJoint hinge;

        [SerializeField] protected float maxAngle;
        [SerializeField] protected float closedAngle;
        [SerializeField] protected float closedAngleThreshold;

        [SerializeField] protected float currentAngle;
        [SerializeField] protected float openAngle;

        [SerializeField] protected Vector2 freeSwingDrag;
        [SerializeField] protected Vector2 heldDrag;

        public Transform InitialAttachPoint;

        public float MaxAngle
        {
            get
            {
                return maxAngle;
            }
        }
        public float ClosedAngle
        {
            get
            {
                return closedAngle;
            }
        }

        public bool Open
        {
            get
            {
                return currentAngle >= openAngle;
            }
        }
        public float CurrentAngle
        {
            get
            {
                return currentAngle;
            }
        }

        protected override void Start()
        {
            base.Start();
            hinge = GetComponent<HingeJoint>();
            rb.maxAngularVelocity = 100f;
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            currentAngle = hinge.angle;

            if (!rb)
            {
                return;
            }

            if (currentAngle < closedAngleThreshold + closedAngle && !PrimaryHand)
            {
                SetLimits(closedAngle, closedAngle + closedAngleThreshold, 0.1f);
            }
        }

        public void SetLimits(float min, float max, float bounciness)
        {
            if (hinge.limits.min == min && hinge.limits.max == max)
            {
                return;
            }

            JointLimits tempLimits = new JointLimits();
            tempLimits.min = min;
            tempLimits.max = max;
            tempLimits.bounciness = bounciness;
            hinge.limits = tempLimits;
        }

        public void SetLimits(float min, float max)
        {
            if (hinge.limits.min == min && hinge.limits.max == max)
            {
                return;
            }

            JointLimits tempLimits = new JointLimits();
            tempLimits.min = min;
            tempLimits.max = max;
            hinge.limits = tempLimits;
        }

        protected override void PrimaryGrasp()
        {
            if (!PrimaryHand)
            {
                return;
            }

            PrimaryHand.StoredItem = this;

            InitialAttachPoint = new GameObject(string.Format("[{0}] InitialAttachPoint", this.gameObject.name)).transform;
            InitialAttachPoint.position = PrimaryHand.transform.position;
            InitialAttachPoint.rotation = PrimaryHand.transform.rotation;
            InitialAttachPoint.localScale = Vector3.one * 0.25f;
            InitialAttachPoint.parent = this.transform;

            rb.drag = heldDrag.x;
            rb.angularDrag = heldDrag.y;

            SetPhysics(onAttach);
            SetLimits(closedAngle, maxAngle);
        }

        protected override void PrimaryDrop()
        {
            PrimaryHand.StoredItem = null;

            transform.SetParent(null, true);

            if (rb)
            {
                rb.drag = freeSwingDrag.x;
                rb.angularDrag = freeSwingDrag.y;
            }

            SetPhysics(onDetach);

            if (InitialAttachPoint != null)
            {
                Destroy(InitialAttachPoint.gameObject);
            }
        }

        public override void Pose()
        {
            if (PrimaryHand && rb)
            {
                Vector3 positionDelta = (PrimaryHand.transform.position - InitialAttachPoint.position) * setPositionSpeed;
                rb.AddForceAtPosition(positionDelta, InitialAttachPoint.position, ForceMode.VelocityChange);
            }
        }
    }
}