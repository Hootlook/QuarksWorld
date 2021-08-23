using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace QuarksWorld
{
    public class Health : MonoBehaviour
    {
        public float health;
        public float maxHealth;
        public int deathTick;
        public Entity killedBy;

        public void SetMaxHealth(float maxHealth)
        {
            this.maxHealth = maxHealth;
            health = maxHealth;
        }
    }
}
