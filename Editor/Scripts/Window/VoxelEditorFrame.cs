using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace FoxEdit
{
    internal class VoxelEditorFrame
    {
        private Transform _frameObject = null;
        private MeshRenderer _voxelPrefab = null;
        private FoxEditWindow _editWindow = null;
        private Dictionary<Vector3Int, VoxelEditorObject> _grid = null;

        #region Initialization

        internal VoxelEditorFrame(Transform parent, int frameIndex, MeshRenderer voxelPrefab, FoxEditWindow editWindow)
        {
            _frameObject = new GameObject("Frame_" + frameIndex.ToString("00")).transform;
            _frameObject.parent = parent;
            _frameObject.localPosition = Vector3.zero;
            _voxelPrefab = voxelPrefab;
            _editWindow = editWindow;

            _grid = new Dictionary<Vector3Int, VoxelEditorObject>();
        }

        internal void LoadFromSave(Vector3Int[] voxelPositions, int paletteIndex, int[] colorIndices)
        {
            for (int i = 0; i < voxelPositions.Length; i++)
            {
                _grid[voxelPositions[i]] = CreateVoxelObject(voxelPositions[i]);
                SetColor(voxelPositions[i], paletteIndex, colorIndices[i]);
            }
        }

        private void LoadFromCopy(Dictionary<Vector3Int, VoxelEditorObject> gridCopy)
        {
            _grid = gridCopy;
        }

        internal VoxelEditorFrame GetCopy(int newFrameIndex, int paletteIndex)
        {
            VoxelEditorFrame newFrame = new VoxelEditorFrame(_frameObject.parent, newFrameIndex, _voxelPrefab, _editWindow);
            Dictionary<Vector3Int, VoxelEditorObject> otherGrid = new Dictionary<Vector3Int, VoxelEditorObject>();

            foreach (Vector3Int gridPosition in _grid.Keys)
            {
                VoxelEditorObject voxelObject = newFrame.CreateVoxelObject(gridPosition);
                int colorIndex = _grid[gridPosition].ColorIndex;
                Material material = _editWindow.GetMaterial(paletteIndex, colorIndex);
                voxelObject.SetColor(material, colorIndex);

                otherGrid[gridPosition] = voxelObject;
            }

            newFrame.LoadFromCopy(otherGrid);
            return newFrame;
        }

        #endregion Initialization

        #region Editing

        internal void Show()
        {
            _frameObject.gameObject.SetActive(true);
        }

        internal void Hide()
        {
            _frameObject.gameObject.SetActive(false);
        }

        public bool TryAddVoxelNextTo(Vector3Int gridPosition, Vector3Int direction, int paletteIndex, int colorIndex)
        {
            if (!_grid.ContainsKey(gridPosition) && gridPosition != Vector3Int.zero)
                return false;

            Vector3Int newGridPosition = gridPosition + direction;

            if (_grid.ContainsKey(newGridPosition))
                return false;

            _grid[newGridPosition] = CreateVoxelObject(newGridPosition);
            SetColor(newGridPosition, paletteIndex, colorIndex);

            return true;
        }

        internal bool TryAddLayer(Vector3Int gridPosition, Vector3Int direction, int paletteIndex, int colorIndex, int baseColorIndex = -1)
        {
            if (!_grid.ContainsKey(gridPosition))
                return false;

            Vector3Int newGridPosition = gridPosition + direction;

            if (_grid.ContainsKey(newGridPosition))
                return false;

            if (baseColorIndex == -1)
                baseColorIndex = _grid[gridPosition].ColorIndex;
            else if (_grid[gridPosition].ColorIndex != baseColorIndex)
                return false;

            _grid[newGridPosition] = CreateVoxelObject(newGridPosition);
            SetColor(newGridPosition, paletteIndex, colorIndex);

            Vector3Int tangent = new Vector3Int(direction.z, direction.x, direction.y);
            TryAddLayer(gridPosition + tangent, direction, paletteIndex, colorIndex, baseColorIndex);
            TryAddLayer(gridPosition - tangent, direction, paletteIndex, colorIndex, baseColorIndex);

            Vector3Int bitangent = new Vector3Int(direction.y, direction.z, direction.x);
            TryAddLayer(gridPosition + bitangent, direction, paletteIndex, colorIndex, baseColorIndex);
            TryAddLayer(gridPosition - bitangent, direction, paletteIndex, colorIndex, baseColorIndex);

            return true;
        }

        internal bool TryRemoveVoxel(Vector3Int gridPosition)
        {
            if (!_grid.ContainsKey(gridPosition) || _grid.Count == 1)
                return false;

            _grid[gridPosition].Destroy();
            _grid.Remove(gridPosition);
            return true;
        }

        internal bool TryRemoveLayer(Vector3Int gridPosition, Vector3Int direction, int baseColorIndex = -1)
        {
            if (!_grid.ContainsKey(gridPosition) || _grid.Count == 1)
                return false;

            if (baseColorIndex == -1)
                baseColorIndex = _grid[gridPosition].ColorIndex;
            else if (_grid[gridPosition].ColorIndex != baseColorIndex)
                return false;

            _grid[gridPosition].Destroy();
            _grid.Remove(gridPosition);

            Vector3Int tangent = new Vector3Int(direction.z, direction.x, direction.y);
            TryRemoveLayer(gridPosition + tangent, direction, baseColorIndex);
            TryRemoveLayer(gridPosition - tangent, direction, baseColorIndex);

            Vector3Int bitangent = new Vector3Int(direction.y, direction.z, direction.x);
            TryRemoveLayer(gridPosition + bitangent, direction, baseColorIndex);
            TryRemoveLayer(gridPosition - bitangent, direction, baseColorIndex);

            return true;
        }

        internal bool TryColorVoxel(Vector3Int gridPosition, int paletteIndex, int colorIndex)
        {
            if (!_grid.ContainsKey(gridPosition) || _grid[gridPosition].ColorIndex == colorIndex)
                return false;

            SetColor(gridPosition, paletteIndex, colorIndex);
            return true;
        }

        internal bool TryFillColor(Vector3Int gridPosition, int paletteIndex, int colorIndex, int baseColorIndex = -1)
        {
            if (!_grid.ContainsKey(gridPosition) || _grid[gridPosition].ColorIndex == colorIndex)
                return false;

            if (baseColorIndex == -1)
                baseColorIndex = _grid[gridPosition].ColorIndex;
            else if (_grid[gridPosition].ColorIndex != baseColorIndex)
                return false;

            SetColor(gridPosition, paletteIndex, colorIndex);

            TryFillColor(gridPosition + Vector3Int.up, paletteIndex, colorIndex, baseColorIndex);
            TryFillColor(gridPosition + Vector3Int.down, paletteIndex, colorIndex, baseColorIndex);
            TryFillColor(gridPosition + Vector3Int.right, paletteIndex, colorIndex, baseColorIndex);
            TryFillColor(gridPosition + Vector3Int.left, paletteIndex, colorIndex, baseColorIndex);
            TryFillColor(gridPosition + Vector3Int.forward, paletteIndex, colorIndex, baseColorIndex);
            TryFillColor(gridPosition + Vector3Int.back, paletteIndex, colorIndex, baseColorIndex);

            return true;
        }

        private VoxelEditorObject CreateVoxelObject(Vector3Int gridPosition)
        {
            MeshRenderer voxelRenderer = GameObject.Instantiate(_voxelPrefab, _frameObject);
            voxelRenderer.name = "EditorVoxel";

            Vector3 localPosition = GridToLocalPosition(gridPosition);
            VoxelEditorObject voxelObject = new VoxelEditorObject(voxelRenderer, localPosition);

            return voxelObject;
        }

        private void SetColor(Vector3Int gridPosition, int paletteIndex, int colorIndex)
        {
            if (!_grid.ContainsKey(gridPosition))
                return;

            Material material = _editWindow.GetMaterial(paletteIndex, colorIndex);
            _grid[gridPosition].SetColor(material, colorIndex);
        }

        internal void Destroy()
        {
            _grid.Clear();
            GameObject.DestroyImmediate(_frameObject.gameObject);
        }

        #endregion Editing

        #region SpaceConversion

        private Vector3 GridToLocalPosition(Vector3Int position)
        {
            return new Vector3(position.x, position.y, position.z) * 0.1f;
        }

        public Vector3Int WorldToGridPosition(Vector3 worldPosition)
        {
            Vector3 localPosition = _frameObject.InverseTransformPoint(worldPosition);
            localPosition *= 10.0f;
            return new Vector3Int(Mathf.RoundToInt(localPosition.x), Mathf.RoundToInt(localPosition.y), Mathf.RoundToInt(localPosition.z));
        }

        public Vector3Int NormalToDirection(Vector3 normal)
        {
            Quaternion inverseRotation = Quaternion.Inverse(_frameObject.rotation);
            normal = inverseRotation * normal;

            return new Vector3Int(Mathf.RoundToInt(normal.x), Mathf.RoundToInt(normal.y), Mathf.RoundToInt(normal.z));
        }

        #endregion SpaceConversion

        #region SaveSystem

        internal VoxelObjectPackedFrameData GetPackedData(bool[] isColorTansparent)
        {
            Vector3Int minBounds;
            Vector3Int maxBounds;
            GetBounds(out minBounds, out maxBounds);

            VoxelObjectPackedFrameData packedData = new VoxelObjectPackedFrameData()
            {
                Data = GetVoxelData(isColorTansparent),
                MinBounds = minBounds,
                MaxBounds = maxBounds,
                VoxelPositions = _grid.Keys.ToArray(),
                ColorIndices = _grid.Values.Select(voxel => voxel.ColorIndex).ToArray()
            };

            return packedData;
        }

        private void GetBounds(out Vector3Int min, out Vector3Int max)
        {
            min = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
            max = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

            foreach (Vector3Int position in _grid.Keys)
            {
                min.x = Mathf.Min(position.x, min.x);
                min.y = Mathf.Min(position.y, min.y);
                min.z = Mathf.Min(position.z, min.z);

                max.x = Mathf.Max(position.x, max.x);
                max.y = Mathf.Max(position.y, max.y);
                max.z = Mathf.Max(position.z, max.z);
            }
        }

        private VoxelData[] GetVoxelData(bool[] isColorTransparent)
        {
            List<VoxelData> meshData = new List<VoxelData>();

            foreach (Vector3Int key in _grid.Keys)
            {
                VoxelData data = GetVisibleFaces(new VoxelData(key), key, isColorTransparent);
                data.ColorIndex = _grid[key].ColorIndex;
                if (data.GetFaces().Length != 0)
                    meshData.Add(data);
            }

            VoxelData[] opaqueMeshData = meshData.Where(mesh => !isColorTransparent[mesh.ColorIndex]).ToArray();
            VoxelData[] transparentMeshData = meshData.Where(mesh => isColorTransparent[mesh.ColorIndex]).ToArray();
            meshData = opaqueMeshData.Concat(transparentMeshData).ToList();

            return meshData.ToArray();
        }

        private VoxelData GetVisibleFaces(VoxelData meshData, Vector3Int key, bool[] isColorTransparent)
        {
            bool isTransparent = isColorTransparent[_grid[key].ColorIndex];

            if (IsFaceVisible(key + new Vector3Int(0, 1, 0), isTransparent, isColorTransparent))
                meshData.AddFace(0);
            if (IsFaceVisible(key + new Vector3Int(0, 0, -1), isTransparent, isColorTransparent))
                meshData.AddFace(1);
            if (IsFaceVisible(key + new Vector3Int(0, -1, 0), isTransparent, isColorTransparent))
                meshData.AddFace(2);
            if (IsFaceVisible(key + new Vector3Int(0, 0, 1), isTransparent, isColorTransparent))
                meshData.AddFace(3);
            if (IsFaceVisible(key + new Vector3Int(-1, 0, 0), isTransparent, isColorTransparent))
                meshData.AddFace(4);
            if (IsFaceVisible(key + new Vector3Int(1, 0, 0), isTransparent, isColorTransparent))
                meshData.AddFace(5);

            return meshData;
        }

        private bool IsFaceVisible(Vector3Int key, bool isTransparent, bool[] isColorTransparent)
        {
            if (!_grid.ContainsKey(key))
                return true;

            if (isColorTransparent[_grid[key].ColorIndex])
                return !isTransparent;

            return false;
        }

        #endregion SaveSystem
    }
}
