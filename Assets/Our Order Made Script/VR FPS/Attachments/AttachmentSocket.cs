using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    public class AttachmentSocket : Slot
    {
        [ReadOnly] [SerializeField] protected Attachment attachment;

        [SerializeField] InteractionVolume interactionVolume;

        float setPositionTime;
        float setRotationTime;

        protected float Timer;

        public delegate void OnAttach(Attachment attachment);
        public OnAttach _OnAttach;

        public delegate void OnDetach(Attachment attachment);
        public OnDetach _OnDetach;

        protected Attachment potentialAttachment;

        [SerializeField] protected float attachDistance = 0.1f;
        [Range(-1, 1)] [SerializeField] protected float primaryAttachDot;
        [Range(-1, 1)] [SerializeField] protected float secondaryAttachDot;

        public TwitchExtension.DotAxis primaryAttachmentSocketDotAxis = TwitchExtension.DotAxis.forward;
        public TwitchExtension.DotAxis secondaryAttachmentSocketDotAxis = TwitchExtension.DotAxis.right;

        [SerializeField] protected List<AudioClip> attachSounds = new List<AudioClip>();
        [SerializeField] protected List<AudioClip> dettachSounds = new List<AudioClip>();
        bool hasExited;

        protected virtual void Start()
        {
            gameObject.layer = LayerMask.NameToLayer("ItemDetection");

            if (!interactionVolume)
                interactionVolume = GetComponent<InteractionVolume>();

            interactionVolume._StartInteraction += GrabAttachment;

            setPositionTime = setPositionTime <= 0 ? 1 : setPositionTime;
            setRotationTime = setRotationTime <= 0 ? 1 : setRotationTime;

            Attachment tempAttachment = GetComponentInChildren<Attachment>();

            if (tempAttachment)
            {
                if (!storedItem)
                {
                    storedItem = tempAttachment;
                    storedItem.Restrained = true;
                    storedItem.SetKinematic();
                }

                if (!attachment)
                {
                    attachment = tempAttachment;
                    potentialAttachment = attachment;
                    AttachPotentialItem();
                }
            }

            interactionVolume.GetComponent<Collider>().isTrigger = true;
            hasExited = !storedItem;

            if (!attachment)
                interactionVolume.restrained = true;
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (potentialAttachment && !storedItem && hasExited)
                if (potentialAttachment.PrimaryHand ^ potentialAttachment.SecondaryHand)
                    if (Vector3.Distance(transform.position, potentialAttachment.transform.position) <= attachDistance)
                    {
                        Vector3 tempPrimaryAxis = TwitchExtension.ReturnAxis(primaryAttachmentSocketDotAxis, transform);
                        Vector3 tempSecondaryAxis = TwitchExtension.ReturnAxis(secondaryAttachmentSocketDotAxis, transform);

                        Vector3 tempAttachmentPrimaryAxis = TwitchExtension.ReturnAxis(potentialAttachment.primaryAttachDotAxis, potentialAttachment.transform);
                        Vector3 tempAttachmentSecondaryAxis = TwitchExtension.ReturnAxis(potentialAttachment.secondaryAttachDotAxis, potentialAttachment.transform);

                        if (Vector3.Dot(tempPrimaryAxis, tempAttachmentPrimaryAxis) >= primaryAttachDot &&
                            Vector3.Dot(tempSecondaryAxis, tempAttachmentSecondaryAxis) >= secondaryAttachDot)
                        {
                            AttachPotentialItem();
                        }
                    }

            if (storedItem != null && Time.time - Timer > 0)
            {
                storedItem.transform.localPosition = Vector3.Lerp(storedItem.transform.localPosition,
                                                          attachment.AttachmentPosOffset,
                                                          (Mathf.Pow(Time.time - Timer, 1.5f)) / setPositionTime);

                storedItem.transform.localRotation = Quaternion.Slerp(storedItem.transform.localRotation,
                                                          Quaternion.Euler(attachment.AttachmentRotOffset),
                                                          (Mathf.Pow(Time.time - Timer, 1.5f)) / setRotationTime);
            }
        }

        protected virtual void AttachPotentialItem() { if (potentialAttachment) AttachPotentialItem(potentialAttachment); }

        protected virtual void AttachPotentialItem(Attachment item)
        {
            interactionVolume.Highlight = item.PrimaryHighlight;

            storedItem = item;
            attachment = item;

            hasExited = false;

            storedItem.Restrained = true;
            storedItem.DetachWithOutStoring();
            storedItem.SetKinematic();
            Destroy(storedItem.Rb);
            //storedItem.Col.isTrigger = true;

            storedItem.transform.SetParent(transform, true);
            item.Enable();

            TwitchExtension.PlayRandomAudioClip(attachSounds, transform.position);

            if (_OnAttach != null)
                _OnAttach(attachment);

            Timer = Time.time;

            interactionVolume.restrained = false;
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.tag != "Interactable")
                return;

            Attachment tempAttachment = other.GetComponent<Attachment>();

            if (!grabbedAttachment)
                if (tempAttachment)
                    if (HasTag(tempAttachment) && !attachment) //!potentialAttachment
                        potentialAttachment = tempAttachment;
        }

        IEnumerator DelayReattach()
        {
            grabbedAttachment = true;
            yield return new WaitForFixedUpdate();
            grabbedAttachment = false;
        }

        void OnTriggerExit(Collider other)
        {
            if (potentialAttachment != null)
                if (other.gameObject == potentialAttachment.gameObject)
                {
                    potentialAttachment = null;
                    hasExited = true;
                }
        }

        bool grabbedAttachment;

        public virtual void GrabAttachment()
        {
            if (!storedItem)
                return;

            if (!interactionVolume.Hand)
                return;

            if (_OnDetach != null)
                _OnDetach(attachment);

            if (!storedItem.Rb) storedItem.rb = storedItem.gameObject.AddComponent<Rigidbody>();

            attachment.Disable();
            storedItem.Restrained = false;
            storedItem.Attach(interactionVolume.Hand);
            interactionVolume.StopInteraction();

            interactionVolume.Highlight = null;

            storedItem = null;
            attachment = null;

            interactionVolume.restrained = true;

            TwitchExtension.PlayRandomAudioClip(dettachSounds, transform.position);
            StartCoroutine(DelayReattach());
        }

        public void EnableAttachment() { attachment.Enable(); }
        public void DisableAttachment() { attachment.Disable(); }
    }
}