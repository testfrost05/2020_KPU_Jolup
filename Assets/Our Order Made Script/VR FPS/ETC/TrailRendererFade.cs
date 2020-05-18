using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//총알 궤적
public class TrailRendererFade : MonoBehaviour
{
	[SerializeField] float visibility; //궤적 보여줄 시간
	[SerializeField] protected float fadeTime; 

	TrailRenderer line; //TrailRenderer로 총알 궤적 만듬

    void Start()
	{
		line = GetComponent<TrailRenderer>();
		line.sharedMaterial.SetFloat("_InvFade", 3);
	}

	void Update()
	{
		visibility -= Time.deltaTime / fadeTime; 
		line.sharedMaterial.SetColor("_TintColor", new Color(.5f, .5f, .5f, visibility)); //궤적 색

		if (visibility <= 0) //설정한 시간이 0이하가 되면 Destroy
			Destroy(gameObject);
	}
}
