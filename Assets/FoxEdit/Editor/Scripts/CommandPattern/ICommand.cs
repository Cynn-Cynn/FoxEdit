
namespace FoxEdit.Commands
{
    internal interface ICommand
    {
        public void Execute();
        public void Undo();
    }
}