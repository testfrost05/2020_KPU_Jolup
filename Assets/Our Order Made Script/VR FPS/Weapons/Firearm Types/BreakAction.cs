using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    public class BreakAction : Firearm //파이어암 상속해서 그 기반으로 더블배럴샷건 구성
    {
        [SerializeField] protected FreeSwingHinge hinge;
        protected Collider hingeCollider;

        protected override void Start()
        {
            base.Start();
            hinge = GetComponentInChildren<FreeSwingHinge>();
            hingeCollider = hinge.GetComponent<Collider>();
        }

        protected override void ChamberBullet(Chamber chamber)
        {
            if (!hinge.locked)
            {
                base.ChamberBullet(chamber);
            }
        }

        protected override void TouchpadDownInput()
        {
            TouchPadInput(UnlockHingeWrapper, TouchPadDirection.up);
        }

        protected void UnlockHingeWrapper()
        {
            if (!hinge)
            {
                return;
            }
        
            hinge.Unlock();

            for (int i = 0; i < chambers.Count; i++)
            {
                if (chambers[i].Bullet)
                {
                    if (chambers[i].Bullet.Spent)
                    {
                        Physics.IgnoreCollision(chambers[i].Bullet.Col, hingeCollider);

                    }
                }
            }

            Invoke("EjectCartridge", 0.2f);
        }

        protected override void EjectCartridge() //샷건 탄환 배출
        {
            if (hinge.locked)
            {
                return;
            }

            for (int i = 0; i < chambers.Count; i++)
            {
                if (chambers[i].Bullet)
                {
                    if (chambers[i].Bullet.Spent)
                    {
                        base.EjectCartridge(i);
                    }
                }

            }
        }

        protected override void Fire()
        {
            for (int i = 0; i < chambers.Count; i++)
            {
                if (!Fire(i))
                {
                    continue;
                }
                break;
            }
        }

        protected override bool FirePreconditions(int i)
        {
            if (!hinge.locked)
            {
                return false;
            }
            return base.FirePreconditions(i);
        }
    }
}