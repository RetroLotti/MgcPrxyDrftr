using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MgcPrxyDrftr.lib
{
    public enum LoopState
    {
        Main,
        Options,
        BoosterDraft,
        DeckCreator,
        DeckManager,
        SetManager,
        RawListManager,
        FolderPrint,
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
                return obj is StateTransition other && CurrentState == other.CurrentState && Command == other.Command;
            }
        }

        private readonly Dictionary<StateTransition, LoopState> transitions;
        public LoopState CurrentState { get; private set; }

        public StateMachine()
        {
            CurrentState = LoopState.Main;
            transitions = new Dictionary<StateTransition, LoopState>
            {
                { new StateTransition(LoopState.Main, "b"), LoopState.BoosterDraft },
                { new StateTransition(LoopState.Main, "c"), LoopState.Main },
                { new StateTransition(LoopState.Main, "d"), LoopState.DeckCreator },
                { new StateTransition(LoopState.Main, "f"), LoopState.FolderPrint },
                { new StateTransition(LoopState.Main, "o"), LoopState.Options },
                { new StateTransition(LoopState.Main, "r"), LoopState.RawListManager },
                { new StateTransition(LoopState.Main, "s"), LoopState.SetManager },
                { new StateTransition(LoopState.Main, "x"), LoopState.Exit },

                { new StateTransition(LoopState.BoosterDraft, "a"), LoopState.BoosterDraft },
                { new StateTransition(LoopState.BoosterDraft, "l"), LoopState.BoosterDraft },
                { new StateTransition(LoopState.BoosterDraft, "g"), LoopState.BoosterDraft },
                { new StateTransition(LoopState.BoosterDraft, "b"), LoopState.Main },

                { new StateTransition(LoopState.FolderPrint, "b"), LoopState.Main },

                { new StateTransition(LoopState.Options, "p"), LoopState.Options },
                { new StateTransition(LoopState.Options, "e"), LoopState.Options },
                { new StateTransition(LoopState.Options, "b"), LoopState.Main },

                { new StateTransition(LoopState.DeckCreator, "a"), LoopState.DeckCreator },
                { new StateTransition(LoopState.DeckCreator, "l"), LoopState.DeckCreator },
                { new StateTransition(LoopState.DeckCreator, "b"), LoopState.Main },

                { new StateTransition(LoopState.SetManager, "a"), LoopState.SetManager },
                { new StateTransition(LoopState.SetManager, "l"), LoopState.SetManager },
                { new StateTransition(LoopState.SetManager, "r"), LoopState.SetManager },
                { new StateTransition(LoopState.SetManager, "b"), LoopState.Main },

                { new StateTransition(LoopState.RawListManager, "b"), LoopState.Main },
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
