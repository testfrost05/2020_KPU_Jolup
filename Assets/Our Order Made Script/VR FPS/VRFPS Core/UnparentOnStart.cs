using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnparentOnStart : MonoBehaviour
{
	public float delay = 1f;

	[SerializeField] protected bool destroyParent;

	IEnumerator Start()
	{
		yield return new WaitForEndOfFrame();

		//yield return new WaitForSeconds(delay);

		Transform tempTransform = transform.parent;
		transform.parent = null;
		if (destroyParent)
			Destroy(tempTransform.gameObject);
		Destroy(this);
	}
}
