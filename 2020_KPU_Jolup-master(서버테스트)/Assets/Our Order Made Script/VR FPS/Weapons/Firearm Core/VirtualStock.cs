using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    public class VirtualStock : MonoBehaviour
    {
        public delegate void VirtualStockEvent();
        public VirtualStockEvent _StartVirtualStock;
        public VirtualStockEvent _StopVirtualStock;

        public Transform shoulderPointVisual;
        public Transform forwardPointVisual;

        [SerializeField] protected Transform head;

        [SerializeField] protected Vector3 shoulderOffset;
        [SerializeField] protected float forwardPlaneOffset;

        public float StockOffset
        {
            get
            {
                return stockOffset;
            }
        }

        [SerializeField] protected float stockOffset;

        public bool Shouldered
        {
            get
            {
                return shouldered;
            }
        }

        bool shouldered;

        public Vector3 ShoulderPosition
        {
            get
            {
                return head.TransformPoint(shoulderOffset);
            }
        }

        public Vector3 ForwardPlanePosition
        {
            get
            {
                Vector3 tempOffset = shoulderOffset;
                tempOffset.z += forwardPlaneOffset;

                return head.TransformPoint(tempOffset);
            }
        }

        protected Plane shoulderPlane;
        protected Plane forwardPlane;

        [SerializeField] protected float shoulderWeaponDistance; 

        private void Start()
        {
            shoulderPlane = new Plane(head.forward, ShoulderPosition);
            forwardPlane = new Plane(head.forward, ForwardPlanePosition);
        }

        private void Update()
        {
            if (shoulderPointVisual)
            {
                shoulderPointVisual.position = ShoulderPosition;
                shoulderPointVisual.rotation = head.rotation;
            }

            if (forwardPointVisual)
            {
                forwardPointVisual.position = ForwardPlanePosition;
                forwardPointVisual.rotation = head.rotation;
            }
        }

        public bool IsTheWeaponShouldered(Vector3 shoulderHandPosition)
        {
            bool isShouldered = Vector3.Distance(shoulderHandPosition, ShoulderPosition) < shoulderWeaponDistance;

            if(shouldered != isShouldered)
            {
                if (isShouldered)
                {
                    if (_StartVirtualStock != null)
                        _StartVirtualStock();
                }
                else
                {
                    if (_StopVirtualStock != null)
                        _StopVirtualStock();
                }
            }

            shouldered = isShouldered;

            return isShouldered;
        }

        public Vector3 ShoulderedPoint(Vector3 handPosition)
        {
            shoulderPlane.SetNormalAndPosition(head.forward, ShoulderPosition);
            Vector3 shoulderPoint = shoulderPlane.ClosestPointOnPlane(handPosition);

            return shoulderPoint;
        }

        public Vector3 ForwardPoint(Vector3 handPosition)
        {
            forwardPlane.SetNormalAndPosition(head.forward, ForwardPlanePosition);
            return forwardPlane.ClosestPointOnPlane(handPosition);
        }
    }
}