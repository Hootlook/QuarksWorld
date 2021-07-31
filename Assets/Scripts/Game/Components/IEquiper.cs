using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuarksWorld
{
    public interface IEquiper
    {
        void OnDesEquip(IEquipable item);
    }
}
