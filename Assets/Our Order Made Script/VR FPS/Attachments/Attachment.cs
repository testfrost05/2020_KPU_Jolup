using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

namespace VrFps
{
    public class Attachment : Item // item 클래스 상속
    {
        [SerializeField] protected InteractionVolume useAttachmentIV; 

        [SerializeField] protected List<AudioClip> useSounds = new List<AudioClip>(); //부착할때 소리

        [SerializeField] protected SteamVR_Action_Boolean useAttachmentInput; //부착아이템 관련 버튼키 설정

        public enum Type //부착 아이템 타입 
        {
            sight, //도트사이트
            scope, //배율스코프
            foregrip, //손잡이
            suppressor, // 소음기
            other //기타
        }

        public Type type;
          
        public Vector3 AttachmentPosOffset; // 부착품 위치 
        public Vector3 AttachmentRotOffset; // 부착품 회전

        public TwitchExtension.DotAxis primaryAttachDotAxis = TwitchExtension.DotAxis.forward;
        public TwitchExtension.DotAxis secondaryAttachDotAxis = TwitchExtension.DotAxis.right;

        protected override void Start()
        {
            base.Start();
            if (useAttachmentIV)
            {
                useAttachmentIV._StartInteraction += Use;
            }
        }

        protected override void Update()
        {
            base.Update();

            LocalInputDown(Use, useAttachmentInput);
        }

        public virtual void Enable()
        {

        }

        public virtual void Disable()
        {

        }

        public virtual void Use()
        {
            TwitchExtension.PlayRandomAudioClip(useSounds, transform.position);
        }

    }
}