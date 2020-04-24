using UnityEngine;
using System;

[AddComponentMenu("FragChild")]

public class FraggedChild : MonoBehaviour //파괴효과 
{

    int forceMax;
    int forceMin;
    [HideInInspector]//인스펙터창에서 변수 숨기기
    public bool fragged;

    //트랜스폼 정보 저장
    Vector3 sPos;
    Quaternion sRot;
    Vector3 sScale;


    FraggedController fragControl; //FraggedController 스크립트 관련



    [HideInInspector]
    public float hitPoints = 1.0f;
    public bool stickyFrag;
    [HideInInspector]
    public bool connected = true;
    [HideInInspector]
    public bool released;

    bool checkToggle = true;

    public Rigidbody cacheRB; //리기드바디

    //대미지를 받았을 때 조각 나가떨어지게
    //대미지 주는 건 gameObject.SendMessage("Damage", 1f, SendMessageOptions.DontRequireReceiver); <- 요런식으로 사용하면됨
    public void Damage(float damage)
    {
        fragMe(fragControl.hitPointDecrease * damage);

        if (fragControl.fragAllOnDamage)
        {
            fragControl.FragAll();
        }
    }

    public void Start()
    {
        cacheRB = GetComponent<Rigidbody>();
        cacheRB.isKinematic = true;
        if (fragControl == null)
        {
            fragControl = transform.parent.parent.GetComponent<FraggedController>();
        }
        GetComponent<Renderer>().enabled = false;
        fragControl = gameObject.transform.parent.parent.GetComponent<FraggedController>();
        forceMax = (int)(fragControl.forceMax * fragControl.fragMass);
        forceMin = (int)(fragControl.forceMin * fragControl.fragMass);
        sRot = transform.rotation;
        sPos = transform.position;
        sScale = transform.localScale;
        cacheRB.mass = fragControl.fragMass;

    }



    public void checkConnections() //연결 확인
    {
        if (!stickyFrag && !this.fragged && !this.connected && (fragControl.stickyTop > 0 || fragControl.stickyBottom > 0))
        {
            int counter = 0;
            Collider[] colls = null;
            colls = Physics.OverlapSphere(transform.position, fragControl.connectOverlapSphere * fragControl.transform.localScale.x, fragControl.stickyMask);
            //OverlapSphere -> 가상의 원을 만들어 추출하려는 반경안의 콜라이더들을 반환 
            for (int i = 0; i <= colls.Length - 1; i++)
            {
                FraggedChild frag = colls[i].transform.GetComponent<FraggedChild>();
                if (frag != null && !frag.fragged && transform.parent == frag.transform.parent)
                {
                    if (frag.stickyFrag || frag.connected)
                    {
                        counter++;
                    }
                }
            }
            if (counter >= fragControl.connectedFragments)
            {
                connected = true;
            }
        }
    }

    //충돌 시작
    public void OnCollisionEnter(Collision collision)
    {
        if ((fragControl.collideMask.value & 1 << collision.gameObject.layer) == 1 << collision.gameObject.layer) //
        {
            if (this.fragControl.collidefragMagnitude > 0 && collision.relativeVelocity.magnitude >
                this.fragControl.collidefragMagnitude)
            {
                fragMe(collision.relativeVelocity.magnitude * .2f * fragControl.hitPointDecrease);
            }

            if (this.fragged && collision.relativeVelocity.magnitude > 1) //파티클
            {
                fragControl.dustParticles.transform.position = this.transform.position;
                fragControl.dustParticles.Emit(1);
                fragControl.fragParticles.transform.position = this.transform.position;
                fragControl.fragParticles.Emit((int)UnityEngine.Random.Range(fragControl.fragEmitMinMax.x * .5f,
                    fragControl.fragEmitMinMax.y * .5f));
            }
        }

        if (fragControl.disableDelay > 0 && (fragControl.disableMask.value & 1 << collision.gameObject.layer)
            == 1 << collision.gameObject.layer)
        {
            Invoke("Disable", fragControl.disableDelay);
        }

    }



    public void addForce(int fMin, int fMax) //날라가는 힘
    {

        if (!cacheRB.isKinematic && this.cacheRB.velocity.magnitude < 1)
        {
            float forceX = (float)UnityEngine.Random.Range(fMin, fMax);

            if (UnityEngine.Random.value > 0.5f)
            {
                forceX *= -1.0f;
            }

            float forceY = (float)UnityEngine.Random.Range(fMin, fMax);

            if (UnityEngine.Random.value > 0.5f)
            {
                forceY *= -1.0f;
            }


            cacheRB.velocity = new Vector3(forceX, (float)UnityEngine.Random.Range(fMin, fMax), forceY) * .05f;
        }
    }

    public void fragMe(float hitFor) //
    {
        if ((fragControl.startMesh != null) && fragControl.startMesh.GetComponent<Renderer>().enabled)
        {
            fragControl.startMesh.GetComponent<Renderer>().enabled = false;
            fragControl.EnableRenderers();
        }
        fragControl.ReleaseFrags(false);
        fragControl.reCounter = 0;

        if (this.connected)
        {
            addForce(forceMin, forceMax);

            if (fragged && checkToggle)
            {
                checkToggle = !checkToggle;
                fragControl.checkConnections();
                this.connected = false;
            }
        }
        else if (hitFor < 200)
        {
            addForce((int)(forceMin * .5f), (int)(forceMax * .5f));
        }

        if (!fragged)
        {
            hitPoints -= hitFor;
            if (fragControl.fragEnabled && hitPoints < 0) //히트포인트가 0미만이면
            {
                MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>(); //메쉬생성
                if (meshCollider != null)
                {
                    meshCollider.convex = true;
                }
                fragged = true;

                if (fragControl.fragParticles != null) //파티클이 있으면 파티클 플레이
                {
                    fragControl.fragParticles.transform.position = this.transform.position;
                    if (this.connected)
                    {
                        fragControl.fragParticles.Emit((int)UnityEngine.Random.Range(fragControl.fragEmitMinMax.x,
                        fragControl.fragEmitMinMax.y));
                    }
                    else
                    {
                        fragControl.fragParticles.Emit((int)(UnityEngine.Random.Range(fragControl.fragEmitMinMax.x,
                            fragControl.fragEmitMinMax.y) * .5f));
                    }
                }

                transform.localScale = sScale * fragControl.fragOffScale;
                cacheRB.isKinematic = false;
                released = true;
            }

            else if (hitFor < 1 && hitPoints > 0) //히트포인트가 0 초과일경우
            {
                if (!this.released)
                {
                    float rotateMultiplier = 1 - hitPoints;
                    gameObject.transform.Rotate(UnityEngine.Random.Range(-fragControl.rotateOnHit, fragControl.rotateOnHit + 1)
                        * rotateMultiplier, 0.0f, UnityEngine.Random.Range(-fragControl.rotateOnHit, fragControl.rotateOnHit + 1)
                        * rotateMultiplier);
                    transform.localEulerAngles = new Vector3((float)Mathf.Clamp((int)transform.localEulerAngles.x, -10, 10),
                        (float)Mathf.Clamp((int)transform.localEulerAngles.y, -10, 10),
                        (float)Mathf.Clamp((int)transform.localEulerAngles.z, -10, 10));
                }

                if (fragControl.dustParticles != null) //더스트 파티클 플레이
                {
                    fragControl.dustParticles.transform.position = this.transform.position;
                    fragControl.dustParticles.Emit(UnityEngine.Random.Range(3, 8));
                }
            }
            else
            {
                gameObject.transform.Rotate((float)UnityEngine.Random.Range(-fragControl.rotateOnHit, fragControl.rotateOnHit + 1), 0.0f, 0.0f);
            }
        }
    }

    public void SpeedCheck()
    {
        if (fragged && cacheRB.velocity.sqrMagnitude > fragControl.limitFragmentSpeed)
        {
            cacheRB.velocity = Vector3.zero;
        }
    }

    public void Disable()
    {
        gameObject.GetComponent<Collider>().enabled = false;
        MeshCollider MC = GetComponent<MeshCollider>();
        if (MC != null)
        {
            MC.enabled = false;
        }
        cacheRB.isKinematic = true;
    }

    public void resetMe() //리셋
    {
        transform.gameObject.SetActive(true);
        transform.GetComponent<Renderer>().enabled = false;
        MeshCollider MC = GetComponent<MeshCollider>();
        if (MC != null)
        {
            MC.convex = false;
            MC.enabled = true;
        }
        transform.position = sPos;
        transform.rotation = sRot;
        transform.localScale = sScale;
        fragged = false;
        hitPoints = 1.0f;
        cacheRB.isKinematic = true;
        connected = true;
        released = false;
    }
}
