using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    public class Revolver : HammerActionPistol //해머액션피스톨를 상속
    {
        protected FreeSwingHinge cylHinge; //실린더 힌지 -> 리볼버는 탄창이 아닌 실린더를 뺴고 끼니
        [SerializeField] protected Transform cylinder; 

        protected RevolverHammer revolverHammer;  //리볼버 해머
        [SerializeField] protected InteractionVolume extractorRod;

        [SerializeField] protected float ejectBulletsVelocityThreshold = 1; //탄피 배출

        [SerializeField] protected TwitchExtension.DotAxis pullToEjectDirection; 

        [SerializeField] protected OVRInput.Button openCylinderInput; //실린더 여는 키 입력
        [SerializeField] protected TouchPadDirection openCylinderDirection;

        protected override void Start() 
        {
            base.Start();

            if (!cylHinge)
            {
                cylHinge = GetComponentInChildren<FreeSwingHinge>();
            }

            if (!revolverHammer)
            {
                revolverHammer = GetComponentInChildren<RevolverHammer>();
            }

            hammer = revolverHammer;

            revolverHammer.Chambers = chambers.Count;

            revolverHammer._OnCockHammer += AdvanceChamberIndex;
            revolverHammer.Hinge = cylHinge;
            revolverHammer.Cyl = cylinder;

            if (extractorRod)
            {
                cylHinge._Close += RestrainExtractorRod;
                cylHinge._Open += FreeExtractorRod;

                extractorRod._StartInteraction += ExtractorRod;
            }
        }


        //실린더 힌지 관련
        protected virtual void RestrainExtractorRod()
        {
            extractorRod.restrained = true;
            cylHinge.Restrained = true;
        }

        protected virtual void FreeExtractorRod()
        {
            extractorRod.restrained = false;
            cylHinge.Restrained = false;
        }

        protected virtual void ExtractorRod()
        {
            if (!cylHinge.locked)
            {
                bool hasSpentShell = false;

                foreach (Chamber chamber in chambers)
                {
                    if (chamber.Bullet != null)
                    {
                        if (chamber.Bullet.Spent)
                        {
                            hasSpentShell = true;
                            break;
                        }
                    }
                }

                if (hasSpentShell)
                {
                    EjectShells();
                }
                else
                {
                    Hand tempHand = extractorRod.Hand;
                    extractorRod.StopInteraction();
                    cylHinge.ForceStart(tempHand);
                }
            }
        }

        protected override void Update() //업데이트
        {
            base.Update();

            if (!PrimaryHand)
                return;

            Vector3 tempVelocity = velocityHistory._ReleaseVelocity;
            Vector3 tempCharVelocity = PrimaryHand.CharControllerVelocity;


            if ((tempVelocity - tempCharVelocity).magnitude > ejectBulletsVelocityThreshold &&
                Vector3.Dot(TwitchExtension.ReturnAxis(pullToEjectDirection, transform), tempVelocity.normalized) > 0.75f)
            {
                EjectShells();
            }

            if (!hammer.Cocked && !hammer.Firing)
            {
                LocalInputDown(cylHinge.Unlock, openCylinderInput);
            }
        }


        protected override void TouchpadUpInput()
        {
            base.TouchpadDownInput();

            if (!hammer.Cocked && !hammer.Firing)
            {
                TouchPadInput(cylHinge.Unlock, openCylinderDirection);
            }
        }


        protected override void TouchpadDownInput() //터치패드 입력
        {
            base.TouchpadDownInput();

            if (!hammer.Cocked && !hammer.Firing)
            {
                TouchPadInput(cylHinge.Unlock, openCylinderDirection);
            }
        }

        protected void EjectShells()
        {
            if (cylHinge.locked)
            {
                return;
            }
            for (int i = 0; i < chambers.Count; i++)
            {
                if (chambers[i].Bullet)
                {
                    if (chambers[i].Bullet.Spent)
                    {
                        chambers[i].EjectBullet(velocityHistory._ReleaseVelocity);
                    }
                }
            }
        }

        protected override bool FirePreconditions()
        {
            if (hammer)
            {
                if (hammer.Firing)
                {
                    pulledTriggerWhileSlideWasBack = true;
                }
            }

            if (chambers[selectedChamberIndex].Bullet)
            {
                if (!chambers[selectedChamberIndex].Bullet.Spent)
                {
                    if (cylHinge.locked)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        protected override void ChamberBullet(Chamber chamber)
        {
            if (!cylHinge.locked)
            {
                base.ChamberBullet(chamber);
            }
        }
    }
}