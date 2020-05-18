using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    [System.Serializable]
    public class VectorHistoryAverage 
    {
        public int velocitySampleSize = 3;
        protected Vector3?[] releaseVelocity;
        protected Vector3?[] releaseAngularVelocity;
        protected int velocityHistoryStep = 1;

        protected Vector3 previousPosition;
        protected Quaternion previousRotation;
        public Quaternion PreviousRotation { get { return previousRotation; } }
        public float releaseForceMultiplier = 1;
        public float releaseAngularForceMultiplier = 1;

        public Vector3 _ReleaseVelocity
        {
            get
            {
                Vector3 tempVelocity = GetMeanVector(releaseVelocity);
                return !float.IsNaN(tempVelocity.x) && !float.IsNaN(tempVelocity.y) && !float.IsNaN(tempVelocity.z) ? tempVelocity : Vector3.zero;
            }
        }

        public Vector3 _ReleaseAngularVelocity
        {
            get
            {
                Vector3 tempVelocity = GetMeanVector(releaseAngularVelocity);
                return !float.IsNaN(tempVelocity.x) && !float.IsNaN(tempVelocity.y) && !float.IsNaN(tempVelocity.z) ? tempVelocity : Vector3.zero;
            }
        }

        public void InitializeHistory()
        {
            releaseVelocity = new Vector3?[velocitySampleSize];
            releaseAngularVelocity = new Vector3?[velocitySampleSize];
        }

        public void VelocityStep(Transform transform)
        {
            velocityHistoryStep++;

            if (velocityHistoryStep >= releaseVelocity.Length && velocityHistoryStep >= releaseAngularVelocity.Length)
            {
                velocityHistoryStep = 0;
            }

            releaseAngularVelocity[velocityHistoryStep] = GetAngularVelocityAngleAxis(previousRotation, transform.rotation);
            previousRotation = transform.rotation;
            releaseVelocity[velocityHistoryStep] = (transform.position - previousPosition) / Time.deltaTime;
            previousPosition = transform.position;
        }

        public Vector3 GetVelocity(Rigidbody rb) //속도 구함
        {
            return GetMeanVector(releaseVelocity) * releaseForceMultiplier / rb.mass;
        }

        public Vector3 GetAngularVelocity(Rigidbody rb) //가속도 구함
        {
            return GetMeanVector(releaseAngularVelocity) * releaseAngularForceMultiplier / rb.mass;
        }


        public IEnumerator ReleaseVelocity(Rigidbody rb)
        {
            Vector3 tempVelocity = GetMeanVector(releaseVelocity);
            Vector3 tempAngularVelocity = GetMeanVector(releaseAngularVelocity);

            yield return new WaitForEndOfFrame();

            if (!float.IsNaN(tempVelocity.x) && !float.IsNaN(tempVelocity.y) && !float.IsNaN(tempVelocity.z))
            {
                rb.velocity = tempVelocity;
            }
            else
            {
                rb.velocity = Vector3.zero;
            }

            if (!float.IsNaN(tempAngularVelocity.x) && !float.IsNaN(tempAngularVelocity.y) && !float.IsNaN(tempAngularVelocity.z))
            {
               rb.angularVelocity = tempAngularVelocity * releaseAngularForceMultiplier;
            }
            else
            {
                rb.angularVelocity = Vector3.one;
            }

            velocityHistoryStep = 0;

            for (int i = 0; i < releaseVelocity.Length; i++)
            {
                releaseVelocity[i] = null;
                releaseAngularVelocity[i] = null;
            }
        }

        public static Vector3 GetAngularVelocityAngleAxis(Quaternion from, Quaternion to) //벡터 각도 구함
        {
            Quaternion angularVelocity = to * Quaternion.Inverse(from);

            float angle = 0.0f;
            Vector3 axis = Vector3.zero;

            angularVelocity.ToAngleAxis(out angle, out axis);

            if (angle > 180f)
            {
                angle -= 360f;
            }

            angle *= Mathf.Deg2Rad;

            axis = (axis * angle) / Time.deltaTime;

            if ((float.IsNaN(axis.x) || float.IsNaN(axis.y) || float.IsNaN(axis.z))
                || float.IsInfinity(axis.x) || float.IsInfinity(axis.y) || float.IsInfinity(axis.z))
            {
                return Vector3.zero;
            }

            return axis;
        }

        public static Vector3 GetMeanVector(Vector3?[] positions) //의미있는 벡터 얻음
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
    }
}