using FoxEdit;
using UnityEngine;
using UnityEngine.Rendering;
using static Codice.CM.WorkspaceServer.DataStore.WkTree.WriteWorkspaceTree;

internal class VoxelRenderParams
{
    public RenderParams OpaqueRenderParams { get { return _opaqueRenderParams; } }
    public RenderParams TransparentRenderParams { get { return _transparentRenderParams; } }
    public Material StaticOpaqueMaterial { get { return _staticOpaqueMaterial; } }
    public Material StaticTransparentMaterial { get { return _staticTransparentMaterial; } }
    public bool HasStaticMaterials { get { return _hasStaticMaterials; } }

    private RenderParams _opaqueRenderParams;
    private RenderParams _transparentRenderParams;
    private Material _staticOpaqueMaterial = null;
    private Material _staticTransparentMaterial = null;

    private bool _hasAnimatedParams = false;
    private bool _hasStaticMaterials = false;

    internal VoxelRenderParams()
    {
        _hasStaticMaterials = false;
        _staticOpaqueMaterial = null;
        _staticTransparentMaterial = null;
    }

    internal void CreateStaticMaterial(bool opaque, bool transparent)
    {
        FoxEditSettings foxEditSettings = FoxEditSettings.GetSettings();
        Material voxelMaterial = foxEditSettings.Materials.voxelLitMaterial;

        if (opaque)
        {
            _staticOpaqueMaterial = new Material(voxelMaterial);
            _staticOpaqueMaterial.DisableKeyword("ANIMATED_VOXEL");
            _staticOpaqueMaterial.name = voxelMaterial.name + "_Opaque";
            _staticOpaqueMaterial.SetOverrideTag("RenderType", "Opaque");
            _staticOpaqueMaterial.renderQueue = (int)RenderQueue.Geometry;
            _staticOpaqueMaterial.SetInteger("_SrcBlend", (int)BlendMode.One);
            _staticOpaqueMaterial.SetInteger("_DstBlend", (int)BlendMode.Zero);
            _staticOpaqueMaterial.SetInteger("_ZWrite", 1);
        }
        if (transparent)
        {
            _staticTransparentMaterial = new Material(voxelMaterial);
            _staticTransparentMaterial.DisableKeyword("ANIMATED_VOXEL");
            _staticTransparentMaterial.name = voxelMaterial.name + "_Transparent";
            _staticTransparentMaterial.SetOverrideTag("RenderType", "Transparent");
            _staticTransparentMaterial.renderQueue = (int)RenderQueue.Transparent;
            _staticTransparentMaterial.SetInteger("_SrcBlend", (int)BlendMode.SrcAlpha);
            _staticTransparentMaterial.SetInteger("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            _staticTransparentMaterial.SetInteger("_ZWrite", 0);
        }

        _hasStaticMaterials = true;
    }

    internal void SetStaticMaterials(Material opaqueMaterial, Material transparentMaterial)
    {
        _staticOpaqueMaterial = opaqueMaterial;
        _staticTransparentMaterial = transparentMaterial;
        _hasStaticMaterials = true;
    }

    internal void CreateAnimatedParams()
    {
        FoxEditSettings foxEditSettings = FoxEditSettings.GetSettings();
        Material voxelMaterial = foxEditSettings.Materials.voxelLitMaterial;

        _opaqueRenderParams = new RenderParams(new Material(voxelMaterial));
        _opaqueRenderParams.material.EnableKeyword("ANIMATED_VOXEL");
        _opaqueRenderParams.matProps = new MaterialPropertyBlock();
        _opaqueRenderParams.shadowCastingMode = ShadowCastingMode.On;
        _opaqueRenderParams.matProps.SetBuffer("_VertexPositions", VoxelSharedData.FaceVertexBuffer);
        _opaqueRenderParams.material.SetOverrideTag("RenderType", "Opaque");
        _opaqueRenderParams.material.renderQueue = (int)RenderQueue.Geometry;
        _opaqueRenderParams.material.SetInteger("_SrcBlend", (int)BlendMode.One);
        _opaqueRenderParams.material.SetInteger("_DstBlend", (int)BlendMode.Zero);
        _opaqueRenderParams.material.SetInteger("_ZWrite", 1);

        _transparentRenderParams = new RenderParams(new Material(voxelMaterial));
        _transparentRenderParams.material.EnableKeyword("ANIMATED_VOXEL");
        _transparentRenderParams.matProps = new MaterialPropertyBlock();
        _transparentRenderParams.shadowCastingMode = ShadowCastingMode.On;
        _transparentRenderParams.matProps.SetBuffer("_VertexPositions", VoxelSharedData.FaceVertexBuffer);
        _transparentRenderParams.material.SetOverrideTag("RenderType", "Transparent");
        _transparentRenderParams.material.renderQueue = (int)RenderQueue.Transparent;
        _transparentRenderParams.material.SetInteger("_SrcBlend", (int)BlendMode.SrcAlpha);
        _transparentRenderParams.material.SetInteger("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        _transparentRenderParams.material.SetInteger("_ZWrite", 0);

        _hasAnimatedParams = true;
    }

    internal void UpdateStaticMaterials(GraphicsBuffer colorsBuffer)
    {
        if (!_hasStaticMaterials)
            return;

        _staticOpaqueMaterial?.SetInteger("_SrcBlend", (int)BlendMode.One);
        _staticOpaqueMaterial?.SetInteger("_DstBlend", (int)BlendMode.Zero);
        _staticOpaqueMaterial?.SetInteger("_ZWrite", 1);

        _staticTransparentMaterial?.SetInteger("_SrcBlend", (int)BlendMode.SrcAlpha);
        _staticTransparentMaterial?.SetInteger("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        _staticTransparentMaterial?.SetInteger("_ZWrite", 0);

        _staticOpaqueMaterial?.SetBuffer("_Colors", colorsBuffer);
        _staticOpaqueMaterial?.SetInt("_ColorCount", colorsBuffer.count);
        _staticTransparentMaterial?.SetBuffer("_Colors", colorsBuffer);
        _staticTransparentMaterial?.SetInt("_ColorCount", colorsBuffer.count);
    }

    internal void SetInstancesData(VoxelObject.AnimationFrames animation, int frameIndex)
    {
        if (!_hasAnimatedParams)
            return;

        if (animation.HasOpaqueFaces)
        {
            _opaqueRenderParams.matProps.SetInteger("_InstanceStartIndex", animation.OpaqueMesh.InstanceStartIndices[frameIndex]);
            _opaqueRenderParams.matProps.SetInt("_FacesCount", animation.OpaqueMesh.InstanceCount[frameIndex]);
        }
        if (animation.HasTransparentFaces)
        {
            _transparentRenderParams.matProps.SetInteger("_InstanceStartIndex", animation.TransparentMesh.InstanceStartIndices[frameIndex]);
            _transparentRenderParams.matProps.SetInt("_FacesCount", animation.TransparentMesh.InstanceCount[frameIndex]);
        }
    }

    internal void SetColorsBuffer(GraphicsBuffer colorsBuffer)
    {
        if (_hasAnimatedParams)
        {
            _opaqueRenderParams.matProps.SetBuffer("_Colors", colorsBuffer);
            _opaqueRenderParams.matProps.SetInt("_ColorCount", colorsBuffer.count);
            _transparentRenderParams.matProps.SetBuffer("_Colors", colorsBuffer);
            _transparentRenderParams.matProps.SetInt("_ColorCount", colorsBuffer.count);
        }

        if (_hasStaticMaterials)
        {
            _staticOpaqueMaterial?.SetBuffer("_Colors", colorsBuffer);
            _staticOpaqueMaterial?.SetInt("_ColorCount", colorsBuffer.count);
            _staticTransparentMaterial?.SetBuffer("_Colors", colorsBuffer);
            _staticTransparentMaterial?.SetInt("_ColorCount", colorsBuffer.count);
        }
    }

    internal void SetWorldBounds(Bounds bounds)
    {
        if (!_hasAnimatedParams)
            return;

        _opaqueRenderParams.worldBounds = bounds;
        _transparentRenderParams.worldBounds = bounds;
    }

    internal void SetBuffers(VoxelObject.AnimationFrames animation, VoxelRenderer.VoxelBuffers buffers)
    {
        if (!_hasAnimatedParams)
            return;

        if (animation.HasOpaqueFaces)
        {
            _opaqueRenderParams.matProps.SetBuffer("_Vertices", buffers.OpaqueVertices);
            _opaqueRenderParams.matProps.SetBuffer("_Quads", buffers.OpaqueQuads);
            _opaqueRenderParams.matProps.SetBuffer("_ObjectToWorldMatrices", buffers.Matrices);
        }
        if (animation.HasTransparentFaces)
        {
            _transparentRenderParams.matProps.SetBuffer("_Vertices", buffers.TransparentVertices);
            _transparentRenderParams.matProps.SetBuffer("_Quads", buffers.TransparentQuads);
            _transparentRenderParams.matProps.SetBuffer("_ObjectToWorldMatrices", buffers.Matrices);
        }
    }

    internal void SetVerticesAndQuads(bool hasOpaqueFaces, bool hasTransparentFaces, VoxelRenderer.VoxelBuffers buffers)
    {
        if (!_hasAnimatedParams)
            return;

        if (hasOpaqueFaces)
        {
            _opaqueRenderParams.matProps.SetBuffer("_Vertices", buffers.OpaqueVertices);
            _opaqueRenderParams.matProps.SetBuffer("_Quads", buffers.OpaqueQuads);
        }
        if (hasTransparentFaces)
        {
            _transparentRenderParams.matProps.SetBuffer("_Vertices", buffers.TransparentVertices);
            _transparentRenderParams.matProps.SetBuffer("_Quads", buffers.TransparentQuads);
        }
    }

    internal void SetObjectToWorldMatrix(Matrix4x4 objectToWorld)
    {
        if (!_hasAnimatedParams)
            return;

        _opaqueRenderParams.matProps.SetMatrix("_ObjectToWorld", objectToWorld);
        _transparentRenderParams.matProps.SetMatrix("_ObjectToWorld", objectToWorld);
    }
}
