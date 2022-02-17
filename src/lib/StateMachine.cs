using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyDraftor.lib
{
    public enum LoopState
    {
        Main,
        Options,
        BoosterDraft,
        DeckCreator,
        DeckManager,
        SetManager,
        Exit
    }

    public class StateMachine
    {
        class StateTransition
        {
            readonly LoopState CurrentState;
            readonly string Command;

            public StateTransition(LoopState currentState, string command)
            {
                CurrentState = currentState;
                Command = command;
            }

            public override int GetHashCode()
            {
                return 17 + 31 * CurrentState.GetHashCode() + 31 * Command.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                StateTransition other = obj as StateTransition;
                return other != null && this.CurrentState == other.CurrentState && this.Command == other.Command;
            }
        }

        Dictionary<StateTransition, LoopState> transitions;
        public LoopState CurrentState { get; private set; }

        public StateMachine()
        {
            CurrentState = LoopState.Main;
            transitions = new Dictionary<StateTransition, LoopState>
            {
                { new StateTransition(LoopState.Main, "x"), LoopState.Exit },
        //        { new StateTransition(ProcessState.Inactive, Command.Begin), ProcessState.Active },
        //        { new StateTransition(ProcessState.Active, Command.End), ProcessState.Inactive },
        //        { new StateTransition(ProcessState.Active, Command.Pause), ProcessState.Paused },
        //        { new StateTransition(ProcessState.Paused, Command.End), ProcessState.Inactive },
        //        { new StateTransition(ProcessState.Paused, Command.Resume), ProcessState.Active }
            };
        }

        public LoopState GetNext(string command)
        {
            StateTransition transition = new(CurrentState, command.ToLower());
            return !transitions.TryGetValue(transition, out LoopState nextState)
                ? throw new Exception("Invalid transition: " + CurrentState + " -> " + command)
                : nextState;
        }

        public LoopState MoveNext(string command)
        {
            CurrentState = GetNext(command);
            return CurrentState;
        }
    }
}
