using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class VoxelPlaceHolder : MonoBehaviour
{
    [SerializeField] [HideInInspector] public int ColorIndex = 0;

    private void OnDestroy()
    {
        GetComponentInParent<VoxelStructure>()?.OnCubeDeletion(transform.position);
    }
}
