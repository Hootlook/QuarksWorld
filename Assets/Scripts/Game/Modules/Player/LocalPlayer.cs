using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuarksWorld
{
    public class LocalPlayer : MonoBehaviour
    {
        public int playerId = -1;

        public PlayerState playerState;
        public GameObject controlledEntity;
        public UserCommand command = UserCommand.defaultCommand;
        public StateBuffer<UserCommand> commandBuffer = new StateBuffer<UserCommand>(ClientCommandBufferSize);

        public const int ClientCommandBufferSize = 32;
    }

    public class StateBuffer<T> where T : struct
    {
        public StateBuffer(int capacity)
        {
            elements = new T[capacity];
        }

        public int LastTick()
        {
            return size > 0 ? tick : -1;
        }

        public int FirstTick()
        {
            return size > 0 ? tick - size + 1 : -1;
        }

        public void Clear()
        {
            size = 0;
        }

        public bool TryGetValue(int tick, ref T result)
        {
            if (IsValidTick(tick))
            {
                result = elements[tick % elements.Length];
                return true;
            }
            else
                return false;
        }

        public void Add(ref T value, int tick)
        {
            elements[tick % elements.Length] = value;

            // Reset the buffer if we receive non consecutive tick
            if (tick - this.tick != 1)
                size = 1;
            else if (size < elements.Length)
                ++size;

            this.tick = tick;
        }

        public void Set(ref T value, int tick)
        {
            elements[tick % elements.Length] = value;
        }

        public bool IsValidTick(int tick)
        {
            return tick <= this.tick && tick > this.tick - size;
        }

        T[] elements;
        int tick;
        int size;
    }
}
