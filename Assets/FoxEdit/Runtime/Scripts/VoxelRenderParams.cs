using FoxEdit;
using UnityEngine;
using UnityEngine.Rendering;

internal class VoxelRenderParams
{
    public RenderParams OpaqueRenderParams { get { return _opaqueRenderParams; } }
    public RenderParams TransparentRenderParams { get { return _transparentRenderParams; } }

    private RenderParams _opaqueRenderParams;
    private RenderParams _transparentRenderParams;

    internal VoxelRenderParams()
    {
        FoxEditSettings foxEditSettings = FoxEditSettings.GetSettings();
        Material voxelMaterial = foxEditSettings.Materials.voxelLitMaterial;

        _opaqueRenderParams = new RenderParams(new Material(voxelMaterial));
        _opaqueRenderParams.matProps = new MaterialPropertyBlock();
        _opaqueRenderParams.shadowCastingMode = ShadowCastingMode.On;
        _opaqueRenderParams.matProps.SetBuffer("_VertexPositions", VoxelSharedData.FaceVertexBuffer);
        _opaqueRenderParams.material.SetInt("_SrcBlend", (int)BlendMode.One);
        _opaqueRenderParams.material.SetInt("_DstBlend", (int)BlendMode.Zero);
        _opaqueRenderParams.material.SetInt("_ZWrite", 1);
        _opaqueRenderParams.material.SetOverrideTag("RenderType", "Opaque");
        _opaqueRenderParams.material.renderQueue = (int)RenderQueue.Geometry;

        _transparentRenderParams = new RenderParams(new Material(voxelMaterial));
        _transparentRenderParams.matProps = new MaterialPropertyBlock();
        _transparentRenderParams.shadowCastingMode = ShadowCastingMode.On;
        _transparentRenderParams.matProps.SetBuffer("_VertexPositions", VoxelSharedData.FaceVertexBuffer);
        _transparentRenderParams.material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        _transparentRenderParams.material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        _transparentRenderParams.material.SetInt("_ZWrite", 0);
        _transparentRenderParams.material.SetOverrideTag("RenderType", "Transparent");
        _transparentRenderParams.material.renderQueue = (int)RenderQueue.Transparent;
    }

    internal void SetInstanceStartIndex(VoxelObject.AnimationFrames animation, int frameIndex)
    {
        if (animation.HasOpaqueFaces)
            _opaqueRenderParams.matProps.SetInteger("_InstanceStartIndex", animation.OpaqueMesh.InstanceStartIndices[frameIndex]);
        if (animation.HasTransparentFaces)
            _transparentRenderParams.matProps.SetInteger("_InstanceStartIndex", animation.TransparentMesh.InstanceStartIndices[frameIndex]);
    }

    internal void SetColorsBuffer(GraphicsBuffer colorsBuffer)
    {
        _opaqueRenderParams.matProps.SetBuffer("_Colors", colorsBuffer);
        _opaqueRenderParams.matProps.SetInt("_ColorCount", colorsBuffer.count);
        _transparentRenderParams.matProps.SetBuffer("_Colors", colorsBuffer);
        _transparentRenderParams.matProps.SetInt("_ColorCount", colorsBuffer.count);
    }

    internal void SetWorldBounds(Bounds bounds)
    {
        _opaqueRenderParams.worldBounds = bounds;
        _transparentRenderParams.worldBounds = bounds;
    }

    internal void SetVerticesAndQuads(VoxelObject.AnimationFrames animation, VoxelRenderer.VoxelBuffers buffers)
    {
        if (animation.HasOpaqueFaces)
        {
            _opaqueRenderParams.matProps.SetBuffer("_Vertices", buffers.OpaqueVertices);
            _opaqueRenderParams.matProps.SetBuffer("_Quads", buffers.OpaqueQuads);
        }
        if (animation.HasTransparentFaces)
        {
            _transparentRenderParams.matProps.SetBuffer("_Vertices", buffers.TransparentVertices);
            _transparentRenderParams.matProps.SetBuffer("_Quads", buffers.TransparentQuads);
        }
    }

    internal void SetObjectToWorldMatrix(Matrix4x4 objectToWorld)
    {
        _opaqueRenderParams.matProps.SetMatrix("_ObjectToWorld", objectToWorld);
        _transparentRenderParams.matProps.SetMatrix("_ObjectToWorld", objectToWorld);
    }
}
