using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    public class Climbable : Item
    {
        protected CharacterControllerMovement characterController;

        public Vector3 climbGrabPosition;

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (PrimaryHand)
            {
                if (Vector3.Distance(climbGrabPosition, PrimaryHand.transform.position) > 0.75f)
                {
                    Detach();
                }
            }
        }

        protected override void PrimaryGrasp()
        {
            base.PrimaryGrasp();
            characterController = PrimaryHand.CharController;
            TwitchExtension.SetItemLayerRecursivly(gameObject, "Default");
            climbGrabPosition = PrimaryHand.transform.position;
            characterController.ClimbingHand = PrimaryHand;
            characterController.climbable = transform;
        }

        protected override void PrimaryDrop()
        {
            if (characterController.climbingHand.Sibling.StoredItem)
            {
                if (characterController.climbingHand.Sibling.StoredItem.GetType() == typeof(Climbable))
                {
                    characterController.ClimbingHand = PrimaryHand.Sibling;
                }
                else if (characterController.climbingHand == PrimaryHand)
                {
                    characterController.ClimbingHand = null;
                }
            }
            else if (characterController.climbingHand == PrimaryHand)
            {
                characterController.ClimbingHand = null;
            }
            base.PrimaryDrop();

            gameObject.layer = LayerMask.NameToLayer("Item");
        }
    }
}
