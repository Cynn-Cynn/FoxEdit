using Codice.Client.BaseCommands;
using FoxEdit;
using log4net.Util;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static FoxEdit.VoxelRenderer;

namespace FoxEdit
{
    internal class VoxelPreview
    {
        private VoxelEditorFrame _frameToPreview = null;
        private VoxelObjectPackedFrameData _frameData = default;
        private VoxelRenderer.VoxelBuffers _buffers = null;
        private VoxelRenderParams _renderParams = null;

        private bool _hasOpaqueFaces = false;
        private bool _hasTransparentFaces = false;
        private VoxelObject.MeshData _opaquePreview = default;
        private VoxelObject.MeshData _transparentPreview = default;

        private int _paletteIndex = -1;

        private bool _drawGrid = true;
        private List<Vector3> _gridVertices = null;
        private List<int> _gridQuads = null;
        private Color _gridColor = Color.black;

        #region Initialize

        internal VoxelPreview(VoxelEditorFrame frame, int paletteIndex, Color gridColor, bool drawGrid = false)
        {
            _frameToPreview = frame;
            _paletteIndex = paletteIndex;

            _gridColor = gridColor;
            _drawGrid = drawGrid;
            _gridVertices = new List<Vector3>();
            _gridQuads = new List<int>();
            _renderParams = new VoxelRenderParams();

            Initialize();
        }

        private void Initialize()
        {
            SetColorBuffer();

            if (_frameToPreview == null)
                return;

            GreedyMeshing();
            CreateBuffers();
            SetWorldBounds();
        }

        #endregion Initialize

        #region GreedyMeshing

        private void GreedyMeshing()
        {
            _frameData = _frameToPreview.GetPackedData();

            List<Vector3>[] vertices = new List<Vector3>[2];
            vertices[0] = new List<Vector3>(); //opaque
            vertices[1] = new List<Vector3>(); //transparent
            List<int>[] quads = new List<int>[2];
            quads[0] = new List<int>(); //opaque
            quads[1] = new List<int>(); //transparent

            bool[] isColorTransparent = VoxelSharedData.GetPalette(_paletteIndex).GetColorOpacities();
            (int, int) instancesCount = VoxelSaveSystem.GreedyMeshing(_frameData, isColorTransparent, ref vertices, ref quads, false);

            _opaquePreview = new VoxelObject.MeshData
            {
                InstanceStartIndices = new int[1] { 0 },
                InstanceCount = new int[1] { instancesCount.Item1 },
                Vertices = vertices[0].ToArray(),
                Quads = quads[0].ToArray()
            };
            _hasOpaqueFaces = instancesCount.Item1 != 0;

            _transparentPreview = new VoxelObject.MeshData
            {
                InstanceStartIndices = new int[1] { 0 },
                InstanceCount = new int[1] { instancesCount.Item2 },
                Vertices = vertices[1].ToArray(),
                Quads = quads[1].ToArray()
            };
            _hasTransparentFaces = instancesCount.Item2 != 0;

            GreedyMeshingForGrid();
        }

        private void GreedyMeshingForGrid()
        {
            if (!_drawGrid)
                return;

            List<Vector3>[] vertices = new List<Vector3>[2];
            vertices[0] = new List<Vector3>(); //opaque
            vertices[1] = new List<Vector3>(); //transparent
            List<int>[] quads = new List<int>[2];
            quads[0] = new List<int>(); //opaque
            quads[1] = new List<int>(); //transparent

            (int, int) instancesCount = VoxelSaveSystem.GreedyMeshing(_frameData, new bool[1] { false }, ref vertices, ref quads, true);
            _gridVertices = vertices[0];
            _gridQuads = quads[0];
        }

        #endregion GreedyMeshing

        #region Actions

        internal void ChangeFrame(VoxelEditorFrame frame)
        {
            _frameToPreview = frame;
            Refresh();
        }

        internal void SetPaletteIndex(int index)
        {
            _paletteIndex = index;
            SetColorBuffer();
        }

        internal void SetDrawGrid(bool value)
        {
            if (value == _drawGrid)
                return;

            _drawGrid = value;
            if (_drawGrid)
                GreedyMeshingForGrid();
        }

        internal void SetGridColor(Color color)
        {
            _gridColor = color;
        }

        internal void Refresh()
        {
            if (_frameToPreview == null)
                return;

            GreedyMeshing();
            SetWorldBounds();
            DisposeBuffers();
            CreateBuffers();
            DrawPreview();
        }

        internal void RefreshColors(bool refreshGreedyMeshing)
        {
            if (refreshGreedyMeshing)
            {
                GreedyMeshing();
                DisposeBuffers();
                CreateBuffers();
            }
            SetColorBuffer();
        }

        internal void Destroy()
        {
            DisposeBuffers();
        }

        #endregion Actions

        #region Buffers

        private void CreateBuffers()
        {
            if (_buffers != null)
                return;

            _buffers = new VoxelBuffers();
            if (_hasOpaqueFaces)
            {
                _buffers.OpaqueVertices = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _opaquePreview.Vertices.Length, sizeof(float) * 3);
                _buffers.OpaqueQuads = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _opaquePreview.Quads.Length, sizeof(int));
                _buffers.OpaqueVertices.SetData(_opaquePreview.Vertices);
                _buffers.OpaqueQuads.SetData(_opaquePreview.Quads);
            }
            if (_hasTransparentFaces)
            {
                _buffers.TransparentVertices = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _transparentPreview.Vertices.Length, sizeof(float) * 3);
                _buffers.TransparentQuads = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _transparentPreview.Quads.Length, sizeof(int));
                _buffers.TransparentVertices.SetData(_transparentPreview.Vertices);
                _buffers.TransparentQuads.SetData(_transparentPreview.Quads);
            }

            _renderParams.SetVerticesAndQuads(_hasOpaqueFaces, _hasTransparentFaces, _buffers);
        }

        private void DisposeBuffers()
        {
            if (_buffers == null)
                return;

            _buffers.OpaqueVertices?.Dispose();
            _buffers.OpaqueQuads?.Dispose();
            _buffers.TransparentVertices?.Dispose();
            _buffers.TransparentQuads?.Dispose();

            _buffers = null;
        }

        private void SetColorBuffer()
        {
            GraphicsBuffer colorsBuffer = VoxelSharedData.GetColorBuffer(_paletteIndex);
            _renderParams.SetColorsBuffer(colorsBuffer);
        }

        private void SetWorldBounds()
        {
            Vector3Int min = _frameData.MinBounds;
            Vector3Int max = _frameData.MaxBounds;

            Bounds bounds = new Bounds();
            Vector3 center = (new Vector3(min.x + max.x + 1.0f, min.y + max.y + 1.0f, min.z + max.z + 1.0f) / 2.0f) * 0.1f;
            center.x -= 0.05f;
            center.z -= 0.05f;
            bounds.center = center;

            Vector3Int size = max - min;
            size.x = Mathf.Abs(size.x) + 1;
            size.y = Mathf.Abs(size.y) + 1;
            size.z = Mathf.Abs(size.z) + 1;

            bounds.extents = new Vector3((float)size.x / 2.0f, (float)size.y / 2.0f, (float)size.z / 2.0f) * 0.1f;
            bounds.center += _frameToPreview.VoxelTransform.position;
            _renderParams.SetWorldBounds(bounds);
            _renderParams.SetObjectToWorldMatrix(_frameToPreview.VoxelTransform.localToWorldMatrix);
        }

        #endregion Buffers

        #region Draw

        internal void DrawPreview()
        {
            if (_hasOpaqueFaces)
                Graphics.RenderPrimitivesIndexed(_renderParams.OpaqueRenderParams, MeshTopology.Triangles, VoxelSharedData.FaceTriangleBuffer, 6 /* 2 triangles */, instanceCount: _opaquePreview.InstanceCount[0]);
            if (_hasTransparentFaces)
                Graphics.RenderPrimitivesIndexed(_renderParams.TransparentRenderParams, MeshTopology.Triangles, VoxelSharedData.FaceTriangleBuffer, 6 /* 2 triangles */, instanceCount: _transparentPreview.InstanceCount[0]);

            if (_drawGrid)
                DrawGrid();
        }

        private void DrawGrid()
        {
            Matrix4x4 localToWorld = _frameToPreview.VoxelTransform.localToWorldMatrix;

            for (int i = 0; i < _gridQuads.Count; i += 5)
            {
                Vector3 corner1 = localToWorld.MultiplyPoint(_gridVertices[_gridQuads[i]]);
                Vector3 corner2 = localToWorld.MultiplyPoint(_gridVertices[_gridQuads[i + 2]]);
                Vector3 distance = corner2 - corner1;

                int xSign = (int)(1 * Mathf.Sign(distance.x));
                int xLineCount = Mathf.RoundToInt(distance.x * 10.0f + xSign);
                if (xLineCount != xSign)
                {
                    for (int x = 0; x != xLineCount; x += xSign)
                    {
                        Vector3 point1 = corner1 + new Vector3(x * 0.1f, 0, 0);
                        Vector3 point2 = corner1 + new Vector3(x * 0.1f, distance.y, distance.z);
                        Debug.DrawLine(point1, point2, _gridColor, 0.01f, true);
                    }
                }

                int ySign = (int)(1 * Mathf.Sign(distance.y));
                int yLineCount = Mathf.RoundToInt(distance.y * 10.0f + ySign);
                if (yLineCount != ySign)
                {
                    for (int y = 0; y != yLineCount; y += ySign)
                    {
                        Vector3 point1 = corner1 + new Vector3(0, y * 0.1f, 0);
                        Vector3 point2 = corner1 + new Vector3(distance.x, y * 0.1f, distance.z);
                        Debug.DrawLine(point1, point2, _gridColor, 0.01f, true);
                    }
                }

                int zSign = (int)(1 * Mathf.Sign(distance.z));
                int zLineCount = Mathf.RoundToInt(distance.z * 10.0f + zSign);
                if (zLineCount != zSign)
                {
                    for (int z = 0; z != zLineCount; z += zSign)
                    {
                        Vector3 point1 = corner1 + new Vector3(0, 0, z * 0.1f);
                        Vector3 point2 = corner1 + new Vector3(distance.x, distance.y, z * 0.1f);
                        Debug.DrawLine(point1, point2, _gridColor, 0.01f, true);
                    }
                }
            }
        }

        #endregion Draw
    }
}