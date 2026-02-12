using System.Collections.Generic;

namespace Chess
{
    public class CommandHistory
    {
        readonly Stack<IGameCommand> _undo = new();
        readonly Stack<IGameCommand> _redo = new();

        public bool Execute(IGameCommand cmd)
        {
            if (cmd == null) return false;

            bool ok = cmd.Execute();
            if (!ok) return false;

            _undo.Push(cmd);
            _redo.Clear();
            GameEvents.OnCommandExecuted?.Invoke(cmd);
            return true;
        }

        public bool CanUndo => _undo.Count > 0;
        public bool CanRedo => _redo.Count > 0;

        public bool Undo()
        {
            if (_undo.Count == 0) return false;

            var cmd = _undo.Pop();
            cmd.Undo();
            _redo.Push(cmd);
            GameEvents.OnCommandUndone?.Invoke(cmd);
            return true;
        }

        public bool Redo()
        {
            if (_redo.Count == 0) return false;

            var cmd = _redo.Pop();
            bool ok = cmd.Execute();
            if (!ok) return false;

            _undo.Push(cmd);
            GameEvents.OnCommandRedone?.Invoke(cmd);
            return true;
        }

        public void Clear()
        {
            _undo.Clear();
            _redo.Clear();
        }
        
        
    }
}