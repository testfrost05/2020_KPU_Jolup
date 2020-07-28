using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
namespace VrFps
{
    [PunRPC]
    public class CapsuleGrab : MonoBehaviourPunCallbacks
    {
        public List<InteractionVolume> CapsuleGrabInteractions()
        {
            Vector3 bottom = transform.position;
            bottom.y -= 1.25f;

            Collider[] colliders = Physics.OverlapCapsule(transform.position, bottom, 0.025f); //충돌 범위 내 모든 충돌체

            List<InteractionVolume> currentInteractions = new List<InteractionVolume>();

            for (int i = 0; i < colliders.Length; i++)
            {
                Item tempItem = colliders[i].GetComponent<Item>();

                if (tempItem)
                {
                    if (tempItem.PrimaryGrip)
                    {
                        currentInteractions.Add(tempItem.PrimaryGrip);
                    }
                }
            }

            return currentInteractions;
        }
    }
}