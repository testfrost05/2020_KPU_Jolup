using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
namespace VrFps
{
    public class Projectile : MonoBehaviourPunCallbacks //총알을 쏠 때
    {
        [SerializeField] protected Rigidbody rb;
        [SerializeField] protected Collider col;

        public Collider Col
        {
            get
            {
                return col;
            }
        }

        [SerializeField] protected float muzzleVelocity = 169f; //머즐 속도

        [SerializeField] protected float forceDamp = 10f; 

        [SerializeField] protected float baseDamage = 100; //대미지
        [SerializeField] protected float maxDistance = 100; //최대 거리

        [SerializeField] protected Explosive explosive; //수류탄이나 바주카 같은거
        [SerializeField] protected float extraForce; 
        [SerializeField] protected float trauma = 0.25f;

        public Transform[] unchildOnCollision;

        [SerializeField] protected float pellets;
        [SerializeField] protected bool destroyOnImpact = true;
        [SerializeField] protected float destroyDelay = 0.05f; 

        [System.Serializable]

        public struct BulletImpactEffect //총알 임펙트, 소리 관련
        {
            public string hitObjectTag; 
            public GameObject impactParticle; 
            public AudioClip[] impactAudio; 
            public GameObject exitParticle;
            public AudioClip[] exitAudio;
            public float scale;
        }

        [SerializeField] protected List<BulletImpactEffect> impactEffects;

        void Start()
        {
            Destroy(gameObject, 7.5f);
        }

        [PunRPC]
        void OnCollisionEnter(Collision col) //충돌 진입
        {
            Effect(col, true);

            if (explosive) //폭발물일경우
            {
                explosive.enabled = true;
            }

            foreach (Transform child in unchildOnCollision)
            {
                child.parent = null;
            }

            col.gameObject.SendMessage("Damage", 3f, SendMessageOptions.DontRequireReceiver); //3f 이상으로하면 벽 파괴된다.
            
            

            HealthManager healthManager = col.transform.GetComponent<HealthManager>();
            if (healthManager)
            {
                healthManager.ApplyDamage(baseDamage);
            }
           
            if (destroyOnImpact)
            {
                if (destroyDelay != 0)
                {
                    Destroy(gameObject, destroyDelay);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }

        public void Fire() //총을 쐈을 때 
        {
            rb.AddForce(transform.forward * muzzleVelocity, ForceMode.Impulse);
            Destroy(gameObject, 7.5f);

            GameObject clone = gameObject;

            for (int i = 0; i < pellets; i++)
            {
                Instantiate(gameObject, transform.position, transform.rotation);
                Destroy(clone, 7.5f);
            }
        }

        public void Fire(float muzzleVelocity, float spread) //총을 쐇을 때 탄 퍼짐 적용
        {
            rb.AddForce(transform.forward * muzzleVelocity, ForceMode.Impulse);
            Destroy(gameObject, 7.5f);

            Projectile clone = this;
            Vector3 spreadV = Vector2.zero;

            for (int i = 0; i < pellets; i++)
            {
                clone = Instantiate(clone, transform.position, transform.rotation);
                spreadV.x = Random.Range(-spread, spread); //탄퍼짐은 랜덤하게 
                spreadV.y = Random.Range(-spread, spread);
                spreadV.z = Random.Range(-spread, spread);
                clone.rb.AddForce((transform.forward * muzzleVelocity) + spreadV, ForceMode.Force);
                Destroy(clone, 7.5f);
            }
        }

        void Effect(Collision col, bool ENTEREXIT) //임펙트관련
        {
            for (int i = 0; i < impactEffects.Count; i++)
            {
                BulletImpactEffect impactEffect = impactEffects[i];

                if (col.gameObject.tag == impactEffect.hitObjectTag)
                {
                    GameObject clone = null;
                    GameObject particleEffect = ENTEREXIT ? impactEffect.impactParticle : impactEffect.exitParticle;
                    
                    if (particleEffect) //파티클 임펙트
                    {
                        clone = Instantiate(particleEffect,
                            col.contacts[0].point + col.contacts[0].normal * 0.01f,
                            Quaternion.FromToRotation(Vector3.forward, col.contacts[0].normal)) as GameObject;

                        Destroy(clone, 3f);

                        Transform decal = clone.transform.GetChild(0);

                        if (decal)
                        {
                            if (decal.name == "Decal")
                            {
                                decal.SetParent(col.transform, true);
                                Destroy(decal.gameObject, 5f);
                            }
                        }
                    }
                }
            }
        }
    }
}