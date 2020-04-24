using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    public class RevolverHammer : Hammer //리볼버용 해머
    {
        public HammerEvent _OnCockHammer;

        [SerializeField] protected FreeSwingHinge hinge;
        [SerializeField] protected Transform cyl;

        public FreeSwingHinge Hinge
        {
            set
            {
                hinge = value;
            }
        }

        public Transform Cyl
        {
            set
            {
                cyl = value;
            }
        }

        int chambers;

        public int Chambers
        {
            set
            {
                chambers = value;
            }
        }

        protected float initialCylRot;
        public float endCylRot;

        bool decocked;

        public override void Fire(bool decock, Vector2 axis)
        {
            base.Fire(decock, axis);

            if (decock)
                decocked = true;
        }

        protected virtual void RestCylinder()
        {
            if (cyl.localEulerAngles.z == endCylRot)
                return;

            if ((!hammerCocked) || !hinge.locked)
            {
                Vector3 tempCylRotation = cyl.localEulerAngles;
                tempCylRotation.z = Mathf.Clamp(tempCylRotation.z - (restHammerSpeed * Time.fixedDeltaTime), endCylRot - (360f / chambers), endCylRot);
                cyl.localEulerAngles = tempCylRotation;
            }
        }

        public override void PullHammer(Vector2 touchpadAxis, float slideAxis)
        {
            float triggerAxis = !firingHammer ? this.triggerAxis : 0;

            if (transform.localEulerAngles.x == 0 && decocked)
            {
                decocked = false;
                previousTouchpadAxis = Vector2.zero;
                previousTriggerAxis = 0;
                initialHammerRot = -1;
            }

            if (touchpadAxis == Vector2.zero)
                initialTouchpadAxis = Vector2.zero;

            if (!hammerCocked && !firingHammer)
            {
                if (previousTouchpadAxis == Vector2.zero && touchpadAxis != Vector2.zero)
                {
                    initialTouchpadAxis = touchpadAxis;
                }

                if (previousTouchpadAxis != Vector2.zero && touchpadAxis != Vector2.zero)
                {
                    if (touchpadAxis.y > initialTouchpadAxis.y)
                    {
                        if (transform.localEulerAngles.x == 0)
                        {
                            initialTouchpadAxis = touchpadAxis;
                            initialHammerRot = transform.localEulerAngles.x;
                        }
                    }
                }

                if (initialHammerRot == -1
                    && ((previousTouchpadAxis == Vector2.zero && touchpadAxis != Vector2.zero)
                    || (previousTriggerAxis < 0.01f && triggerAxis > 0.01f)))
                {
                    initialHammerRot = transform.localEulerAngles.x;
                    initialCylRot = cyl.localEulerAngles.z;
                }

                if ((previousTouchpadAxis == Vector2.zero && touchpadAxis == Vector2.zero) && (previousTriggerAxis < 0.01f && triggerAxis < 0.01f))
                    initialHammerRot = -1;
            }

           

            float yDelta = Mathf.Clamp(touchpadAxis.y - initialTouchpadAxis.y, -1, 1);
            yDelta -= triggerAxis;

            if (((touchpadAxis != Vector2.zero && previousTouchpadAxis != Vector2.zero)
                || (previousTriggerAxis > 0.01f && triggerAxis > 0.01f)) && !firingHammer)
            {
                if (!hammerCocked)
                {
                    if (hinge.locked && !decocked)
                    {
                        Vector3 tempCylRotation = cyl.localEulerAngles;
                        tempCylRotation.z = Mathf.Clamp((-yDelta * 120) + initialCylRot, endCylRot - (360f / chambers), endCylRot);
                        cyl.localEulerAngles = tempCylRotation;
                    }

                    PoseHammer((-yDelta * 120) + (initialHammerRot != -1 ? initialHammerRot : 0));
                }
            }
            else
            {
                if (!hammerCocked)
                {
                    Rest();

                    if (firingHammer && transform.localEulerAngles.x == 0)
                    {
                        firingHammer = false;

                        initialTouchpadAxis = touchpadAxis;
                        initialHammerRot = transform.localEulerAngles.x;
                        initialCylRot = cyl.localEulerAngles.z;

                        _Fire();
                    }
                }

                if (!hammerCocked || !hinge.locked)
                    RestCylinder();
            }

            previousTouchpadAxis = touchpadAxis;
            previousTriggerAxis = triggerAxis;
        }

        public override void LockHammer()
        {
            base.LockHammer();

            if (hinge.locked && !decocked)
            {
                if (_OnCockHammer != null)
                    _OnCockHammer();

                if (Mathf.Approximately(endCylRot, 360f))
                    endCylRot = 0f;

                Vector3 tempCylRotation = cyl.localEulerAngles;
                tempCylRotation.z = endCylRot;
                cyl.localEulerAngles = tempCylRotation;

                endCylRot += 360f / chambers;
            }
        }
    }
}