using UnityEditor.Compilation;
using UnityEngine;

namespace FoxEdit.EditorUtils
{
    internal class CompilationHook : MonoBehaviour
    {
        static CompilationHook()
        {
            CompilationPipeline.compilationStarted += OnCompilationStarted;
        }

        private static void OnCompilationStarted(object obj)
        {
            if (FoxEditManager.VoxelEditor != null)
            {
                FoxEditManager.StopEditVoxelObject();
                Debug.Log("test");
            }
        }

    }
}