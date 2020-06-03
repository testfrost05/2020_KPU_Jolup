using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    public class MultiItemSlot : Slot //Slot을 상속
    {
        [SerializeField] protected Slot secondarySlot;
        [SerializeField] protected Slot tertiarySlot;
        [SerializeField] protected int secondarySizeLimit;

        public override bool ValidItem(Item potentialItem) //유효 아이템 확인
        {
            if (potentialItem == null)
            {
                return false;
            }

            if (potentialItem.Size > sizeLimit)
            {
                return false;
            }

            if ((secondarySlot.HasItem || tertiarySlot.HasItem) && potentialItem.Size > secondarySizeLimit)
            {
                return false;
            }

            if (!HasTag(potentialItem))
            {
                return false;
            }

            return true;
        }

        public override Item StoredItem //아이템 저장
        {
            set
            {
                if (value)
                {
                    if (value.Size > secondarySizeLimit)
                    {
                        secondarySlot.sizeLimit = -1;
                        tertiarySlot.sizeLimit = -1;
                    }
                    else
                    {
                        secondarySlot.sizeLimit = secondarySizeLimit;
                        tertiarySlot.sizeLimit = secondarySizeLimit;
                    }
                }
                else
                {
                    secondarySlot.sizeLimit = secondarySizeLimit;
                    tertiarySlot.sizeLimit = secondarySizeLimit;

                    offset.localPosition = Vector3.zero;
                    offset.localRotation = Quaternion.identity;
                }

                storedItem = value;
            }
        }
    }
}