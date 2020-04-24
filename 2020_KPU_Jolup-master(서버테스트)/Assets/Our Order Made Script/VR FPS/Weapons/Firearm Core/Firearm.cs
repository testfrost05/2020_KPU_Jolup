using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    public class Firearm : Item
    {
        //각종 파츠
        protected Transform muzzle;
        protected FirearmSlide slide;
        protected Trigger trigger;
        protected FireSelector fireSelector;
        protected SocketSlide magWell;
        protected Magazine magazine;

        protected List<Chamber> chambers = new List<Chamber>();
        protected int selectedChamberIndex;

        [SerializeField] protected RecoilManager recoilManager;
        protected FirearmSFX_Manager firearmAudioManager;

        [SerializeField] protected bool stackTriggerPulls;
        [SerializeField] protected bool slideStopOnEmptyMag;

        [SerializeField] protected GameObject muzzleFlash;
        [SerializeField] protected float muzzleVelocity;
        [SerializeField] protected float spread;

        [SerializeField] protected float slideEjectVelocity;

        protected bool enableSlideAudio = true;

        protected override void Start()
        {
            base.Start();

            recoilManager.SetSelf(this);

            if (!slide)
            {
                slide = GetComponentInChildren<FirearmSlide>();
            }
            if (!magWell)
            {
                magWell = GetComponentInChildren<SocketSlide>();
            }
            if (!trigger)
            {
                trigger = GetComponentInChildren<Trigger>();
            }
            if (!fireSelector)
            {
                fireSelector = GetComponentInChildren<FireSelector>();
            }
            if (!muzzle)
            {
                muzzle = transform.Find("Muzzle");
            }
            if (!muzzle)
            {
                muzzle = transform.Find("muzzle");
            }
            if (!magazine)
            {
                magazine = GetComponentInChildren<Magazine>();
            }

            if (!firearmAudioManager)
            {
                firearmAudioManager = GetComponent<FirearmSFX_Manager>();
            }
            if (!firearmAudioManager)
            {
                firearmAudioManager = gameObject.AddComponent<FirearmSFX_Manager>();
            }

            //자식 오브젝트에서 장전손잡이, 방아쇠, 탄창 등등 찾기

            if (virtualStock)
            {
                virtualStock._StartVirtualStock += StartVirutalStock;
                virtualStock._StopVirtualStock += StopVirtualStock;
            }

            if (chambers.Count == 0)
            {
                Chamber[] tempChambers = GetComponentsInChildren<Chamber>();

                foreach (Chamber tempChamber in tempChambers)
                {
                    chambers.Add(tempChamber);
                    tempChamber._LoadBullet += ChamberBullet;
                }
            }

            if (magWell)
            {
                magWell._OnGrab += RemoveMag;
                magWell._OnLoad += LoadMag;
                magWell._OnReachedStart += RemoveMag;

                magWell._OnReachedStart += DetachMagSFX;

                magWell._OnStartSlide += RestrainTapedMag;
            }

            if (slide)
            {
                slide._CatchBullet += CatchBullet;
                slide._OnReachedStart += ChamberRoundFromMagazine;

                slide._OnReachedEnd += EjectCartridge;
                slide._OnReachedEnd += CatchBullet;

                slide._PulledPassedSlideStop += SlideStop;
                slide._RestingOnSlideStop += RestOnSlideStop;

                slide._OnReachedStart += SlideFowardAudio;
                slide._OnReachedEnd += SlideBackAudio;
            }

            if (attachmentMngr)
            {
                attachmentMngr._OnAttach += AddAttachment;
                attachmentMngr._OnDetach += RemoveAttachment;
            }

            if (trigger)
            {
                trigger._TriggerPulled += PullTrigger;
                trigger._TriggerHeld += HoldTrigger;
                trigger._TriggerReleased += ReleaseTrigger;
            }

            if (slide && fireSelector)
            {
                slide.minSliderDistance = fireSelector.FireMode == FireSelector._FireMode.safety ?
                    fireSelector.SafetySlideLimit > 0 ? fireSelector.SafetySlideLimit : slide.minSliderDistance : 0;
            }
            else if (slide)
            {
                slide.minSliderDistance = 0;
            }
        }

        //슬라이드 사운드
        protected virtual void SlideFowardAudio()
        {
            if (!enableSlideAudio)
            {
                enableSlideAudio = true;
                return;
            }

            if (firearmAudioManager)
            {
                firearmAudioManager.PlayRandomAudioClip(firearmAudioManager.actionFowardSounds, slide.transform.position);
            }
        }

        protected virtual void SlideBackAudio()
        {
            if (!enableSlideAudio)
            {
                return;
            }
            if (firearmAudioManager)
            {
                firearmAudioManager.PlayRandomAudioClip(firearmAudioManager.actionBackSounds, slide.transform.position);
            }
        }

        bool magOnPressDown;

        protected override void Update()
        {
            base.Update();

            recoilManager.ApplyRecoil();

            if (!PrimaryHand)
                return;

            if (trigger) trigger.PoseTrigger(PrimaryHand.TriggerRotation);

            LocalInputDown(TouchpadDownInput, PrimaryHand.TouchpadInput);

            LocalInputUp(TouchpadUpInput, PrimaryHand.TouchpadInput);
        
            if (LocalInputUp(null, PrimaryHand.TriggerInput))
            {
                releasedTriggerAfterPickUp = true;
            }

            LocalEjectMagInput();
            LocalFireSelectorInput();
            LocalSlideStopInput();
            
        }

        protected virtual void LocalFireSelectorInput() //조정간 객체에서 정보 받아와서 총쏠수있는지 아닌지 
        {
            if (fireSelector)
            {
                if (fireSelector.FireSelectorInput != null)
                {
                    if (LocalInputDown(null, fireSelector.FireSelectorInput))
                    {
                        float tempSlidePosition = slide ? slide.slidePosition : Mathf.Infinity;

                        if ((fireSelector.NextFireMode() == FireSelector._FireMode.safety && tempSlidePosition >= fireSelector.SafetySlideLimit)
                            || fireSelector.NextFireMode() != FireSelector._FireMode.safety)
                        {
                            fireSelector.SwitchFireMode();
                        }
                    }
                }
            }
        }

        protected virtual void LocalSlideStopInput() //세이프 모드이면 슬라이드 총을 못쏘니 자동 장전도 안함
        {
            if (slide)
            {
                if (slide.SlideStopInput != null)
                {
                    if (LocalInputDown(null, slide.SlideStopInput))
                    {
                        if (slide)
                        {
                            if (fireSelector)
                            {
                                slide.minSliderDistance = fireSelector.FireMode ==
                                    FireSelector._FireMode.safety ? fireSelector.SafetySlideLimit > 0 ?
                                    fireSelector.SafetySlideLimit : slide.minSliderDistance : 0;
                            }
                            slide.SlideStopTouchpadInput();
                        }
                    }
                }
            }
        }

        protected virtual void LocalEjectMagInput() //탄창빼는거
        {
            if (magWell)
                if (magWell.EjectInput != null)
                {
                    if (LocalInputDown(null, magWell.EjectInput))
                    {
                        magOnPressDown = magazine;
                    }
                    LocalInputUp(EjectMagazine, magWell.EjectInput);
                }
        }

        protected virtual void TouchpadUpInput()
        {
            TouchPadInput(EjectMagazine, magWell.EjectTouchpadDirection);
        }

        Magazine ejectMag;

        void EjectMagazine() //버튼으로 탄창 빼기
        {
            if (magWell && magOnPressDown)
            {
                magWell.EjectSlider();
                ejectMag = magazine;
                magazine = null;
                caughtRoundFromMagazine = false;
            }

            magOnPressDown = false;
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
        }

        bool releasedTriggerAfterPickUp = true;

        protected override void PrimaryGrasp() //총 잡는거 설정
        {
            if (trigger)
            {
                trigger.ReleasedTrigger = false;
            }

            if (primaryGrip.StartInputID == PrimaryHand.TriggerInput &&
                OVRInput.Get(primaryGrip.StartInputID, PrimaryHand.inputSource))
            {
                releasedTriggerAfterPickUp = false;
            }

            if (recoilManager.recoilRotationOffset)
            {
                GameObject destroyThis = recoilManager.recoilRotationOffset.gameObject;
                Destroy(destroyThis);
            }

            //총기 반동
            recoilManager.recoilRotationOffset = new GameObject(string.Format("Recoil Rotation Offset", name)).transform;
            recoilManager.recoilRotationOffset.SetParent(PrimaryHand.Offset, true);
            recoilManager.recoilRotationOffset.localScale = Vector3.one;
            recoilManager.recoilRotationOffset.localPosition = Vector3.zero;
            recoilManager.recoilRotationOffset.localRotation = Quaternion.identity;

            recoilManager.recoilPositionOffset = new GameObject(string.Format("Recoil Position Offset", name)).transform;
            recoilManager.recoilPositionOffset.SetParent(recoilManager.recoilRotationOffset, true);
            recoilManager.recoilPositionOffset.localScale = Vector3.one;
            recoilManager.recoilPositionOffset.localPosition = Vector3.zero;
            recoilManager.recoilPositionOffset.localRotation = Quaternion.identity;

            base.PrimaryGrasp();

            transform.SetParent(recoilManager.recoilPositionOffset, true);

            PrimaryHand.audioSourceContainer.transform.parent = muzzle;
            PrimaryHand.audioSourceContainer.transform.localPosition = Vector3.zero;
        }

        public override void StartVirutalStock()
        {
            base.StartVirutalStock();
            transform.SetParent(recoilManager.recoilPositionOffset, true);
        }

        protected override void PrimaryDrop() //총 떨구는거 설정
        {

            PrimaryHand.audioSourceContainer.transform.parent = PrimaryHand.transform;
            PrimaryHand.audioSourceContainer.transform.localPosition = Vector3.zero;

            base.PrimaryDrop();

            releasedTriggerAfterPickUp = true;

            if (recoilManager.recoilPositionOffset || recoilManager.recoilRotationOffset)
            {
                GameObject destroyThis = recoilManager.recoilRotationOffset.gameObject;
                recoilManager.recoilPositionOffset = null;
                recoilManager.recoilRotationOffset = null;
                Destroy(destroyThis);
            }

            recoilManager.ClearRecoil();
        }

        public override void StopVirtualStock()
        {
            base.StopVirtualStock();
            transform.SetParent(recoilManager.recoilPositionOffset, true);
        }

        protected override void SecondaryGrasp() // 총을 잡고 다른 한손으로 총의 다른 부분을 또 잡을수 있게
        {
            base.SecondaryGrasp();

            SecondaryHand.audioSourceContainer.transform.parent = muzzle;
            SecondaryHand.audioSourceContainer.transform.localPosition = Vector3.zero;
        }

        protected override void SecondaryDrop() //2번째로 잡은걸 놓기
        {
            SecondaryHand.audioSourceContainer.transform.parent = SecondaryHand.transform;
            SecondaryHand.audioSourceContainer.transform.localPosition = Vector3.zero;

            base.SecondaryDrop();
        }

        protected virtual void TouchpadDownInput() // 컨트롤러 입력
        {
            magOnPressDown = magazine;

            float tempSlidePosition = slide ? slide.slidePosition : Mathf.Infinity;

            if (fireSelector)
            {
                if ((fireSelector.NextFireMode() == FireSelector._FireMode.safety && tempSlidePosition >= fireSelector.SafetySlideLimit)
                    || fireSelector.NextFireMode() != FireSelector._FireMode.safety)
                {
                    TouchPadInput(fireSelector.SwitchFireMode, fireSelector._TouchPadDirection);
                }
            }

            if (slide)
            {
                if (fireSelector)
                {
                    slide.minSliderDistance = fireSelector.FireMode == FireSelector._FireMode.safety ? fireSelector.SafetySlideLimit > 0 ? fireSelector.SafetySlideLimit : slide.minSliderDistance : 0;
                }
                TouchPadInput(slide.SlideStopTouchpadInput, slide.SlideStopTouchpadDirection);
            }
        }

        protected int burstCount;

        protected virtual void PullTrigger() //한번 눌러 총을 쏠때
        {
            if (!releasedTriggerAfterPickUp)
            {
                return;
            }

            switch (fireSelector.FireMode) //조정간에 따라 쏘고 소리 재생
            {
                case FireSelector._FireMode.safety:
                    DryFireSounds();
                    break;
                case FireSelector._FireMode.semi:
                    if (!FirePreconditions())
                    {
                        DryFireSounds();
                    }
                    Fire();
                    break;
                case FireSelector._FireMode.burst:
                    if (!FirePreconditions())
                    {
                        DryFireSounds();
                    }
                    break;
                case FireSelector._FireMode.full:
                    if (!FirePreconditions())
                    {
                        DryFireSounds();
                    }
                    break;
            }
        }

        void DryFireSounds()
        {
            if (firearmAudioManager)
            {
                firearmAudioManager.PlayRandomAudioClip(firearmAudioManager.dryFireSounds, chambers[0].transform.position);
            }
        }

        protected virtual void HoldTrigger() //꾹 누르고 총을 쏠때
        {
            if (!releasedTriggerAfterPickUp)
            {
                return;
            }

            switch (fireSelector.FireMode) //조정간에 따라서
            {
                case FireSelector._FireMode.semi:// 단발
                    if (stackTriggerPulls && pulledTriggerWhileSlideWasBack)
                    {
                        pulledTriggerWhileSlideWasBack = false;
                        Fire();
                    }
                    break;
                case FireSelector._FireMode.burst:
                    if (burstCount < 3) // 점사
                    {
                        Fire();
                    }
                    break;
                case FireSelector._FireMode.full: //연사
                    Fire();
                    break;
            }
        }

        protected virtual void ReleaseTrigger()
        {
            burstCount = 0;
            pulledTriggerWhileSlideWasBack = false;
        }

        protected virtual void EjectCartridge()
        {
            EjectCartridge(selectedChamberIndex);
        }

        protected virtual void EjectCartridge(int i) //탄피 빼기
        {
            Chamber tempChamber = chambers[i];

            if (!tempChamber)
            {
                return;
            }

            if (slide)
            {
                if (slide.InteractionPoint)
                {
                    if (slide.AverageVelocity() >= slideEjectVelocity)
                    {
                        return;
                    }
                }
            }

            Bullet tempBullet = tempChamber.Bullet;

            if (!tempBullet)
            {
                return;
            }

            Physics.IgnoreCollision(tempBullet.Col, Col);

            tempChamber.EjectBullet(velocityHistory._ReleaseVelocity);
        }

        protected virtual void SlideStop() 
        {
            if (!slideStopOnEmptyMag)
            {
                return;
            }

            bool emptyMag = magazine ? magazine.Empty : false;

            slide.SlideStop = emptyMag;

            if (emptyMag)
            {
                enableSlideAudio = true;
            }
        }

        protected virtual void RestOnSlideStop()
        {
            if (firearmAudioManager)
            {
                firearmAudioManager.PlayRandomAudioClip(firearmAudioManager.restOnSlideStopSounds, slide.transform.position);
            }
        }

        protected bool caughtRoundFromMagazine;

        protected virtual void CatchBullet()
        {
            caughtRoundFromMagazine = magazine ? !magazine.Empty : false;
        }

        protected virtual void ChamberRoundFromMagazine()
        {
            if (!caughtRoundFromMagazine)
            {
                return;
            }
            caughtRoundFromMagazine = false;

            if (!magazine)
            {
                return;
            }
            if (magazine.Empty)
            {
                return;
            }
            if (chambers[0].Bullet)
            {
                return;
            }
            Bullet tempBullet = magazine.SavedBullets[magazine.SavedBullets.Count - 1];
            magazine.SavedBullets.Remove(tempBullet);
            magazine.CurrentRounds -= 1;

            chambers[0].ChamberBullet(tempBullet);

            IgnoreCollision(chambers[0].Bullet.Projectile.Col, true);
        }

        protected virtual void ChamberBullet(Chamber chamber) { chamber.LoadPotentialBullet(); }

        protected virtual void LoadMag(Item item) //탄창 결합
        {
            Magazine tempMag = item as Magazine;

            if (tempMag)
            {
                LoadMag(tempMag);
            }
        }

        protected virtual void LoadMag(Magazine magazine)
        {
            if (!magazine)
            {
                return;
            }
            this.magazine = magazine;
            Physics.IgnoreCollision(magazine.Col, Col);
            if (firearmAudioManager)
            {
                firearmAudioManager.PlayRandomAudioClip(firearmAudioManager.loadMagazineSounds, magazine.transform.position);
            }
        }

        protected virtual void RemoveMag() //탄창 빼기
        {
            if (ejectMag)
            {
                ejectMag.Rb.velocity = velocityHistory._ReleaseVelocity + (-ejectMag.transform.up * magWell.EjectSpeed * 0.075f);

                StartCoroutine(DelayIgnorePhysics(ejectMag.Col, Col, false, 0.25f));

                ejectMag = null;
            }

            if (magazine)
            {
                if (magazine.TapedMagazine)
                {
                    magazine.TapedMagazine.Restrained = false;
                }
            }

            magazine = null;
            caughtRoundFromMagazine = false;
        }

        protected virtual void RestrainTapedMag(Item mag) // 탄창 결합한 상태로 묶기
        {
            Magazine tapedMag = mag as Magazine;

            mag.gameObject.layer = gameObject.layer;

            if (tapedMag == null)
            {
                return;
            }

            tapedMag = tapedMag.TapedMagazine;

            if (tapedMag == null)
            {
                return;
            }

            tapedMag.Detach();
            tapedMag.DetachSlot();
            tapedMag.transform.SetParent(mag.transform, true);
            tapedMag.SetPhysics(true, false, true);

            tapedMag.Restrained = true;
        }

        IEnumerator DelayIgnorePhysics(Collider colliderOne, Collider colliderTwo, bool ignore, float delay)
        {
            yield return new WaitForSeconds(delay);
            Physics.IgnoreCollision(colliderOne, colliderTwo, ignore);
        }

        protected virtual void DetachMagSFX()
        {
            if (firearmAudioManager)
            {
                firearmAudioManager.PlayRandomAudioClip(firearmAudioManager.unloadMagazineSounds, magWell.transform.position);
            }
        }
        protected bool pulledTriggerWhileSlideWasBack;

        protected virtual bool FirePreconditions()
        {
            return FirePreconditions(selectedChamberIndex);
        }

        protected virtual bool FirePreconditions(int i)
        {
            if (slide)
            {
                if (slide.slidePosition < 1)
                {
                    pulledTriggerWhileSlideWasBack = true;
                }
            }

            if (chambers[i].Bullet)
            {
                if (!chambers[i].Bullet.Spent)
                {
                    if (slide)
                    {
                        if (slide.slidePosition >= 1)
                        {
                            return true;
                        }
                    }
                    else
                        return true;
                }
            }
            return false;
        }

        protected virtual void Fire()
        {
            Fire(selectedChamberIndex);
        }

        protected virtual bool Fire(int i) // 총 쏘기
        {
            if (!FirePreconditions(i))
            {
                return false;
            }
            if (slide)
            {
                slide.AnimateSlide();
            }
            if (PrimaryHand || SecondaryHand)
            {
                recoilManager.IncreaseAllRecoil();
            }
            if (muzzleVelocity == 0)
            {
                chambers[i].Bullet.Fire(muzzle);
            }
            else
            {
                chambers[i].Bullet.Fire(muzzle, muzzleVelocity, spread);
            }

            MuzzleFlash();

            if (fireSelector.FireMode == FireSelector._FireMode.burst)
            {
                burstCount++;
            }
            enableSlideAudio = false;

            if (firearmAudioManager)
            {
                firearmAudioManager.FireFX(suppressed);
            }
            return true;
        }

        protected virtual void MuzzleFlash() //총 쏠때 나는 빛과 연기
        {
            if (muzzleFlash)
            {
                GameObject clone = Instantiate(muzzleFlash, muzzle.position, Quaternion.LookRotation(muzzle.forward), muzzle);
                Transform smoke = clone.transform.Find("Smoke");

                if (smoke)
                {
                    smoke.SetParent(null, true);
                    Destroy(smoke.gameObject, 1f);
                }

                Destroy(clone.gameObject, 0.5f);
            }
        }

        protected virtual void AdvanceChamberIndex()
        {
            if (selectedChamberIndex == chambers.Count - 1)
            {
                selectedChamberIndex = 0;
            }
            else
            {
                selectedChamberIndex++;
            }
        }

        protected bool suppressed;

        protected void AddAttachment(Attachment attachment) //부착품 끼기 ex 탄창, 사이트 , 손잡이 
        {
            attachment.gameObject.layer = gameObject.layer;

            if (attachment.type == Attachment.Type.foregrip)
            {
                Foregrip = true;
            }

            if (attachment.type == Attachment.Type.suppressor)
            {
                Suppressed = true;
            }
        }

        protected void RemoveAttachment(Attachment attachment) //부착품 해제
        {
            if (attachment.type == Attachment.Type.foregrip)
            {
                Foregrip = false;
            }
            if (attachment.type == Attachment.Type.suppressor)
            {
                Suppressed = false;
            }
        }

        protected bool Suppressed
        {
            set
            {
                Vector3 tempMuzzlePos = muzzle.localPosition;

                if (value != suppressed)
                {
                    tempMuzzlePos.z += value ? 0.1513081f / transform.lossyScale.z : -0.1513081f / transform.lossyScale.z;
                }

                muzzle.localPosition = tempMuzzlePos;
                suppressed = value;
            }
        }

        protected bool Foregrip
        {
            set
            {
                if (value)
                {
                    recoilManager.ScaleRotation(0.5f, 0.5f, 1, 0.5f, 1);
                    recoilManager.ScaleTranslation(0.5f, 0.5f, 1, 0.5f, 1);
                }
                else
                {
                    recoilManager.UnscaleRotation();
                    recoilManager.UnscaleTranslation();
                }
            }
        }
    }
}