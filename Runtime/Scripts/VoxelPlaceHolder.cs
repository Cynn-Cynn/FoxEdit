using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoxEdit
{
    [ExecuteAlways]
    public class VoxelPlaceHolder : MonoBehaviour
    {
        [SerializeField][HideInInspector] public int ColorIndex = 0;

        private void OnDestroy()
        {
            GetComponentInParent<VoxelFrame>()?.OnCubeDeletion(transform.position);
        }
    }
}
