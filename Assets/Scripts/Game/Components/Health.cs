using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace QuarksWorld
{
    public class Health : NetworkBehaviour
    {
        [SyncVar] public float health;
        [SyncVar] public float maxHealth;
        [SyncVar] public int deathTick;
        [SyncVar] public int killedBy;

        public void SetMaxHealth(float maxHealth)
        {
            this.maxHealth = maxHealth;
            health = maxHealth;
        }
    }
}
