using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace QuarksWorld
{
    [System.Serializable]
    public struct UserCommand
    {
        public enum Button : uint
        {
            None = 0,
            Jump = 1 << 0,
            Boost = 1 << 1,
            PrimaryFire = 1 << 2,
            SecondaryFire = 1 << 3,
            Reload = 1 << 4,
            Melee = 1 << 5,
            Use = 1 << 6,
            Ability1 = 1 << 7,
            Ability2 = 1 << 8,
            Ability3 = 1 << 9,
        }

        public struct ButtonBitField
        {
            public uint flags;

            public bool IsSet(Button button)
            {
                return (flags & (uint)button) > 0;
            }

            public void Or(Button button, bool val)
            {
                if (val)
                    flags = flags | (uint)button;
            }

            public void Set(Button button, bool val)
            {
                if (val)
                    flags = flags | (uint)button;
                else
                {
                    flags = flags & ~(uint)button;
                }
            }
        }

        public int checkTick;        // For debug purposes
        public int renderTick;
        public float moveYaw;
        public float moveMagnitude;
        public float lookYaw;
        public float lookPitch;
        public ButtonBitField buttons;

        public static readonly UserCommand defaultCommand = new UserCommand(0);

        private UserCommand(int i)
        {
            checkTick = 0;
            renderTick = 0;
            moveYaw = 0;
            moveMagnitude = 0;
            lookYaw = 0;
            lookPitch = 90;
            buttons.flags = 0;
        }

        public void ClearCommand()
        {
            buttons.flags = 0;
        }

        public Vector3 lookDir
        {
            get { return Quaternion.Euler(new Vector3(-lookPitch, lookYaw, 0)) * Vector3.down; }
        }
        public Quaternion lookRotation
        {
            get { return Quaternion.Euler(new Vector3(90 - lookPitch, lookYaw, 0)); }
        }
    }
}
