using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    [System.Serializable]
    public class RecoilManager //총기반동 매니저
    {

        //반동 포지션 로테이션
        public Transform recoilPositionOffset;
        public Transform recoilRotationOffset;

        [System.Serializable]
        public struct Recoil //리코일액시스에서 가져옴
        {
            public RecoilAxis x;
            public RecoilAxis y;
            public RecoilAxis z;
        }


        [SerializeField] protected Recoil translation;
        [SerializeField] protected Recoil rotation;


        //반동 관련
        public void ClearRecoil()
        {
            translation.x.ResetAxis();
            translation.y.ResetAxis();
            translation.z.ResetAxis();
            rotation.x.ResetAxis();
            rotation.y.ResetAxis();
            rotation.z.ResetAxis();
        }

        public void SetSelf(Item self)
        {
            SetRecoilSelf(ref translation, self);
            SetRecoilSelf(ref rotation, self);
        }

        //스케일
        public void ScaleRotation(float maxDamp, float incrementDamp, float decreaseTargetDamp, float increaseDamp, float decreaseDamp)
        {
            UnscaleRotation();
            ScaleRecoil(maxDamp, incrementDamp, decreaseTargetDamp, increaseDamp, decreaseDamp, ref rotation);
        }

        public void ScaleTranslation(float maxDamp, float incrementDamp, float decreaseTargetDamp, float increaseDamp, float decreaseDamp)
        {
            UnscaleTranslation();
            ScaleRecoil(maxDamp, incrementDamp, decreaseTargetDamp, increaseDamp, decreaseDamp, ref translation);
        }

        public void ScaleRecoil(float maxScale, float incrementScale, float decreaseTargetScale, float increaseScale, float decreaseScale,
                                       ref Recoil recoil)
        {
            ScaleRecoilAxis(maxScale, incrementScale, decreaseTargetScale, increaseScale, decreaseScale, ref recoil.x);
            ScaleRecoilAxis(maxScale, incrementScale, decreaseTargetScale, increaseScale, decreaseScale, ref recoil.y);
            ScaleRecoilAxis(maxScale, incrementScale, decreaseTargetScale, increaseScale, decreaseScale, ref recoil.z);
        }

        void ScaleRecoilAxis(float maxScale, float incrementScale, float decreaseTargetScale, float increaseScale, float decreaseScale,
                         ref RecoilAxis recoil)
        {
            recoil.twoHandMaxScale *= maxScale;
            recoil.twoHandIncrementScale *= incrementScale;
            recoil.twoHandDecreaseTargetScale *= decreaseTargetScale;
            recoil.twoHandIncreaseScale *= increaseScale;
            recoil.twoHandDecreaseScale *= decreaseScale;
        }

        public void UnscaleRotation()
        {
            UnscaleRecoil(ref rotation);
        }

        public void UnscaleTranslation()
        {
            UnscaleRecoil(ref translation);
        }

        public void UnscaleRecoil(ref Recoil recoil)
        {
            UnscaleRecoilAxis(ref recoil.x);
            UnscaleRecoilAxis(ref recoil.y);
            UnscaleRecoilAxis(ref recoil.z);
        }

        public void UnscaleRecoilAxis(ref RecoilAxis recoil)
        {
            recoil.SetInitial();
        }

        public void ApplyRecoil()
        {
            if (!recoilPositionOffset)
            {
                return;
            }

            ApplyRecoil(ref translation);
            ApplyRecoil(ref rotation);

            recoilPositionOffset.localPosition = new Vector3(translation.x.Current, translation.y.Current, translation.z.Current);
            recoilRotationOffset.localRotation = Quaternion.Euler(new Vector3(rotation.x.Current, rotation.y.Current, rotation.z.Current));

            DecreaseRecoil(ref translation);
            DecreaseRecoil(ref rotation);
        }

        void ApplyRecoil(ref Recoil recoil)
        {
            recoil.x.ApplyRecoil();
            recoil.y.ApplyRecoil();
            recoil.z.ApplyRecoil();
        }

        public void IncreaseAllRecoil()
        {
            IncreaseRecoil(ref translation);
            IncreaseRecoil(ref rotation);
        }

        void IncreaseRecoil(ref Recoil recoil)
        {
            recoil.x.IncreaseRecoil();
            recoil.y.IncreaseRecoil();
            recoil.z.IncreaseRecoil();
        }

        void DecreaseRecoil(ref Recoil recoil)
        {
            recoil.x.DecreaseRecoil();
            recoil.y.DecreaseRecoil();
            recoil.z.DecreaseRecoil();
        }

        void SetRecoilSelf(ref Recoil recoil, Item self)
        {
            recoil.x.self = self;
            recoil.y.self = self;
            recoil.z.self = self;

            recoil.x.Initialize();
            recoil.y.Initialize();
            recoil.z.Initialize();
        }
    }
}