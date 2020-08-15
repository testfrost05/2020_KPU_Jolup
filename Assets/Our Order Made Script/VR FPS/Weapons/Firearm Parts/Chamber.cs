using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    public class Chamber : MonoBehaviour
    {
        public delegate void ChamberEvent(Chamber chamber);
        public ChamberEvent _LoadBullet;

        [SerializeField] protected List<AudioClip> ejectBulletSounds = new List<AudioClip>();
        [SerializeField] protected List<AudioClip> loadBulletSounds = new List<AudioClip>();

        [ReadOnly] [SerializeField] protected Bullet bullet;
        public Bullet Bullet { get { return bullet; } }
        [SerializeField] protected string[] bulletTags;

        protected Bullet potentialBullet;
        [SerializeField] protected float loadBulletDistance = 0.15f;
        [SerializeField] protected float loadBulletDot = 0.75f;

        [SerializeField] protected bool requireHeldBullets = true;

        [SerializeField] protected float ejectForce;
        [SerializeField] protected float ejectTorque;

        [SerializeField] protected Transform eject;

        [SerializeField] protected DotAxis chamberDotAxis;
        [SerializeField] protected DotAxis bulletDotAxis;

        [SerializeField] protected InteractionVolume interactionVolume;
        public InteractionVolume IV { get { return interactionVolume; } }

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

        void RestrainChamber()
        {
            if (interactionVolume)
                interactionVolume.restrained = true;
        }

        void UnrestrainChamber()
        {
            if (interactionVolume)
                interactionVolume.restrained = false;
        }

        void Start()
        {

            interactionVolume = GetComponent<InteractionVolume>();

            if (interactionVolume)
                interactionVolume._StartInteraction += GrabBullet;

            gameObject.layer = LayerMask.NameToLayer("ItemDetection");

            if (eject == null)
                eject = transform;

            if (bullet)
            {
                bullet.Restrained = true;
                bullet.Col.enabled = false;
                return;
            }

            Bullet tempBullet = GetComponentInChildren<Bullet>();

            if (!tempBullet)
                return;

            ChamberBullet(tempBullet);
        }

        void GrabBullet()
        {

            if (!bullet) return;

            Hand tempHand = interactionVolume.Hand;

            Bullet tempBullet = bullet;


            interactionVolume.StopInteraction();

            EjectBullet();

            tempBullet.Restrained = false;
            tempBullet.Attach(tempHand);

        }

        public virtual void ChamberBullet(Bullet bullet)
        {
            if (!bullet) return;

            if (bullet.Spent) return;

            Hand bulletHand = bullet.PrimaryHand;

            bullet.dropStack = false;
            bullet.DetachWithOutStoring();
            bullet.dropStack = true;
            bullet.Restrained = true;

            bullet.SetPhysics(true, false, true, true);

            bullet.transform.SetParent(transform, true);
            bullet.transform.localPosition = Vector3.zero;
            bullet.transform.localEulerAngles = Vector3.zero;

            bullet.MeshRenderer.enabled = true;

            this.bullet = bullet;
            potentialBullet = null;

            bullet.chambered = true;

            if (bulletHand)
                if (bulletHand.GetType() == typeof(Hand))
                    (bulletHand as Hand).GrabFromStack();

            TwitchExtension.PlayRandomAudioClip(loadBulletSounds, transform.position);

            UnrestrainChamber();
        }

        [SerializeField] protected DotAxis ejectDirection;
        [SerializeField] protected DotAxis torqueDirection;

        public virtual void EjectBullet() { EjectBullet(Vector3.zero); }

        public virtual void EjectBullet(Vector3 additionalVelocity)
        {
            if (!bullet)
                return;

            if (!bullet.Rb)
                bullet.rb = bullet.gameObject.AddComponent<Rigidbody>();

            bullet.transform.SetParent(null, true);
            bullet.transform.position = eject.position;
            bullet.SetPhysics(false, true, true, false);

            bullet.Restrained = bullet.Spent;

            bullet.Rb.AddForce((ReturnAxis(ejectDirection, eject) * ejectForce) + additionalVelocity, ForceMode.Impulse);
            bullet.Rb.maxAngularVelocity = ejectTorque;
            bullet.Rb.AddTorque(ReturnAxis(torqueDirection, eject) * ejectTorque, ForceMode.VelocityChange);

            bullet.chambered = false;

            if (bullet.Spent)
                Destroy(bullet.gameObject, 5f);

            bullet = null;

            TwitchExtension.PlayRandomAudioClip(ejectBulletSounds, transform.position);
            RestrainChamber();
        }


        public virtual void EjectBulletLight()
        {
            if (!bullet) return;

            bullet.chambered = false;
            bullet = null;

            RestrainChamber();
        }

        protected virtual void OnTriggerEnter(Collider _potentialBullet)
        {
            if (bullet)
                return;

            if (_potentialBullet.gameObject.tag != "Interactable")
                return;

            Bullet tempBullet = _potentialBullet.GetComponent<Bullet>();

            if (tempBullet)
                if (ItemHasAcceptedTag(tempBullet))
                {
                    bool tempHeld = requireHeldBullets ? (tempBullet.PrimaryHand ?? tempBullet.SecondaryHand) || tempBullet.Held : true;

                    if (tempHeld && !tempBullet.Spent)
                    {
                        potentialBullet = tempBullet;
                    }
                }
        }

        bool ItemHasAcceptedTag(Item item)
        {
            foreach (string tag in bulletTags)
                if (item.HasTag(tag)) return true;

            return false;
        }

        void FixedUpdate()
        {
            if (potentialBullet)
                _LoadBullet(this);
        }

        public virtual void LoadPotentialBullet()
        {
            if (bullet || !potentialBullet)
                return;

            if (potentialBullet.chambered)
                return;

            if (requireHeldBullets)
                if (!(potentialBullet.PrimaryHand ^ potentialBullet.SecondaryHand) && !potentialBullet.Held)
                    return;

            if (Vector3.Distance(potentialBullet.transform.position, transform.position) > loadBulletDistance)
                return;

            Vector3 chamberAxis = ReturnAxis(chamberDotAxis, transform);
            Vector3 bulletAxis = ReturnAxis(bulletDotAxis, potentialBullet.transform);

            if (Vector3.Dot(chamberAxis, bulletAxis) < loadBulletDot)
                return;

            ChamberBullet(potentialBullet);
        }

        protected virtual void OnTriggerExit(Collider _potentialBullet)
        {
            if (potentialBullet)
                if (potentialBullet.transform == _potentialBullet.transform)
                    potentialBullet = null;
        }
    }
}