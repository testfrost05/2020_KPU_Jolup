using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class FraggedController : MonoBehaviour
{
    [Header("Fragments")]
    public bool fragEnabled = true; //파편들이 떨어져 나갈수있는지 없는지
    public int forceMax = 250; //파편이 떨어질떄 주는 최대 힘			
    public int forceMin = 50; //최소힘				
    public float fragOffScale = 1.0f;					//떨어져나간 파편 스케일
    public int rotateOnHit = 10; 					//대미지 받았을떄 랜덤으로 회전하는 정도
    public float fragMass = 0.01f;
    public float hitPointDecrease = .2f;            //각 메쉬의 히트포인트 크기
    public float limitFragmentSpeed = 25.0f;			//파편 날라갈 때 속도 제한
    public bool fragAllOnDamage;                    //부위가 다 대미지 받으면 다 부숨


    [Header("Collisions")]
    public int collidefragMagnitude = 0; 				// 충돌시 파편들 진동 (0 이면 없음, 5가 적당, 25 최대)
    public LayerMask collideMask;                   //레이어마스크

    [Header("Particles")]
    public Vector2 fragEmitMinMax = new Vector2(2.0f, 4.0f);


    [Header("Connections")]
    public int stickyTop;
    public int stickyBottom;
    public int connectedFragments = 3; 			//주위가 없어서 안떨어지기 위한 주위 최소 파편수
    public float connectOverlapSphere = .5f; 		//OverlapSphere 크기 설정
    public LayerMask stickyMask = (LayerMask)1;             //레이어마스크 설정

    [Header("After Fragment")]
    public LayerMask disableMask;					//대미지 받은 부분 레이어 마스크
    public float disableDelay = 0.0f; 				//안보이는 정도
    public bool combineFrags = true;
    public int combineMeshesDelay = 3;

    [HideInInspector] //인스펙터창에서 변수 숨기기
    public Transform startMesh; 					//시작 메쉬
    [HideInInspector]
    public ParticleSystem fragParticles; 			//파편 떨어져 나갈 때 나오는 파티클
    [HideInInspector]
    public ParticleSystem dustParticles; 			//대미지 받을 때마다 나오는 파티클
    [HideInInspector]
    public int reCounter = 1;
    [HideInInspector]
    public GameObject combinedFrags;
    [HideInInspector]
    public MeshFilter[] meshFilters;
    [HideInInspector]
    public Transform fragments;

    [Header("Change Materials")]
    public Material[] fragMaterials;


    public void ResetFrags() //리셋 오브젝트
    {
        if (startMesh != null)
        {
            this.startMesh.GetComponent<Renderer>().enabled = true;
        }
        fragParticles.Clear();
        foreach (Transform child in fragments)
        {
            child.GetComponent<FraggedChild>().resetMe();
        }
        if (combinedFrags != null)
        {
            Destroy(combinedFrags);
        }
        if (startMesh == null)
        {
            reCombine();
        }
    }

    public void Start()
    {
        fragParticles = transform.FindChild("Particles Fragment").GetComponent<ParticleSystem>();
        fragParticles.Stop();
        dustParticles = transform.FindChild("Particles Dust").GetComponent<ParticleSystem>();
        dustParticles.Stop();
        startMesh = transform.FindChild("Original Mesh");
        fragments = transform.FindChild("Fragments");
        meshFilters = new MeshFilter[fragments.transform.childCount];
        meshFilters = fragments.transform.GetComponentsInChildren<MeshFilter>(true);
        FindSticky();
        if (startMesh == null)
        {
            CombineFrags();
        }
        InvokeRepeating("reCombine", 1.0f, 1.0f);
        ChangeMaterials();
    }

    public void ChangeMaterials() //마테리얼 변경
    {
        for (int m = 0; m < fragMaterials.Length; m++)
        {
            for (int i = 0; i < fragments.childCount; i++)
            {
                Renderer child = fragments.GetChild(i).GetComponent<Renderer>();
                child.sharedMaterials = fragMaterials;
            }
        }
    }

    public void FixedUpdate()
    {
        if (limitFragmentSpeed > 0 && (combinedFrags == null) && !this.startMesh.GetComponent<Renderer>().enabled)
        {
            for (int i = fragments.childCount; i > 0; i--)
            {
                FraggedChild child = fragments.GetChild(i - 1).GetComponent<FraggedChild>();
                child.SpeedCheck();
            }
        }
    }

    public int Compare(float first, float second) //비교
    {
        return second.CompareTo(first);
    }

    public void FragAll() //자식 파편들
    {
        for (int i = fragments.transform.childCount; i > 0; i--)
        {
            FraggedChild child = fragments.GetChild(i - 1).GetComponent<FraggedChild>();
            child.fragMe(200.0f);
        }
    }

    public void checkConnections() //연결 확인
    {
        if (stickyTop > 0 || stickyBottom > 0)
        {

            FraggedChild frag = null;
            for (int i = stickyTop; i < meshFilters.Length; i++)
            {
                frag = meshFilters[i].GetComponent<FraggedChild>();
                frag.connected = false;
            }

            for (int j = stickyTop; j < meshFilters.Length; j++)
            {
                frag.checkConnections();
                frag = meshFilters[j].GetComponent<FraggedChild>();
            }

            for (int u = meshFilters.Length - stickyBottom - 1; u >= stickyTop; u--)
            {
                frag = meshFilters[u].GetComponent<FraggedChild>();
                if (!frag.fragged)
                {
                    frag.checkConnections();
                    if (!frag.connected)
                    {
                        frag.fragMe(2.0f);
                    }
                }
            }
        }
    }


    public void FindSticky() //스티키 프레그먼트 찾음
    {
        if (stickyTop > 0 || stickyBottom > 0)
        {
            meshFilters = meshFilters.OrderByDescending(x => x.transform.position.y).ToArray();
            for (int j = 0; j < stickyTop; j++)
            {
                FraggedChild g = meshFilters[j].GetComponent<FraggedChild>();
                g.stickyFrag = true;
            }
            for (int i = meshFilters.Length - stickyBottom; i < meshFilters.Length; i++)
            {
                FraggedChild k = meshFilters[i].GetComponent<FraggedChild>();
                k.stickyFrag = true;
            }
        }
    }

    public int Contains(ArrayList l, string n)
    {
        for (int i = 0; i < l.Count; i++)
        {
            if ((l[i] as Material).name == n)
            {
                return i;
            }
        }
        return -1;
    }

    public void EnableRenderers()
    {
        foreach (Transform child in fragments) //자식 파편들 랜더링
        {
            child.GetComponent<Renderer>().enabled = true;
        }
    }


    public void CombineFrags() //결합된 파편
    {
        if ((combinedFrags == null) && !this.startMesh.GetComponent<Renderer>().enabled)
        {
            combinedFrags = new GameObject();
            combinedFrags.name = "Combined Fragments";
            combinedFrags.gameObject.AddComponent<MeshFilter>();
            combinedFrags.gameObject.AddComponent<MeshRenderer>();

            if (meshFilters.Length == 0)
            {
                meshFilters = new MeshFilter[fragments.transform.childCount];
                meshFilters = fragments.transform.GetComponentsInChildren<MeshFilter>(true);
            }

            ArrayList materials = new ArrayList();
            ArrayList combineInstanceArrays = new ArrayList();

            for (int i = 0; i < meshFilters.Length; i++)
            {
                MeshFilter[] meshFilterss = meshFilters[i].GetComponentsInChildren<MeshFilter>();
                foreach (MeshFilter meshFilter in meshFilterss)
                {
                    MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
                    FraggedChild c = meshFilter.transform.GetComponent<FraggedChild>();
                    if ((c != null) && c.fragged == false || (c != null) && combineFrags)
                    {
                        c.fragged = false;
                        c.hitPoints = 1.0f;

                        meshFilters[i].transform.gameObject.GetComponent<Renderer>().enabled = false;
                        meshFilters[i].GetComponent<Rigidbody>().isKinematic = true;
                        for (int o = 0; o < meshFilter.sharedMesh.subMeshCount; o++)
                        {
                            int materialArrayIndex = Contains(materials, meshRenderer.sharedMaterials[o].name);
                            if (materialArrayIndex == -1)
                            {
                                materials.Add(meshRenderer.sharedMaterials[o]);
                                materialArrayIndex = materials.Count - 1;
                            }
                            combineInstanceArrays.Add(new ArrayList());
                            CombineInstance combineInstance = new CombineInstance();
                            combineInstance.transform = meshRenderer.transform.localToWorldMatrix;
                            combineInstance.subMeshIndex = o;
                            combineInstance.mesh = meshFilter.sharedMesh;
                            (combineInstanceArrays[materialArrayIndex] as ArrayList).Add(combineInstance);
                        }
                    }
                }
            }
            Mesh[] meshes = new Mesh[materials.Count];
            CombineInstance[] combineInstances = new CombineInstance[materials.Count];
            for (int m = 0; m < materials.Count; m++)
            {
                CombineInstance[] combineInstanceArray = (combineInstanceArrays[m] as ArrayList).ToArray(typeof(CombineInstance)) as CombineInstance[];
                meshes[m] = new Mesh();
                meshes[m].CombineMeshes(combineInstanceArray, true, true);
                combineInstances[m] = new CombineInstance();
                combineInstances[m].mesh = meshes[m];
                combineInstances[m].subMeshIndex = 0;
            }
            combinedFrags.GetComponent<MeshFilter>().sharedMesh = new Mesh();
            combinedFrags.GetComponent<MeshFilter>().sharedMesh.CombineMeshes(combineInstances, false, false);
            foreach (Mesh mesh in meshes)
            {
                mesh.Clear();
                DestroyImmediate(mesh);
            }
            MeshRenderer meshRendererCombine = combinedFrags.GetComponent<MeshFilter>().GetComponent<MeshRenderer>();
            if (meshRendererCombine == null)
            {
                meshRendererCombine = gameObject.AddComponent<MeshRenderer>();
            }
            Material[] materialsArray = materials.ToArray(typeof(Material)) as Material[];
            meshRendererCombine.materials = materialsArray;
            if (Application.isEditor && !Application.isPlaying && combinedFrags.transform.parent != transform)
            {
                combinedFrags.transform.parent = transform;
            }
        }
    }

    public void ReleaseFrags(bool editor) //결합된 파편 제거
    {
        if (combinedFrags != null)
        {
            for (int i = 0; i < meshFilters.Length; i++)
            {
                meshFilters[i].transform.gameObject.GetComponent<Renderer>().enabled = true;
            }
            Destroy(combinedFrags);
        }
    }

    public void reCombine() //재결합
    {
        if ((startMesh == null) || !startMesh.GetComponent<Renderer>().enabled)
        {
            if (combineMeshesDelay >= 0 && combinedFrags == null && reCounter > combineMeshesDelay)
            {
                CombineFrags();
            }
            else if (reCounter <= combineMeshesDelay)
            {
                reCounter++;
            }
        }
    }
}
