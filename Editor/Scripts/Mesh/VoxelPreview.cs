using Codice.Client.BaseCommands;
using FoxEdit;
using log4net.Util;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FoxEdit
{
    internal class VoxelPreview
    {
        private enum OpacityType
        {
            Both,
            Opaque,
            Transparent
        }

        private VoxelEditorFrame _frameToPreview = null;
        VoxelObjectPackedFrameData _frameData = default;

        private GraphicsBuffer _opaqueVerticesBuffer = null;
        private GraphicsBuffer _transparentVerticesBuffer = null;
        private GraphicsBuffer _opaqueQuadsBuffer = null;
        private GraphicsBuffer _transparentQuadsBuffer = null;

        private RenderParams _opaqueRenderParams;
        private RenderParams _transparentRenderParams;

        private bool _hasOpaqueFaces = false;
        private bool _hasTransparentFaces = false;
        private VoxelObject.MeshData _opaquePreview = default;
        private VoxelObject.MeshData _transparentPreview = default;

        private int _paletteIndex = -1;

        internal VoxelPreview(VoxelEditorFrame frame, int paletteIndex)
        {
            _frameToPreview = frame;
            _paletteIndex = paletteIndex;

            Initialize();
        }

        internal void Refresh()
        {
            GreedyMeshing();
            SetWorldBounds();
            SetVoxelBuffers();
            DrawPreview();
        }

        internal void ChangeFrame(VoxelEditorFrame frame)
        {
            _frameToPreview = frame;
            Initialize();
        }

        internal void Destroy()
        {
            DisposeBuffers(OpacityType.Both);
        }

        internal void SetPaletteIndex(int index)
        {
            _paletteIndex = index;
            SetColorBuffer();
        }

        private void Initialize()
        {
            if (_frameToPreview == null)
                return;

            GreedyMeshing();
            SetRenderParams();
            SetWorldBounds();
            SetColorBuffer();
            SetVoxelBuffers();
        }

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
            (int, int) instancesCount = VoxelSaveSystem.GreedyMeshing(_frameData, isColorTransparent, ref vertices, ref quads);

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
        }

        private void SetRenderParams()
        {
            FoxEditSettings foxEditSettings = FoxEditSettings.GetSettings();

            _opaqueRenderParams = new RenderParams(foxEditSettings.Materials.animatedOpaqueMaterial);
            _opaqueRenderParams.matProps = new MaterialPropertyBlock();
            _opaqueRenderParams.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            _opaqueRenderParams.matProps.SetBuffer("_VertexPositions", VoxelSharedData.FaceVertexBuffer);

            _transparentRenderParams = new RenderParams(foxEditSettings.Materials.animatedTransparentMaterial);
            _transparentRenderParams.matProps = new MaterialPropertyBlock();
            _transparentRenderParams.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            _transparentRenderParams.matProps.SetBuffer("_VertexPositions", VoxelSharedData.FaceVertexBuffer);
        }

        private void SetColorBuffer()
        {
            GraphicsBuffer colorsBuffer = VoxelSharedData.GetColorBuffer(_paletteIndex);
            if (colorsBuffer != null)
            {
                _opaqueRenderParams.matProps.SetBuffer("_Colors", colorsBuffer);
                _transparentRenderParams.matProps.SetBuffer("_Colors", colorsBuffer);
            }
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
            _opaqueRenderParams.worldBounds = bounds;
            _transparentRenderParams.worldBounds = bounds;

            _opaqueRenderParams.matProps.SetMatrix("_ObjectToWorld", _frameToPreview.VoxelTransform.localToWorldMatrix);
            _transparentRenderParams.matProps.SetMatrix("_ObjectToWorld", _frameToPreview.VoxelTransform.localToWorldMatrix);
        }

        private void SetVoxelBuffers()
        {
            if (_opaqueVerticesBuffer != null && (_opaqueVerticesBuffer.count != _opaquePreview.Vertices.Length || _opaqueQuadsBuffer.count != _opaquePreview.Quads.Length))
                DisposeBuffers(OpacityType.Opaque);
            if (_transparentVerticesBuffer != null && (_transparentVerticesBuffer.count != _transparentPreview.Vertices.Length || _transparentQuadsBuffer.count != _transparentPreview.Quads.Length))
                DisposeBuffers(OpacityType.Transparent);

            if (_opaqueVerticesBuffer == null && _hasOpaqueFaces)
                CreateBuffers(OpacityType.Opaque);
            if (_transparentVerticesBuffer == null && _hasTransparentFaces)
                CreateBuffers(OpacityType.Transparent);

            if (_hasOpaqueFaces)
                SetBufferData(OpacityType.Opaque);
            if (_hasTransparentFaces)
                SetBufferData(OpacityType.Transparent);
        }

        private void CreateBuffers(OpacityType opacityType)
        {
            if (opacityType == OpacityType.Opaque)
            {
                _opaqueVerticesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _opaquePreview.Vertices.Length, sizeof(float) * 3);
                _opaqueQuadsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _opaquePreview.Quads.Length, sizeof(int));
            }
            else if (opacityType == OpacityType.Transparent)
            {
                _transparentVerticesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _transparentPreview.Vertices.Length, sizeof(float) * 3);
                _transparentQuadsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _transparentPreview.Quads.Length, sizeof(int));
            }
        }

        private void SetBufferData(OpacityType opacityType)
        {
            if (opacityType == OpacityType.Opaque)
            {
                _opaqueVerticesBuffer.SetData(_opaquePreview.Vertices);
                _opaqueQuadsBuffer.SetData(_opaquePreview.Quads);
                _opaqueRenderParams.matProps.SetBuffer("_Vertices", _opaqueVerticesBuffer);
                _opaqueRenderParams.matProps.SetBuffer("_Quads", _opaqueQuadsBuffer);
            }
            else if (opacityType == OpacityType.Transparent)
            {
                _transparentVerticesBuffer.SetData(_transparentPreview.Vertices);
                _transparentQuadsBuffer.SetData(_transparentPreview.Quads);
                _transparentRenderParams.matProps.SetBuffer("_Vertices", _transparentVerticesBuffer);
                _transparentRenderParams.matProps.SetBuffer("_Quads", _transparentQuadsBuffer);
            }
        }

        private void DisposeBuffers(OpacityType opacityType)
        {
            if (opacityType == OpacityType.Both || opacityType == OpacityType.Opaque)
            {
                _opaqueVerticesBuffer?.Dispose();
                _opaqueVerticesBuffer = null;

                _opaqueQuadsBuffer?.Dispose();
                _opaqueQuadsBuffer = null;
            }
            if (opacityType == OpacityType.Both || opacityType == OpacityType.Transparent)
            {
                _transparentVerticesBuffer?.Dispose();
                _transparentVerticesBuffer = null;

                _transparentQuadsBuffer?.Dispose();
                _transparentQuadsBuffer = null;
            }
        }

        internal void DrawPreview()
        {
            if (_hasOpaqueFaces)
                Graphics.RenderPrimitivesIndexed(_opaqueRenderParams, MeshTopology.Triangles, VoxelSharedData.FaceTriangleBuffer, 6 /* 2 triangles */, instanceCount: _opaquePreview.InstanceCount[0]);
            if (_hasTransparentFaces)
                Graphics.RenderPrimitivesIndexed(_transparentRenderParams, MeshTopology.Triangles, VoxelSharedData.FaceTriangleBuffer, 6 /* 2 triangles */, instanceCount: _transparentPreview.InstanceCount[0]);
        }
    }
}