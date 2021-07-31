using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuarksWorld
{
    public class Weapon : MonoBehaviour, IEquipable
    {
        IEquiper owner;

        public void Equip()
        {
            throw new NotImplementedException();
        }

        void OnDestroy()
        {
            owner.OnDesEquip(this);
        }
    }
}
