using Autodesk.Fbx;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FoxEdit
{
    internal class VoxelSaveSystem
    {
        public static void Save(string meshName, string saveDirectory, VoxelRenderer voxelRenderer, VoxelPalette palette, int paletteIndex, List<VoxelFrame> frameList, ComputeShader computeStaticMesh)
        {
            string assetPath = GetAssetPath(meshName, saveDirectory, "asset");
            VoxelObject voxelObject = GetVoxelObject(voxelRenderer, assetPath);
            FillObject(voxelObject, frameList, palette, paletteIndex);

            string fbxPath = GetAssetPath(meshName, saveDirectory, "fbx");
            voxelObject.StaticMesh = GetStaticMesh(voxelObject, fbxPath, meshName, computeStaticMesh);


            EditorUtility.SetDirty(voxelObject);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = voxelObject;
        }

        #region AssetManagement

        private static string GetAssetPath(string meshName, string saveDirectory, string extension)
        {
            string assetPath = null;
            if (saveDirectory == null || saveDirectory == "")
                assetPath = $"Assets/{meshName}.{extension}";
            else
                assetPath = $"Assets/{saveDirectory}/{meshName}.{extension}";

            return assetPath;
        }

        private static VoxelObject GetVoxelObject(VoxelRenderer voxelRenderer, string assetPath)
        {
            VoxelObject voxelObject = voxelRenderer.VoxelObject;

            if (voxelObject == null)
            {
                voxelObject = ScriptableObject.CreateInstance<VoxelObject>();
                AssetDatabase.CreateAsset(voxelObject, assetPath);
                voxelRenderer.VoxelObject = voxelObject;
                EditorUtility.SetDirty(voxelRenderer);
            }

            return voxelObject;
        }

        #endregion AssetManagement

        #region SaveObject

        private static void FillObject(VoxelObject voxelObject, List<VoxelFrame> frameList, VoxelPalette palette, int paletteIndex)
        {
            Vector3Int[] minBounds = new Vector3Int[frameList.Count];
            Vector3Int[] maxBounds = new Vector3Int[frameList.Count];

            List<Vector4> positionsAndColorIndices = new List<Vector4>();

            int[] faceIndices = new int[frameList.Count * 6];
            int[] frameFaceIndices = new int[6];
            int[] startIndices = new int[frameList.Count];
            int[] instanceCounts = new int[frameList.Count];
            int[] voxelIndices = new int[0];
            VoxelObject.EditorFrameVoxels[] editorVoxelPositions = new VoxelObject.EditorFrameVoxels[frameList.Count];

            int startIndex = 0;

            voxelObject.VoxelIndices = new int[6];
            List<int>[] voxelIndicesByFace = CreateListArray(6);

            for (int frame = 0; frame < frameList.Count; frame++)
            {
                Vector3Int min;
                Vector3Int max;
                bool[] isColorTransparent = palette.Colors.Select(material => material.Color.a < 1.0f).ToArray();
                VoxelData[] voxelData = frameList[frame].GetMeshData(out min, out max, isColorTransparent);
                editorVoxelPositions[frame].VoxelPositions = frameList[frame].GetEditorVoxelPositions();
                editorVoxelPositions[frame].ColorIndices = frameList[frame].GetEditorVoxelColorIndices();
                minBounds[frame] = min;
                maxBounds[frame] = max;

                int instanceCount = 0;

                for (int voxel = 0; voxel < voxelData.Length; voxel++)
                {
                    VoxelData data = voxelData[voxel];

                    int voxelIndex = StorePositonAndColorIndex(positionsAndColorIndices, data);
                    int faceCount = StoreIndicesByFace(voxelIndicesByFace, frameFaceIndices, data.GetFaces(), voxelIndex);

                    instanceCount += faceCount;
                }

                SortIndices(faceIndices, frameFaceIndices, ref voxelIndices, voxelIndicesByFace, frame);

                startIndices[frame] = startIndex;
                instanceCounts[frame] = instanceCount;
                startIndex += instanceCount;

                ClearVoxelIndicesByFace(voxelIndicesByFace);
            }

            voxelObject.Bounds = CreateBounds(minBounds, maxBounds);
            voxelObject.PaletteIndex = paletteIndex;

            voxelObject.VoxelPositions = positionsAndColorIndices.Select(voxelData => (Vector3)voxelData).ToArray();
            voxelObject.VoxelIndices = voxelIndices;
            voxelObject.FaceIndices = faceIndices;
            voxelObject.ColorIndices = positionsAndColorIndices.Select(voxelData => (int)voxelData.w).ToArray();

            voxelObject.FrameCount = frameList.Count;
            voxelObject.InstanceCount = instanceCounts;
            voxelObject.MaxInstanceCount = instanceCounts.Max();
            voxelObject.InstanceStartIndices = startIndices;
            voxelObject.EditorVoxelPositions = editorVoxelPositions;
        }

        private static List<int>[] CreateListArray(int size)
        {
            List<int>[] listArray = new List<int>[size];
            for (int i = 0; i < 6; i++)
            {
                listArray[i] = new List<int>();
            }
            return listArray;
        }

        private static int StorePositonAndColorIndex(List<Vector4> voxelData, VoxelData data)
        {
            int index = 0;
            Vector4 positionAndColorIndex = new Vector4(data.Position.x, data.Position.y, data.Position.z, data.ColorIndex);

            if (!voxelData.Contains(positionAndColorIndex))
            {
                index = voxelData.Count;
                voxelData.Add(positionAndColorIndex);
            }
            else
            {
                index = voxelData.IndexOf(positionAndColorIndex);
            }

            return index;
        }

        private static int StoreIndicesByFace(List<int>[] voxelIndicesByFace, int[] frameFaceIndices, int[] faces, int voxelIndex)
        {
            for (int index = 0; index < faces.Length; index++)
            {
                voxelIndicesByFace[faces[index]].Add(voxelIndex);
                frameFaceIndices[faces[index]] += 1;
            }

            return faces.Length;
        }

        private static void SortIndices(int[] faceIndices, int[] frameFaceIndices, ref int[] voxelIndices, List<int>[] voxelIndicesByFace, int frameIndex)
        {
            int frameOffset = frameIndex * 6;

            for (int i = 0; i < 6; i++)
            {
                if (i != 0)
                    frameFaceIndices[i] += faceIndices[i - 1 + frameOffset];
                faceIndices[i + frameOffset] = frameFaceIndices[i];
                frameFaceIndices[i] = 0;

                voxelIndices = voxelIndices.Concat(voxelIndicesByFace[i]).ToArray();
            }
        }

        private static void ClearVoxelIndicesByFace(List<int>[] voxelIndicesByFace)
        {
            for (int i = 0; i < 6; i++)
            {
                voxelIndicesByFace[i].Clear();
            }
        }

        private static Bounds CreateBounds(Vector3Int[] minBounds, Vector3Int[] maxBounds)
        {
            Vector3Int min = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
            Vector3Int max = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

            for (int i = 0; i < minBounds.Length; i++)
            {
                min.x = Mathf.Min(min.x, minBounds[i].x);
                min.y = Mathf.Min(min.y, minBounds[i].y);
                min.z = Mathf.Min(min.z, minBounds[i].z);

                max.x = Mathf.Max(max.x, maxBounds[i].x);
                max.y = Mathf.Max(max.y, maxBounds[i].y);
                max.z = Mathf.Max(max.z, maxBounds[i].z);
            }

            Bounds bounds = new Bounds();
            bounds.center = (new Vector3(min.x + max.x + 1.0f, min.y + max.y + 1.0f, min.z + max.z + 1.0f) / 2.0f) * 0.1f;

            Vector3Int size = max - min;
            size.x = Mathf.Abs(size.x) + 1;
            size.y = Mathf.Abs(size.y) + 1;
            size.z = Mathf.Abs(size.z) + 1;

            bounds.extents = new Vector3((float)size.x / 2.0f, (float)size.y / 2.0f, (float)size.z / 2.0f) * 0.1f;

            return bounds;
        }

        #endregion SaveObject

        #region FbxCreation

        private static Mesh GetStaticMesh(VoxelObject voxelObject, string fbxPath, string meshName, ComputeShader computeStaticMesh)
        {
            CreateFBX(fbxPath, voxelObject, meshName, computeStaticMesh);
            AssetDatabase.Refresh();
            GameObject meshGameObject = AssetDatabase.LoadAssetAtPath(fbxPath, typeof(GameObject)) as GameObject;
            return meshGameObject.GetComponent<MeshFilter>().sharedMesh;
        }

        private static void CreateFBX(string fbxPath, VoxelObject voxelObject, string meshName, ComputeShader computeStaticMesh)
        {
            using (var fbxManager = FbxManager.Create())
            {
                FbxIOSettings fbxIOSettings = FbxIOSettings.Create(fbxManager, Globals.IOSROOT);

                fbxManager.SetIOSettings(fbxIOSettings);
                FbxExporter fbxExporter = FbxExporter.Create(fbxManager, "Exporter");
                int fileFormat = fbxManager.GetIOPluginRegistry().FindWriterIDByDescription("FBX ascii (*.fbx)");
                bool status = fbxExporter.Initialize(fbxPath, fileFormat, fbxIOSettings);

                if (!status)
                {
                    Debug.LogError(string.Format("failed to initialize exporter, reason: {0}", fbxExporter.GetStatus().GetErrorString()));
                    return;
                }

                FbxScene fbxScene = FbxScene.Create(fbxManager, "Voxel Scene");
                FbxDocumentInfo fbxSceneInfo = FbxDocumentInfo.Create(fbxManager, "Voxel Static Mesh");
                fbxSceneInfo.mTitle = $"{meshName}";
                fbxSceneInfo.mAuthor = "FoxEdit";
                fbxScene.SetSceneInfo(fbxSceneInfo);

                FbxNode mesh = CreateStaticMesh(fbxManager, voxelObject, meshName, computeStaticMesh);
                fbxScene.GetRootNode().AddChild(mesh);

                fbxExporter.Export(fbxScene);

                fbxScene.Destroy();
                fbxExporter.Destroy();
            }
        }

        private static FbxNode CreateStaticMesh(FbxManager fbxManager, VoxelObject voxelObject, string meshName, ComputeShader computeStaticMesh)
        {
            Vector3[] positions = null;
            Vector3[] normals = null;
            ComputeVerticesPositionsAndNormals(out positions, out normals, voxelObject, computeStaticMesh);

            FbxMesh fbxMesh = ConvertUnityMeshToFbxMesh(fbxManager, positions, normals, voxelObject, meshName);

            FbxNode meshNode = FbxNode.Create(fbxManager, $"{meshName}");
            meshNode.LclTranslation.Set(new FbxDouble3(0.0, 0.0, 0.0));
            meshNode.LclRotation.Set(new FbxDouble3(0.0, 0.0, 0.0));
            meshNode.LclScaling.Set(new FbxDouble3(1.0, 1.0, 1.0));
            meshNode.SetNodeAttribute(fbxMesh);

            return meshNode;
        }

        #endregion FbxCreation

        #region VerticesMaths

        private static void ComputeVerticesPositionsAndNormals(out Vector3[] positions, out Vector3[] normals, VoxelObject voxelObject, ComputeShader computeStaticMesh)
        {
            int kernel = computeStaticMesh.FindKernel("VoxelGeneration");

            //Voxel
            GraphicsBuffer voxelPositionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, voxelObject.VoxelPositions.Length, sizeof(float) * 3);
            voxelPositionBuffer.SetData(voxelObject.VoxelPositions);
            computeStaticMesh.SetBuffer(kernel, "_VoxelPositions", voxelPositionBuffer);

            GraphicsBuffer faceIndicesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, voxelObject.FaceIndices.Length, sizeof(int));
            faceIndicesBuffer.SetData(voxelObject.FaceIndices);
            computeStaticMesh.SetBuffer(kernel, "_FaceIndices", faceIndicesBuffer);

            GraphicsBuffer voxelIndicesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, voxelObject.VoxelIndices.Length, sizeof(int));
            voxelIndicesBuffer.SetData(voxelObject.VoxelIndices);
            computeStaticMesh.SetBuffer(kernel, "_VoxelIndices", voxelIndicesBuffer);

            Matrix4x4[] rotationMatrices = GetRotationMatrices();
            GraphicsBuffer rotationMatricesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 6, sizeof(float) * 16);
            rotationMatricesBuffer.SetData(rotationMatrices);
            computeStaticMesh.SetBuffer(kernel, "_RotationMatrices", rotationMatricesBuffer);

            GraphicsBuffer positionsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, voxelObject.InstanceCount[0] * 4, sizeof(float) * 3);
            computeStaticMesh.SetBuffer(kernel, "_VertexPosition", positionsBuffer);

            GraphicsBuffer normalsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, voxelObject.InstanceCount[0], sizeof(float) * 3);
            computeStaticMesh.SetBuffer(kernel, "_VertexNormals", normalsBuffer);

            int instanceCount = voxelObject.InstanceCount[0];
            computeStaticMesh.SetInt("_InstanceCount", instanceCount);

            uint threadGroupSize = 0;
            computeStaticMesh.GetKernelThreadGroupSizes(kernel, out threadGroupSize, out _, out _);
            int threadGroups = Mathf.CeilToInt((float)instanceCount / threadGroupSize);
            computeStaticMesh.Dispatch(kernel, threadGroups, 1, 1);

            positions = new Vector3[voxelObject.InstanceCount[0] * 4];
            normals = new Vector3[voxelObject.InstanceCount[0]];

            positionsBuffer.GetData(positions);
            normalsBuffer.GetData(normals);

            voxelPositionBuffer.Dispose();
            voxelIndicesBuffer.Dispose();
            faceIndicesBuffer.Dispose();
            rotationMatricesBuffer.Dispose();
            positionsBuffer.Dispose();
            normalsBuffer.Dispose();
        }

        private static Matrix4x4[] GetRotationMatrices()
        {
            float halfPi = Mathf.PI / 2.0f;

            Matrix4x4[] rotationMatrices = new Matrix4x4[6];
            rotationMatrices[0] = GetRotationMatrixX(0);
            rotationMatrices[1] = GetRotationMatrixX(halfPi);
            rotationMatrices[2] = GetRotationMatrixX(halfPi * 2);
            rotationMatrices[3] = GetRotationMatrixX(-halfPi);
            rotationMatrices[4] = GetRotationMatrixZ(halfPi);
            rotationMatrices[5] = GetRotationMatrixZ(-halfPi);

            return rotationMatrices;
        }

        private static Matrix4x4 GetRotationMatrixX(float angle)
        {
            float c = Mathf.Cos(angle);
            float s = Mathf.Sin(angle);

            return new Matrix4x4
            (
                new Vector4(1, 0, 0, 0),
                new Vector4(0, c, -s, 0),
                new Vector4(0, s, c, 0),
                new Vector4(0, 0, 0, 1)
            );
        }

        private static Matrix4x4 GetRotationMatrixZ(float angle)
        {
            float c = Mathf.Cos(angle);
            float s = Mathf.Sin(angle);

            return new Matrix4x4
            (
                new Vector4(c, -s, 0, 0),
                new Vector4(s, c, 0, 0),
                new Vector4(0, 0, 1, 0),
                new Vector4(0, 0, 0, 1)
            );
        }

        private static FbxMesh ConvertUnityMeshToFbxMesh(FbxManager fbxManager, Vector3[] vertices, Vector3[] normals, VoxelObject voxelObject, string meshName)
        {
            FbxMesh fbxMesh = FbxMesh.Create(fbxManager, $"SM_{meshName}");

            //Vertices
            fbxMesh.InitControlPoints(vertices.Length);
            for (int i = 0; i < vertices.Length; i++)
            {
                fbxMesh.SetControlPointAt(new FbxVector4(vertices[i].x, vertices[i].y, vertices[i].z, 1), i);
            }

            //Triangles
            for (int i = 0; i < vertices.Length / 4; i++)
            {
                fbxMesh.BeginPolygon();
                fbxMesh.AddPolygon(0 + i * 4);
                fbxMesh.AddPolygon(1 + i * 4);
                fbxMesh.AddPolygon(2 + i * 4);
                fbxMesh.EndPolygon();

                fbxMesh.BeginPolygon();
                fbxMesh.AddPolygon(0 + i * 4);
                fbxMesh.AddPolygon(2 + i * 4);
                fbxMesh.AddPolygon(3 + i * 4);
                fbxMesh.EndPolygon();
            }

            //Normals
            var normalElement = FbxLayerElementNormal.Create(fbxMesh, "Normals");
            normalElement.SetMappingMode(FbxLayerElement.EMappingMode.eByControlPoint);
            normalElement.SetReferenceMode(FbxLayerElement.EReferenceMode.eDirect);
            var normalArray = normalElement.GetDirectArray();
            for (int i = 0; i < normals.Length; i++)
            {
                FbxVector4 fbxNormal = new FbxVector4(normals[i].x, normals[i].y, normals[i].z, 0);
                normalArray.Add(fbxNormal);
                normalArray.Add(fbxNormal);
                normalArray.Add(fbxNormal);
                normalArray.Add(fbxNormal);
            }
            fbxMesh.GetLayer(0).SetNormals(normalElement);

            //UVs (used for color indices)
            var uvElement = FbxLayerElementUV.Create(fbxMesh, "UVs");
            uvElement.SetMappingMode(FbxLayerElement.EMappingMode.eByControlPoint);
            uvElement.SetReferenceMode(FbxLayerElement.EReferenceMode.eDirect);
            var uvArray = uvElement.GetDirectArray();
            for (int i = 0; i < vertices.Length / 4; i++)
            {
                int voxelIndex = voxelObject.VoxelIndices[i];
                int colorIndex = voxelObject.ColorIndices[voxelIndex];
                uvArray.Add(new FbxVector2(colorIndex, 0));
                uvArray.Add(new FbxVector2(colorIndex, 0));
                uvArray.Add(new FbxVector2(colorIndex, 0));
                uvArray.Add(new FbxVector2(colorIndex, 0));
            }
            fbxMesh.GetLayer(0).SetUVs(uvElement);

            return fbxMesh;
        }

        #endregion VerticesMaths
    }
}
