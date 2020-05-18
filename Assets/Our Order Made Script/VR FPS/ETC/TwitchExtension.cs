using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VrFps;


public abstract class TwitchExtension //확장
{

    public static void SetLayerRecursivly(GameObject gameObject, string newLayer) //레이어 변경
	{
        if (gameObject.layer == LayerMask.NameToLayer("Default")) //디폴트 레이어면
            gameObject.layer = LayerMask.NameToLayer(newLayer); //변경

        foreach (Transform child in gameObject.transform)
        {
            SetLayerRecursivly(child.gameObject, newLayer);
        }
	}

	public static void SetItemLayerRecursivly(GameObject gameObject, string newLayer) //아이템레이어 변경
	{
        if (gameObject.layer == LayerMask.NameToLayer("HeldItem") || gameObject.layer == LayerMask.NameToLayer("InventoryItem")
                || gameObject.layer == LayerMask.NameToLayer("Item"))
        {
            gameObject.layer = LayerMask.NameToLayer(newLayer); //아이템레이어면 변경
        }
        foreach (Transform child in gameObject.transform)
        {
            SetItemLayerRecursivly(child.gameObject, newLayer);
        }
	}

    public static void SetItemLayerRecursivly(GameObject gameObject, LayerMask newLayer) //스트링이 아니라 레이어마스크형으로
    {
        if (gameObject.layer == LayerMask.NameToLayer("HeldItem") || gameObject.layer == LayerMask.NameToLayer("InventoryItem")
               || gameObject.layer == LayerMask.NameToLayer("Item"))
        {
            gameObject.layer = newLayer;
        }

        foreach (Transform child in gameObject.transform)
        {
            SetItemLayerRecursivly(child.gameObject, newLayer);
        }
    }

    public enum DotAxis 
	{
		right,
		left,
		up,
		down,
		forward,
		back
	}

	public static Vector3 ReturnAxis(DotAxis axis, Transform transform) //DotAxis 리턴
	{
        if (!transform) //위치없음
        {
            return Vector3.zero;
        }
		switch (axis) //DotAxis 마다
		{
            case DotAxis.up:
                return transform.up;
            case DotAxis.down:
                return -transform.up;
            case DotAxis.right:
                return transform.right;
            case DotAxis.left:
                return -transform.right;
            case DotAxis.forward:
				return transform.forward;
			case DotAxis.back:
				return -transform.forward;
		}

		return Vector3.zero;
	}

	public enum Axis
	{
		x,
		y,
		z
	}

	public static Vector3 ReturnAxis(Axis axis, Transform transform) //Axis 리턴
	{
		switch (axis)
		{
			case Axis.x:
				return transform.right;
			case Axis.y:
				return transform.up;
			case Axis.z:
				return transform.forward;
		}

		return Vector3.zero;
	}

	public static void PlayRandomAudioClip(List<AudioClip> list, Vector3 position) //정해진 위치에 오디오 재생
	{
        if (list.Count == 0)
        {
            return;
        }

		AudioClip randomAudioClipClone = list[Random.Range(0, list.Count - 1)];

        if (randomAudioClipClone)
        {
            AudioSource.PlayClipAtPoint(randomAudioClipClone, position);
        }
	}

	public static Transform GetClosest(List<Transform> transforms, Vector3 referencePoint) //가장가까운 트랜스폼 가져옴
	{
		Transform closestTransform = null;
		float minDistance = Mathf.Infinity;
		foreach (Transform _transform in transforms)
		{
			float distance = Vector3.Distance(referencePoint, _transform.position);
			if (distance < minDistance)
				closestTransform = _transform;
		}

		return closestTransform;
	}

	public static Vector3 GetClosest(List<Vector3> positions, Vector3 referencePoint) //가장 가까운 포지션 가져옴
	{
		Vector3 closestPosition = Vector3.zero;
		float minDistance = Mathf.Infinity;
		foreach (Vector3 position in positions)
		{
			float distance = Vector3.Distance(referencePoint, position);
			if (distance < minDistance)
				closestPosition = position;
		}

		return closestPosition;
	}

	public static int GetClosestIndex(List<Transform> transforms, Vector3 referencePoint) //가장 가까운 인덱스 가져옴
	{
		int index = 0;
		float minDistance = Mathf.Infinity;
		for(int i = 0; i < transforms.Count; i++)
		{
			float distance = Vector3.Distance(referencePoint, transforms[i].position);
			if (distance < minDistance)
				index = i;
		}

		return index;
	}

	public static int GetClosestIndex(List<Vector3> positions, Vector3 referencePoint)
	{
		int index = 0;
		float minDistance = Mathf.Infinity;
		for (int i = 0; i < positions.Count; i++)
		{
			float distance = Vector3.Distance(referencePoint, positions[i]);
			if (distance < minDistance)
				index = i;
		}

		return index;
	}

	public static bool InverseBool (bool boolToInverse)
    {
        return !boolToInverse;
    }

    public static Vector3 GetMeanVector(Vector3?[] positions) //백터값 얻음
    {
        float x = 0f;
        float y = 0f;
        float z = 0f;

        int count = 0;
        for (int index = 0; index < positions.Length; index++)
        {
            if (positions[index] != null)
            {
                x += positions[index].Value.x;
                y += positions[index].Value.y;
                z += positions[index].Value.z;

                count++;
            }
        }

        return new Vector3(x / count, y / count, z / count);
    }

    public static float NormalizeInput(float min, float max, float value) 
    {
        if (max - min <= 0)
            return 0;

        return Mathf.Clamp01((value - min) / (max - min));
    }

    public static float InverseNormalizeInput(float min, float max, float value) 
    {
        return 1-NormalizeInput(min, max, value);
    }

    public static float GetVerticalDirection(Vector3 from, Vector3 to) //수직 각도 얻음
    {
        float angle = 0;
        float x = Vector3.Distance(from, new Vector3(to.x, from.y, to.z));
        float y = Mathf.Abs(to.y - from.y);
        float tan = y / x;
        angle = Mathf.Rad2Deg * Mathf.Atan(tan);
        return angle;
    }

    public static float GetHorizontalDirection(Vector3 from, Vector3 to, Vector3 forward) //수평 각도 얻음
    {
       float angle = 0;
       Vector3 direction = to - from;
	   direction.y = 0;
       angle = Vector3.Angle(direction, forward) * Mathf.Sign(Vector3.Dot(-direction, Vector3.forward));
       return angle;
    }

	public static float GetAngleAroundZAxis(Vector3 from, Vector3 to, Vector3 forward) //각도 얻음
	{
		float angle = 0;
		Vector3 direction = to - from;
		direction.z = 0;
		angle = Vector3.Angle(direction, forward) * Mathf.Sign(Vector3.Dot(-direction, Vector3.forward));
		return angle;
	}

	public static void ForGizmo(Vector3 pos, Vector3 direction, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
    {
        Gizmos.DrawRay(pos, direction);

        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Gizmos.DrawRay(pos + direction, right * arrowHeadLength);
        Gizmos.DrawRay(pos + direction, left * arrowHeadLength);
    }

    public static void ForGizmo(Vector3 pos, Vector3 direction, Color color, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
    {
        Gizmos.color = color;
        Gizmos.DrawRay(pos, direction);

        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Gizmos.DrawRay(pos + direction, right * arrowHeadLength);
        Gizmos.DrawRay(pos + direction, left * arrowHeadLength);
    }

    public static void ForDebug(Vector3 pos, Vector3 direction, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
    {
        Debug.DrawRay(pos, direction);

        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Debug.DrawRay(pos + direction, right * arrowHeadLength);
        Debug.DrawRay(pos + direction, left * arrowHeadLength);
    }

    public static void ForDebug(Vector3 pos, Vector3 direction, Color color, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
    {
        Debug.DrawRay(pos, direction, color);

        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Debug.DrawRay(pos + direction, right * arrowHeadLength, color);
        Debug.DrawRay(pos + direction, left * arrowHeadLength, color);
    }
}
