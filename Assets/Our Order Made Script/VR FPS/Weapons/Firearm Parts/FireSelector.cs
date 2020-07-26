using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Photon.Pun;

namespace VrFps
{
    [PunRPC]
    public class FireSelector : MonoBehaviour
    {
        public enum _FireMode // 총 사격 모드
        {
            safety, //안전 - 총 못씀
            semi, //단발
            full, //연발
            burst //점사
        }
       

        [SerializeField] protected _FireMode fireMode; //사격 모드 설정
        public _FireMode FireMode
        {
            get
            {
                return fireMode;
            }
        }
        [SerializeField] protected List<_FireMode> availableFireModes = new List<_FireMode>();//조정간 변경 가능 모드 배열
        [SerializeField] protected List<Vector3> fireSelectorRotations; //조정간 로테이션 조정

        [SerializeField] protected float safetySlideLimit = -1;
        public float SafetySlideLimit
        {
            get
            {
                return safetySlideLimit;
            }
        }

        [SerializeField] protected VrFpsInput.TouchPadDirection touchpadDirection;
        public VrFpsInput.TouchPadDirection _TouchPadDirection
        {
            get
            {
                return touchpadDirection;
            }
        }

        protected int selectedFireModeIndex;

        [SerializeField] protected List<AudioClip> switchFireModeSounds = new List<AudioClip>(); //사격 모드 바꿀때 나는 소리
        //ex) 군대에서 k1이나 k2 쓸 때 안전 -> 단발로 바꾸면 팅 같은 소리 나는데 그런 소리 넣도록

        [SerializeField] protected SteamVR_Action_Boolean fireSelectorInput; //사격 모드 바꿀 버튼 설정
        public SteamVR_Action_Boolean FireSelectorInput
        {
            get
            {
                return fireSelectorInput;
            }
        }

        void Start()
        {
            selectedFireModeIndex = availableFireModes.IndexOf(fireMode);

            if (availableFireModes.Count != fireSelectorRotations.Count)
            {
                fireSelectorRotations.AddRange(new Vector3[availableFireModes.Count - fireSelectorRotations.Count]);
            }
        }

        public virtual void SwitchFireMode()
        {
            if (availableFireModes.Count - 1 <= 0)
            {
                return;
            }

            selectedFireModeIndex = selectedFireModeIndex < availableFireModes.Count - 1 ? selectedFireModeIndex + 1 : 0;
            fireMode = availableFireModes[selectedFireModeIndex];

            transform.localEulerAngles = fireSelectorRotations[selectedFireModeIndex];
            //조정간 로테이션 변경
            TwitchExtension.PlayRandomAudioClip(switchFireModeSounds, transform.position);
        }

        public _FireMode NextFireMode()
        {
            return availableFireModes[selectedFireModeIndex < availableFireModes.Count - 1 ? selectedFireModeIndex + 1 : 0];
        }
    }
}