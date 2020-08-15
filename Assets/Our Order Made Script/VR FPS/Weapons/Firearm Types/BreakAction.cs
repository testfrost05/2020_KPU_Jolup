using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

namespace VrFps
{
    public class BreakAction : Firearm
    {
        [SerializeField] protected FreeSwingHinge hinge;
        protected Collider hingeCollider;

        [SerializeField] protected SteamVR_Action_Boolean openBreakInput;

        [SerializeField] protected bool AutoEjectCartridges;

        protected override void Update()
        {
            base.Update();
            LocalInputDown(UnlockHingeWrapper, openBreakInput);
        }

        protected override void Start()
        {
            base.Start();
            hinge = GetComponentInChildren<FreeSwingHinge>();
            hingeCollider = hinge.GetComponent<Collider>();
            hinge._Open += ActivateChamberInteractionVolume;
            hinge._Close += DeactivateChamberInteractionVolume;
        }

        void ActivateChamberInteractionVolume()
        {
            ActivateChamberInteractionVolume(true);
        }

        void DeactivateChamberInteractionVolume()
        {
            ActivateChamberInteractionVolume(false);
        }

        void ActivateChamberInteractionVolume(bool on)
        {
            foreach (Chamber chamber in chambers)
                chamber.IV.restrained = !on;
        }

        protected override void ChamberBullet(Chamber chamber)
        {
            if (!hinge.locked)
                base.ChamberBullet(chamber);
        }

        protected override void TouchpadDownInput()
        {
            TouchPadInput(UnlockHingeWrapper, VrFpsInput.TouchPadDirection.up);
        }

        protected void UnlockHingeWrapper()
        {
            if (!hinge)
                return;

            hinge.Unlock();

            for (int i = 0; i < chambers.Count; i++)
                if (chambers[i].Bullet)
                    if (chambers[i].Bullet.Spent)
                        Physics.IgnoreCollision(chambers[i].Bullet.Col, hingeCollider);

            if (AutoEjectCartridges)
                Invoke("EjectCartridge", 0.2f);
        }

        protected override void EjectCartridge()
        {
            if (hinge.locked)
                return;

            for (int i = 0; i < chambers.Count; i++)
            {
                if (chambers[i].Bullet)
                    if (chambers[i].Bullet.Spent)
                        base.EjectCartridge(i);
            }
        }

        protected override void Fire()
        {
            for (int i = 0; i < chambers.Count; i++)
            {
                if (!Fire(i))
                    continue;

                break;
            }
        }

        protected override bool FirePreconditions(int i)
        {
            if (!hinge.locked)
                return false;

            return base.FirePreconditions(i);
        }
    }
}