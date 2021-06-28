using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace QuarksWorld.Systems
{
    public class MovableSystemServer
    {
        int spawnNum;

        public MovableSystemServer()
        {
            Console.AddCommand("spawnbox", CmdSpawnBox, "Spawn <n> boxes", GetHashCode());
            Console.AddCommand("despawnboxes", CmdDespawnBoxes, "Despawn all boxes", GetHashCode());
        }

        private void CmdDespawnBoxes(string[] args)
        {
            foreach (var box in movables)
            {
                NetworkServer.Destroy(box);
            }
            movables.Clear();
        }

        private void CmdSpawnBox(string[] args)
        {
            if (args.Length > 0)
                int.TryParse(args[0], out spawnNum);
            else
                spawnNum = 1;
            spawnNum = Mathf.Clamp(spawnNum, 1, 100);
        }

        public void Shutdown()
        {
            Console.RemoveCommandsWithTag(GetHashCode());
        }

        public void Update()
        {
            if (spawnNum <= 0)
                return;
            spawnNum--;

            int x = spawnNum % 10 - 5;
            int z = spawnNum / 10 - 5;

            GameObject obj = Object.Instantiate((GameObject)Resources.Load("Prefabs/MovableBox"), Vector3.up * 5, Quaternion.identity);
            
            NetworkServer.Spawn(obj);
            movables.Add(obj);
        }

        private List<GameObject> movables = new List<GameObject>();
    }
}
