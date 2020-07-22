using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    public class RayGrab : MonoBehaviour // 원거리 그렙
    {

        [SerializeField] protected GameObject rayGrab;
        [SerializeField] protected Material rayHighlightMat;
        [SerializeField] protected Material rayEmptyMat;

        [SerializeField] protected LayerMask rayGrabLayerMask;
        [SerializeField] protected float rayGrabDistance = 5f;
        MeshRenderer rayMeshRenderer;

        void Start()
        {
            SetRayLength(rayGrabDistance);

            rayMeshRenderer = rayGrab.GetComponent<MeshRenderer>();
        }

        public InteractionVolume RaycastGrab() //레이캐스트를 이요하여 그렙
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, rayGrabDistance, rayGrabLayerMask, QueryTriggerInteraction.Ignore))
            {
                Item tempRAYIV = hit.transform.GetComponent<Item>();

                if (tempRAYIV)
                {
                    if (raycastInteraction != tempRAYIV.PrimaryGrip && raycastInteraction)
                    {
                        raycastInteraction.HighlightIsActive = false;
                    }

                    raycastInteraction = tempRAYIV.PrimaryGrip;

                    rayMeshRenderer.sharedMaterial = rayHighlightMat;

                    return tempRAYIV.PrimaryGrip;
                }
                else
                {
                    ClearRayIV();
                }

                SetRayLength(hit.distance / 2);
            }
            else
            {
                ClearRayIV();
                SetRayLength(rayGrabDistance / 2);
            }

            SetRayGrabActive(true);

            return null;
        }

        public void SetRayGrabActive(bool active) //원거리 그렙 활성화
        {
            if (rayGrab.activeSelf != active)
            {
                rayGrab.SetActive(active);
            }
        }

        void SetRayLength(float length) // 원거리 그렙 가능 거리 설정
        {
            Vector3 tempScale = rayGrab.transform.lossyScale;
            Vector3 tempPosition = rayGrab.transform.localPosition;

            tempScale.y = length;
            tempPosition.z = length;

            rayGrab.transform.localPosition = tempPosition;
            rayGrab.transform.localScale = tempScale;
        }

        InteractionVolume raycastInteraction;

        public void ClearRayIV()
        {
            if (raycastInteraction)
            {
                raycastInteraction.HighlightIsActive = false;
                raycastInteraction = null;
                rayMeshRenderer.sharedMaterial = rayEmptyMat;
            }
        }
    }
}