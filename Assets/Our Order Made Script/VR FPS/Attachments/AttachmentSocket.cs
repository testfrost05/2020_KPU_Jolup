using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    public class AttachmentSocket : Slot
    {
        [ReadOnly] [SerializeField] protected Attachment attachment;

        [SerializeField] InteractionVolume interactionVolume;

        float setPosTime; //위치이동 시간 설정
        float setRotTime; //회정 시간 설정

        protected float Timer;

        bool hasExited; //부착품 붙어있는지 아닌지 

        public delegate void OnAttach(Attachment attachment); //OnAttach
        public OnAttach _OnAttach;

        public delegate void OnDetach(Attachment attachment); //OnDetach
        public OnDetach _OnDetach;

        protected Attachment potentialAttech;

        [SerializeField] protected float attachDistance = 0.1f; //부착물 거리

        [Range(-1, 1)] [SerializeField] protected float primaryAttachDot;
        [Range(-1, 1)] [SerializeField] protected float secondaryAttachDot;

        public TwitchExtension.DotAxis primaAttachSocketDotAxis = TwitchExtension.DotAxis.forward;
        public TwitchExtension.DotAxis seconAttachSocketDotAxis = TwitchExtension.DotAxis.right;

        [SerializeField] protected List<AudioClip> attachSounds = new List<AudioClip>(); //부착할때 소리
        [SerializeField] protected List<AudioClip> dettachSounds = new List<AudioClip>(); //땔때 소리
       

        protected virtual void Start()
        {
            gameObject.layer = LayerMask.NameToLayer("ItemDetection"); //레이어 설정

            if (!interactionVolume)
                interactionVolume = GetComponent<InteractionVolume>(); 

            interactionVolume._StartInteraction += GrabAttachment; 

            setPosTime = setPosTime <= 0 ? 1 : setPosTime;
            setRotTime = setRotTime <= 0 ? 1 : setRotTime;

            Attachment tempAttach = GetComponentInChildren<Attachment>(); 

            if (tempAttach) 
            {
                if (!storedItem)
                {
                    storedItem = tempAttach;
                    storedItem.Restrained = true;
                    storedItem.SetKinematic(); 
                }

                if (!attachment)
                {
                    attachment = tempAttach;
                    potentialAttech = attachment;
                    AttachPotentialItem(); 
                    if (_OnAttach != null)
                        _OnAttach(attachment);
                }
            }

            interactionVolume.GetComponent<Collider>().isTrigger = true; //트리거
            hasExited = !storedItem;

            if (!attachment)
            {
                interactionVolume.restrained = true;
            }
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            //위치랑 회전
            if (potentialAttech && !storedItem && hasExited)
            {
                if (potentialAttech.PrimaryHand ^ potentialAttech.SecondaryHand)
                {
                    if (Vector3.Distance(transform.position, potentialAttech.transform.position) <= attachDistance)
                    {
                        Vector3 tempPrimaryAxis = TwitchExtension.ReturnAxis(primaAttachSocketDotAxis, transform);
                        Vector3 tempSecondaryAxis = TwitchExtension.ReturnAxis(seconAttachSocketDotAxis, transform);

                        Vector3 tempAttachPrimaryAxis = TwitchExtension.ReturnAxis(potentialAttech.primaryAttachDotAxis, potentialAttech.transform);
                        Vector3 tempAttachSecondaryAxis = TwitchExtension.ReturnAxis(potentialAttech.secondaryAttachDotAxis, potentialAttech.transform);

                        if (Vector3.Dot(tempPrimaryAxis, tempAttachPrimaryAxis) >= primaryAttachDot &&
                            Vector3.Dot(tempSecondaryAxis, tempAttachSecondaryAxis) >= secondaryAttachDot)
                        {
                            AttachPotentialItem();
                        }
                    }
                }
            }

            if (storedItem != null && Time.time - Timer > 0)
            {
                storedItem.transform.localPosition = Vector3.Lerp(storedItem.transform.localPosition,
                                                          attachment.AttachmentPosOffset,
                                                          (Mathf.Pow(Time.time - Timer, 1.5f)) / setPosTime);

                storedItem.transform.localRotation = Quaternion.Slerp(storedItem.transform.localRotation,
                                                          Quaternion.Euler(attachment.AttachmentRotOffset),
                                                          (Mathf.Pow(Time.time - Timer, 1.5f)) / setRotTime);
            }
        }

        protected virtual void AttachPotentialItem()
        {
            if (potentialAttech)
            {
                AttachPotentialItem(potentialAttech);
            }
        }

        protected virtual void AttachPotentialItem(Attachment item) //부착품 붙였을때
        {
            interactionVolume.Highlight = item.PrimaryHighlight;

            storedItem = item;
            attachment = item;

            hasExited = false;

            storedItem.Restrained = true;
            storedItem.Col.isTrigger = true;
            storedItem.SetKinematic();
            storedItem.DetachWithOutStoring();

            storedItem.transform.SetParent(transform, true);
            item.Enable();

            TwitchExtension.PlayRandomAudioClip(attachSounds, transform.position);

            if (_OnAttach != null)
            {
                _OnAttach(attachment);
            }

            Timer = Time.time;

            interactionVolume.restrained = false;
        }

        void OnTriggerEnter(Collider other) //트리거 들어갈떄
        {
            if (other.gameObject.tag != "Interactable")
            {
                return;
            }

            Attachment tempAttach = other.GetComponent<Attachment>();

            if (tempAttach)
            {
                if (HasTag(tempAttach) && !attachment) //!potentialAttech
                {
                    potentialAttech = tempAttach;
                }
            }
        }

        void OnTriggerExit(Collider other) //트리거에서 빠져나올때
        {
            if (potentialAttech != null)
            {
                if (other.gameObject == potentialAttech.gameObject)
                {
                    potentialAttech = null;
                    hasExited = true;
                }
            }
        }

        public virtual void GrabAttachment() //부착품 잡기
        {
            if (!interactionVolume.Hand) //interactionVolum에 핸드 설정 안되어있음 리턴
            {
                return;
            }

            if (!storedItem) //부착품 없으면 리턴
            {
                return;
            }

            if (_OnDetach != null)
            {
                _OnDetach(attachment);
            }
                
            attachment.Disable();
            storedItem.Restrained = false;
            storedItem.Attach(interactionVolume.Hand);
            interactionVolume.StopInteraction();

            interactionVolume.Highlight = null;

            storedItem = null;
            attachment = null;

            interactionVolume.restrained = true;

            TwitchExtension.PlayRandomAudioClip(dettachSounds, transform.position);
        }

        public void EnableAttachment()
        {
            attachment.Enable();
        }
        public void DisableAttachment()
        {
            attachment.Disable();
        }
    }
}