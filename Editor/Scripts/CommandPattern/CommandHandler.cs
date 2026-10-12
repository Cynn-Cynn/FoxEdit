using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoxEdit.Commands
{
    internal class CommandHandler<T> where T : ICommand
    {
        public Action<bool> OnCanUndoChanged;
        public Action<bool> OnCanRedoChanged;
        public Action OnUndoRedo;

        private Stack<T> _executedCommands = new Stack<T>();
        private Stack<T> _undoedCommands = new Stack<T>();

        public virtual void ExecuteCommand(T command)
        {
            command.Execute();
            _undoedCommands.Clear();
            _executedCommands.Push(command);
            SendCanEvents();
        }

        public bool CanUndo() => _executedCommands.Count > 0;
        public bool CanRedo() => _undoedCommands.Count > 0;

        public virtual void Undo()
        {
            if (!CanUndo())
                return;

            T undoed = _executedCommands.Pop();
            undoed.Undo();
            _undoedCommands.Push(undoed);
            SendCanEvents();
            OnUndoRedo?.Invoke();
        }

        public virtual void Redo()
        {
            if (!CanRedo())
                return;

            T redoed = _undoedCommands.Pop();
            redoed.Execute();
            _executedCommands.Push(redoed);
            OnUndoRedo?.Invoke();
            SendCanEvents();
        }

        public void SendCanEvents()
        {
            OnCanRedoChanged?.Invoke(CanRedo());
            OnCanUndoChanged?.Invoke(CanUndo());
        }
    }
}
