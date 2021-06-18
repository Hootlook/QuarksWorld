using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuarksWorld.Systems
{
    public class PlayerModuleClient 
    {
        public PlayerModuleClient(GameWorld gameWorld)
        {
            world = gameWorld;
        }

        public void Update() { }

        public void Shutdown() { }

        public void SampleInput(bool userInputEnabled, float deltaTime, int renderTick)
        {
            if (userInputEnabled)
                UserCommand.AccumulateInput(ref command, deltaTime);

            command.renderTick = renderTick;
        }

        public void ResetInput(bool userInputEnabled)
        {
            // Clear keys and resample to make sure released keys gets detected.
            // Pass in 0 as deltaTime to make mouse input and view stick do nothing
            UserCommand.ClearInput(ref command);

            if (userInputEnabled)
                UserCommand.AccumulateInput(ref command, 0.0f);
        }

        public void StoreCommand(int tick)
        {
            var localPlayer = PlayerState.localPlayer;

            if (localPlayer == null)
                return;

            localPlayer.command.tick = tick;

            var lastBufferTick = commandBuffer.LastTick();
            if (tick != lastBufferTick && tick != lastBufferTick + 1)
            {
                commandBuffer.Clear();
                GameDebug.Log(string.Format("Trying to store tick:{0} but last buffer tick is:{1}. Clearing buffer", tick, lastBufferTick));
            }

            if (tick == lastBufferTick)
                commandBuffer.Set(ref localPlayer.command, tick);
            else
                commandBuffer.Add(ref localPlayer.command, tick);
        }

        public void SendCommand(int tick)
        {
            var localPlayer = PlayerState.localPlayer;

            if (localPlayer == null)
                return;

            var command = UserCommand.defaultCommand;
            var commandValid = commandBuffer.TryGetValue(tick, ref command);
            if (commandValid)
            {
                Mirror.NetworkClient.Send(command);
            }
        }

        readonly GameWorld world;

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
