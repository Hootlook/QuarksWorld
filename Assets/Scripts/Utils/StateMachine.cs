using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuarksWorld
{
    class StateMachine<T>
    {
        public delegate void StateFunc();

        public void Add(T id, StateFunc enter, StateFunc update, StateFunc leave)
        {
            states.Add(id, new State(id, enter, update, leave));
        }

        public T CurrentState() => currentState.Id;

        public void Update() => currentState.Update();

        public void Shutdown()
        {
            if (currentState != null && currentState.Leave != null)
                currentState.Leave();
            currentState = null;
        }

        public void SwitchTo(T state)
        {
            GameDebug.Assert(states.ContainsKey(state), "Trying to switch to unknown state " + state.ToString());
            GameDebug.Assert(currentState == null || !currentState.Id.Equals(state), "Trying to switch to " + state.ToString() + " but that is already current state");

            var newState = states[state];
            GameDebug.Log("Switching state: " + (currentState != null ? currentState.Id.ToString() : "null") + " -> " + state.ToString());

            if (currentState != null && currentState.Leave != null)
                currentState.Leave();
            newState.Enter?.Invoke();
            currentState = newState;
        }

        class State
        {
            public State(T id, StateFunc enter, StateFunc update, StateFunc leave)
            {
                Id = id;
                Enter = enter;
                Update = update;
                Leave = leave;
            }
            public T Id;
            public StateFunc Enter;
            public StateFunc Update;
            public StateFunc Leave;
        }

        State currentState = null;
        Dictionary<T, State> states = new Dictionary<T, State>();
    }
}
