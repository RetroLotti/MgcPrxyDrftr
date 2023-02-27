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
        private class StateTransition
        {
            private readonly LoopState _currentState;
            private readonly string _command;

            public StateTransition(LoopState currentState, string command)
            {
                _currentState = currentState;
                _command = command;
            }

            public override int GetHashCode()
            {
                return 17 + 31 * _currentState.GetHashCode() + 31 * _command.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return obj is StateTransition other && _currentState == other._currentState && _command == other._command;
            }
        }

        private readonly Dictionary<StateTransition, LoopState> _transitions;
        public LoopState CurrentState { get; private set; }

        public StateMachine()
        {
            CurrentState = LoopState.Main;
            _transitions = new Dictionary<StateTransition, LoopState>
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
                { new StateTransition(LoopState.Options, "d"), LoopState.Options },
                { new StateTransition(LoopState.Options, "a"), LoopState.Options },
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

        private LoopState GetNext(string command)
        {
            StateTransition transition = new(CurrentState, command.ToLower());
            return !_transitions.TryGetValue(transition, out var nextState)
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
