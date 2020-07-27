using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace VrFps
{
    [PunRPC]
    public class Chamber : MonoBehaviourPunCallbacks
    {
        public delegate void ChamberEvent(Chamber chamber);
        public ChamberEvent _LoadBullet;

        [SerializeField] protected List<AudioClip> ejectBulletSounds = new List<AudioClip>(); //쏜 탄 배출할때 소리
        [SerializeField] protected List<AudioClip> loadBulletSounds = new List<AudioClip>(); //탄약실에 총알 장전 되는 소리

        [ReadOnly] [SerializeField] protected Bullet bullet; //총알객체
        public Bullet Bullet
        {
            get
            {
                return bullet;
            }
        }
        [SerializeField] protected string bulletTag;

        protected Bullet potentialBullet;
        [SerializeField] protected float loadBulletDistance = 0.15f;
        [SerializeField] protected float loadBulletDot = 0.75f;

        [SerializeField] protected bool requireHeldBullets = true;

        [SerializeField] protected float ejectForce; //탄피가 나가는 방향과 속도
        [SerializeField] protected float ejectTorque; 

        [SerializeField] protected Transform eject; //탄피가 배출되는 위치

        [SerializeField] protected DotAxis chamberDotAxis;
        [SerializeField] protected DotAxis bulletDotAxis;

        public enum DotAxis 
        {
            right,
            left,
            up,
            down,
            forward,
            back
        }

        Vector3 ReturnAxis(DotAxis axis, Transform transform)
        {
            switch (axis)
            {
                case DotAxis.forward:
                    return transform.forward;
                case DotAxis.right:
                    return transform.right;
                case DotAxis.up:
                    return transform.up;
                case DotAxis.back:
                    return -transform.forward;
                case DotAxis.left:
                    return -transform.right;
                case DotAxis.down:
                    return -transform.up;
            }

            return Vector3.zero;
        }

        void Start()
        {
            gameObject.layer = LayerMask.NameToLayer("ItemDetection"); //레이어마스크 설정

            if (eject == null)//이젝트가 없을경우 이젝트를 챔버 위치로
            {
                eject = transform;
            }

            if (bullet)
            {
                bullet.Restrained = true;
                bullet.Col.enabled = false;
                return;
            }

            Bullet tempBullet = GetComponentInChildren<Bullet>(); //총알객체

            if (!tempBullet)
            {
                return;
            }
            ChamberBullet(tempBullet);
        }

        public virtual void ChamberBullet(Bullet bullet) //약실에 총알 
        {
            if (!bullet)
            {
                return;
            }
            if (bullet.Spent)
            {
                return;
            }

            Hand bulletHand = bullet.PrimaryHand;

            bullet.dropStack = false;
            bullet.DetachWithOutStoring();
            bullet.dropStack = true;
            bullet.Restrained = true;

            bullet.SetPhysics(true, false, true, true);

            bullet.transform.SetParent(transform, true);
            bullet.transform.localPosition = Vector3.zero;
            bullet.transform.localEulerAngles = Vector3.zero;

            bullet.GetComponentInChildren<MeshRenderer>().enabled = true;

            this.bullet = bullet;
            potentialBullet = null;

            bullet.chambered = true;

            if (bulletHand)
            {
                if (bulletHand.GetType() == typeof(Hand))
                {
                    (bulletHand as Hand).GrabFromStack();
                }
            }

            TwitchExtension.PlayRandomAudioClip(loadBulletSounds, transform.position);
        }

        [SerializeField] protected DotAxis ejectDirection;
        [SerializeField] protected DotAxis torqueDirection;

        public virtual void EjectBullet() { EjectBullet(Vector3.zero); } 

        public virtual void EjectBullet(Vector3 additionalVelocity) //총알 배출
        {
            if (!bullet)
            {
                return;
            }

            bullet.transform.SetParent(null, true);
            bullet.transform.position = eject.position;
            bullet.SetPhysics(false, true, true, false);

            bullet.Restrained = bullet.Spent;

            //총알이 배출되는 방향, 힘 설정
            bullet.Rb.AddForce((ReturnAxis(ejectDirection, eject) * ejectForce) + additionalVelocity, ForceMode.Impulse);
            bullet.Rb.maxAngularVelocity = ejectTorque;
            bullet.Rb.AddTorque(ReturnAxis(torqueDirection, eject) * ejectTorque, ForceMode.VelocityChange);
           

            bullet.chambered = false;

            //배출된 총알 일정시간후 삭제
            if (bullet.Spent)
            {
                Destroy(bullet.gameObject, 5f);
            }
            

            bullet = null;

            TwitchExtension.PlayRandomAudioClip(ejectBulletSounds, transform.position);
        }


        public virtual void EjectBulletLight()
        {
            if (!bullet)
            {
                return;
            }
            bullet.chambered = false;
            bullet = null;
        }

        //약실로 총알 장전
        protected virtual void OnTriggerEnter(Collider _potentialBullet) 
        {
            if (bullet)
            {
                return;
            }

            if (_potentialBullet.gameObject.tag != "Interactable")
            {
                return;
            }

            Bullet tempBullet = _potentialBullet.GetComponent<Bullet>();

            if (tempBullet)
            {
                if (tempBullet.HasTag(bulletTag))
                {
                    bool tempHeld = requireHeldBullets ? (tempBullet.PrimaryHand ?? tempBullet.SecondaryHand) || tempBullet.Held : true;

                    if (tempHeld && !tempBullet.Spent)
                    {
                        potentialBullet = tempBullet;
                    }
                }
            }
        }

        void FixedUpdate()
        {
            if (potentialBullet)
            {
                _LoadBullet(this);
            }
        }

       
        public virtual void LoadPotentialBullet()
        {
            if (bullet || !potentialBullet)
            {
                return;
            }

            if (potentialBullet.chambered)
            {
                return;
            }

            if (requireHeldBullets)
            {
                if (!(potentialBullet.PrimaryHand ^ potentialBullet.SecondaryHand) && !potentialBullet.Held)
                {
                    return;
                }
            }
            if (Vector3.Distance(potentialBullet.transform.position, transform.position) > loadBulletDistance)
            {
                return;
            }
            Vector3 chamberAxis = ReturnAxis(chamberDotAxis, transform);
            Vector3 bulletAxis = ReturnAxis(bulletDotAxis, potentialBullet.transform);

            if (Vector3.Dot(chamberAxis, bulletAxis) < loadBulletDot)
            {
                return;
            }

            ChamberBullet(potentialBullet);
        }

        protected virtual void OnTriggerExit(Collider _potentialBullet)
        {
            if (potentialBullet)
            {
                if (potentialBullet.transform == _potentialBullet.transform)
                {
                    potentialBullet = null;
                }
            }
        }
    }
}