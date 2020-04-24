using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    [System.Serializable]
    public class RecoilAxis //총기반동축
    {

        [HideInInspector] public Item self;

        [ReadOnly]
        [SerializeField]
        float target;

        [ReadOnly]
        [SerializeField]
        protected float current;

        public float Current
        {
            get
            {
                return current;
            }
        }

        [SerializeField] protected bool decreaseWhileIncreasing;
        [SerializeField] public bool debug;

        [Range(0, 1)] [SerializeField] protected float IncreaseChance;

        //한손으로 잡을 떄 총기 반동
        [SerializeField] protected Vector2 limits;
        [SerializeField] protected float increment;
        [SerializeField] protected float decreaseTargetSpeed;
        [SerializeField] protected float increaseSpeed;
        [SerializeField] protected float decreaseSpeed;


        //양손으로 잡을 떄 총기 반동
        [SerializeField] public float twoHandMaxScale;
        [SerializeField] public float twoHandIncrementScale;
        [SerializeField] public float twoHandDecreaseTargetScale;
        [SerializeField] public float twoHandIncreaseScale;
        [SerializeField] public float twoHandDecreaseScale;

        protected float TwoHandMaxScale
        {
            get
            {
                return self ? self.PrimaryHand && self.SecondaryHand ? twoHandMaxScale : 1 : 1;
            }
        }
        protected float TwoHandIncrementScale
        {
            get
            {
                return self ? self.PrimaryHand && self.SecondaryHand ? twoHandIncrementScale : 1 : 1;
            }
        }
        protected float TwoHandDecreaseTargetScale
        {
            get
            {
                return self ? self.PrimaryHand && self.SecondaryHand ? twoHandDecreaseTargetScale : 1 : 1;
            }
        }
        protected float TwoHandIncreaseScale
        {
            get
            {
                return self ? self.PrimaryHand && self.SecondaryHand ? twoHandIncreaseScale : 1 : 1;
            }
        }
        protected float TwoHandDecreaseScale
        {
            get
            {
                return self ? self.PrimaryHand && self.SecondaryHand ? twoHandDecreaseScale : 1 : 1;
            }
        }

        bool targetIsGreaterThanCurrentAtTheTimeOfIncrease;
        bool targetIsLessThanCurrentAtTheTimeOfIncrease;

        bool reachedTarget;

        [System.Serializable]
        public struct Initial
        {
            public float max;
            public float increment;
            public float decreaseTarget;
            public float decrease;
            public float increase;
        }

        public Initial initial;

        public void Initialize()
        {
            //양손일 경우 변경
            initial.max = twoHandMaxScale;
            initial.increment = twoHandIncrementScale;
            initial.decreaseTarget = twoHandDecreaseTargetScale;
            initial.decrease = twoHandIncreaseScale;
            initial.increase = twoHandDecreaseScale;

            if (limits.x > limits.y)
            {
                Debug.LogWarning(" { " + self.name + " } RecoilAxis: The lower limit must be less than the upper limit, swapping");
                limits.x = limits.x + limits.y;
                limits.y = limits.x - limits.y;
                limits.x = limits.x - limits.y;
            }
        }

        public void SetInitial()
        {
            twoHandMaxScale = initial.max;
            twoHandIncrementScale = initial.increment;
            twoHandDecreaseTargetScale = initial.decreaseTarget;
            twoHandIncreaseScale = initial.decrease;
            twoHandDecreaseScale = initial.increase;
        }

        public void IncreaseRecoil() 
        {
            target = Mathf.Clamp(target + (increment * TwoHandIncrementScale * (Random.Range(0.0f, 1.0f) <= IncreaseChance ? 1 : -1)),
                                       limits.x * TwoHandMaxScale, limits.y * TwoHandMaxScale);

            targetIsGreaterThanCurrentAtTheTimeOfIncrease = target > current;

            targetIsLessThanCurrentAtTheTimeOfIncrease = !targetIsGreaterThanCurrentAtTheTimeOfIncrease;

            reachedTarget = false;
        }

        public void DecreaseRecoil() 
        {
            if (!reachedTarget && (!decreaseWhileIncreasing && ((current < target && target > 0)
                || (current > target && target < 0))))
            {
                return;
            }

            float tempSpeed = decreaseTargetSpeed * TwoHandDecreaseTargetScale * Time.deltaTime;

            if (Mathf.Abs(target) < tempSpeed)
            {
                tempSpeed = Mathf.Abs(target);
            }

            target = Mathf.Clamp(target - (target >= 0 ? tempSpeed : -tempSpeed), limits.x * TwoHandMaxScale, limits.y * TwoHandMaxScale);
        }

        float speed = 0;

        public void ApplyRecoil()
        {
            if (!reachedTarget)
            {
                speed = increaseSpeed * TwoHandDecreaseScale;
                if (target > 0)
                {
                    if ((targetIsGreaterThanCurrentAtTheTimeOfIncrease && current >= target))
                        reachedTarget = true;
                }
                else if (target < 0)
                {
                    if ((targetIsLessThanCurrentAtTheTimeOfIncrease && current <= target))
                    {
                        reachedTarget = true;
                    }
                }
            }
            else
            {
                speed = decreaseSpeed * TwoHandDecreaseScale;
            }

            speed = target == current ? 0 : speed;

            current = Mathf.MoveTowards(current, target, speed * Time.deltaTime);
        }


        public void ResetAxis()
        {
            current = 0;
            target = 0;
        }
    }
}