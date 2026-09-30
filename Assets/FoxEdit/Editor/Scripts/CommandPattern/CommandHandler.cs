using System.Collections.Generic;
using UnityEngine;

namespace FoxEdit.Commands
{
    internal class CommandHandler<T> where T : ICommand
    {
        private Stack<T> _executedCommands;
        private Stack<T> _undoedCommands;

        public void ExecuteCommand(T command)
        {
            command.Execute();
            _executedCommands.Push(command);
        }

        public void Undo()
        {
            if (_executedCommands.Count == 0)
                return;

            T undoed = _executedCommands.Pop();
            undoed.Undo();
        }

        public void Redo()
        {
            if (_undoedCommands.Count == 0)
                return;

            T redoed = _undoedCommands.Pop();
            redoed.Execute();
            _executedCommands.Push(redoed);
        }
    }
}
