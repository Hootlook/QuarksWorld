using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace QuarksWorld
{
    public class Health : MonoBehaviour
    {
        public float health;
        public float maxHealth;
        public int deathTick;
        public int killedBy;

        public void SetMaxHealth(float maxHealth)
        {
            this.maxHealth = maxHealth;
            health = maxHealth;
        }
    }
}
