using Autodesk.Fbx;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static FoxEdit.VoxelObject;

namespace FoxEdit
{
    internal class VoxelSaveSystem
    {
        internal static void Save(string meshName, string saveDirectory, VoxelRenderer voxelRenderer, VoxelPalette palette, int paletteIndex, List<VoxelEditorAnimation> animationList, ComputeShader computeStaticMesh)
        {
            string assetPath = GetAssetPath(meshName, saveDirectory, "asset");
            VoxelObject voxelObject = GetVoxelObject(voxelRenderer, assetPath);
            voxelObject = BinaryFillObject(voxelObject, animationList, palette, paletteIndex, saveDirectory, meshName);

            //FillObject(voxelObject, frameList, palette, paletteIndex);
            //string fbxPath = GetAssetPath(meshName, saveDirectory, "fbx");
            //voxelObject.StaticMesh = GetStaticMesh(voxelObject, fbxPath, meshName, computeStaticMesh);

            voxelRenderer.VoxelObject = voxelObject;
            EditorUtility.SetDirty(voxelRenderer);
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
                assetPath = $"{meshName}.{extension}";
            else
                assetPath = $"{saveDirectory}/{meshName}.{extension}";

            return assetPath;
        }

        private static VoxelObject GetVoxelObject(VoxelRenderer voxelRenderer, string assetPath)
        {
            VoxelObject voxelObject = AssetDatabase.LoadAssetAtPath<VoxelObject>(assetPath);

            if (voxelObject == null)
            {
                voxelObject = ScriptableObject.CreateInstance<VoxelObject>();
                AssetDatabase.CreateAsset(voxelObject, assetPath);
                EditorUtility.SetDirty(voxelRenderer);
                AssetDatabase.SaveAssets();
                voxelRenderer.VoxelObject = voxelObject;
            }

            return voxelObject;
        }

        #endregion AssetManagement

        private static VoxelObject BinaryFillObject(VoxelObject voxelObject, List<VoxelEditorAnimation> animationList, VoxelPalette palette, int paletteIndex, string saveDirectory, string meshName)
        {
            List<Vector3Int> minBounds = new List<Vector3Int>();
            List<Vector3Int> maxBounds = new List<Vector3Int>();

            List<EditorFrameVoxels> editorVoxelPositions = new List<EditorFrameVoxels>();

            bool[] isColorTransparent = palette.Colors.Select(material => material.Color.a < 1.0f).ToArray();

            List<Vector4> vertices = new List<Vector4>();
            List<int> startIndices = new List<int>();
            List<int> instanceCounts = new List<int>();
            AnimationFrames[] animationIndices = new AnimationFrames[animationList.Count];

            for (int animation = 0; animation < animationList.Count; animation++)
            {
                animationIndices[animation] = new AnimationFrames
                {
                    AnimName = animationList[animation].Name,
                    StartIndex = animation == 0 ? 0 : animationIndices[animation - 1].StartIndex + animationIndices[animation - 1].FrameCount,
                    FrameCount = animationList[animation].FramesCount
                };
                for (int frame = 0; frame < animationList[animation].FramesCount; frame++)
                {
                    VoxelObjectPackedFrameData packedData = animationList[animation][frame].GetPackedData(isColorTransparent);

                    VoxelData[] voxelData = packedData.Data;
                    EditorFrameVoxels editorVoxel = new VoxelObject.EditorFrameVoxels
                    {
                        VoxelPositions = packedData.VoxelPositions,
                        ColorIndices = packedData.ColorIndices
                    };
                    editorVoxelPositions.Add(editorVoxel);
                    minBounds.Add(packedData.MinBounds);
                    maxBounds.Add(packedData.MaxBounds);

                    List<Vector4> frameVertices = new List<Vector4>();
                    string completeMeshName = $"{meshName}_Animation{animation.ToString("00")}_Frame_{frame.ToString("00")}";
                    string fbxPath = GetAssetPath("SM_" + completeMeshName, saveDirectory, "fbx");
                    Mesh greedyMesh = GreedyMeshing(packedData, palette.PaletteSize, isColorTransparent, fbxPath, completeMeshName, out frameVertices);
                    startIndices.Add(vertices.Count / 4);
                    instanceCounts.Add(frameVertices.Count / 4);
                    vertices = vertices.Concat(frameVertices).ToList();

                    if (frame == 0 && animation == 0)
                        voxelObject.StaticMesh = greedyMesh;
                }
            }

            voxelObject.Bounds = CreateBounds(minBounds, maxBounds);
            voxelObject.PaletteIndex = paletteIndex;

            voxelObject.AnimationIndices = animationIndices;
            voxelObject.InstanceCount = instanceCounts.ToArray();
            voxelObject.InstanceStartIndices = startIndices.ToArray();
            voxelObject.EditorVoxelPositions = editorVoxelPositions.ToArray();

            voxelObject.Vertices = vertices.ToArray();

            return voxelObject;
        }

        private static Bounds CreateBounds(List<Vector3Int> minBounds, List<Vector3Int> maxBounds)
        {
            Vector3Int min = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
            Vector3Int max = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

            for (int i = 0; i < minBounds.Count; i++)
            {
                min.x = Mathf.Min(min.x, minBounds[i].x);
                min.y = Mathf.Min(min.y, minBounds[i].y);
                min.z = Mathf.Min(min.z, minBounds[i].z);

                max.x = Mathf.Max(max.x, maxBounds[i].x);
                max.y = Mathf.Max(max.y, maxBounds[i].y);
                max.z = Mathf.Max(max.z, maxBounds[i].z);
            }

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

            return bounds;
        }


        private static Mesh GreedyMeshing(VoxelObjectPackedFrameData data, int colorCount, bool[] isColorTransparent, string path, string meshName, out List<Vector4> frameVertices)
        {
            Vector3Int size = data.MinBounds - data.MaxBounds;
            size.x = Mathf.Abs(size.x) + 1;
            size.y = Mathf.Abs(size.y) + 1;
            size.z = Mathf.Abs(size.z) + 1;

            BitArray[][] binaryMasks = FillBinaryMasks(data, size, data.MinBounds);

            int[] colors = data.ColorIndices.GroupBy(color => color).Select(group => group.Key).ToArray();
            Dictionary<int, BitArray[][][]> greedyPlanes = FillGreedPlanes(data, binaryMasks, colors, size, data.MinBounds);

            //DebugGreedyPlanes(greedyPlanes, colors, palette);

            Dictionary<int, List<Rect>[][]> quads = Combine(greedyPlanes, colors);
            Mesh binaryMesh = GetBinaryStaticMesh(quads, size, data.MinBounds, path, meshName, out frameVertices);
            return binaryMesh;
        }

        private static Dictionary<int, List<Rect>[][]> Combine(Dictionary<int, BitArray[][][]> greedyPlanes, int[] colors)
        {
            Dictionary<int, List<Rect>[][]> quads = new Dictionary<int, List<Rect>[][]>();

            foreach (var color in colors)
            {
                quads[color] = new List<Rect>[6][];
                for (int axis = 0; axis < 6; axis++)
                {
                    quads[color][axis] = new List<Rect>[greedyPlanes[color][axis].Length];
                    for (int slice = 0; slice < greedyPlanes[color][axis].Length; slice++)
                    {
                        quads[color][axis][slice] = new List<Rect>();
                        for (int y = 0; y < greedyPlanes[color][axis][slice].Length; y++)
                        {
                            int x = 0;
                            int length = greedyPlanes[color][axis][slice][y].Length;
                            while (x < length)
                            {
                                BitArray clone = greedyPlanes[color][axis][slice][y].Clone() as BitArray;
                                clone.RightShift(x);
                                int newOffset = TrailingCount(clone, false);
                                x += newOffset;
                                if (x >= length)
                                    continue;

                                clone.RightShift(newOffset);
                                int width = Mathf.Max(1, TrailingCount(clone, true));
                                BitArray widthMask = new BitArray(length, false);
                                for (int w = 0; w < width; w++)
                                {
                                    widthMask.Set(w, true);
                                }
                                BitArray mask = widthMask.Clone() as BitArray;
                                mask = mask.LeftShift(x);
                                mask = mask.Not();

                                int height = 1;
                                while (y + height < greedyPlanes[color][axis][slice].Length)
                                {
                                    BitArray nextRow = greedyPlanes[color][axis][slice][y + height].Clone() as BitArray;
                                    nextRow = nextRow.RightShift(x);
                                    nextRow = nextRow.And(widthMask);
                                    if (!BitEqual(nextRow, widthMask))
                                        break;

                                    greedyPlanes[color][axis][slice][y + height] = greedyPlanes[color][axis][slice][y + height].And(mask);
                                    height += 1;
                                }

                                quads[color][axis][slice].Add(new Rect(x, y, width, height));
                                x += width;
                            }
                        }
                    }
                }
            }

            return quads;
        }

        private static bool BitEqual(BitArray array1, BitArray array2)
        {
            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                    return false;
            }
            return true;
        }

        private static int TrailingCount(BitArray array, bool target)
        {
            int length = array.Length;

            for (int i = 0; i < length; i++)
            {
                if (array[i] != target)
                    return i;
            }

            return length;
        }

        private static void DebugGreedyPlanes(Dictionary<int, BitArray[][][]> greedyPlanes, int[] colors, VoxelPalette palette)
        {
            List<string> directions = new List<string>()
            {
                "Right",
                "Left",
                "Up",
                "Down",
                "Forward",
                "Back"
            };

            string result = "";
            foreach (int color in colors)
            {
                result += $"Color => {palette.Colors[color].Color}:\n\n";
                for (int axis = 0; axis < 6; axis++)
                {
                    result += $"  Axis => {directions[axis]}:\n\n";
                    int start = 0;
                    int end = greedyPlanes[color][axis].Length;
                    int direction = 1;
                    if (axis == 1 || axis == 3 || axis == 5)
                    {
                        start = end - 1;
                        end = -1;
                        direction = -1;
                    }
                    for (int slice = start; slice != end; slice += direction)
                    //for (int slice = 0; slice < greedyPlanes[color][axis].Length; slice++)
                    {
                        int start2 = 0;
                        int end2 = greedyPlanes[color][axis][slice].Length;
                        int direction2 = 1;
                        if (axis == 0 || axis == 1 || axis == 3 || axis == 4 || axis == 5)
                        {
                            start2 = end2 - 1;
                            end2 = -1;
                            direction2 = -1;
                        }
                        for (int up = start2; up != end2; up += direction2)
                        //for (int up = 0; up < greedyPlanes[color][axis][slice].Length; up++)
                        {
                            result += "    ";
                            int start3 = 0;
                            int end3 = greedyPlanes[color][axis][slice][up].Length;
                            int direction3 = 1;
                            if (axis == 0 || axis == 5)
                            {
                                start3 = end3 - 1;
                                end3 = -1;
                                direction3 = -1;
                            }
                            for (int right = start3; right != end3; right += direction3)
                            //for (int x = 0; x < greedyPlanes[color][axis][slice][y].Length; x++)
                            {
                                result += greedyPlanes[color][axis][slice][up][right] ? "O" : "_";
                            }
                            result += "\n";
                        }
                        result += "\n";
                    }
                    result += "\n";
                }
                result += "\n";
            }
            Debug.Log(result);
        }

        private static BitArray[][] FillBinaryMasks(VoxelObjectPackedFrameData data, Vector3Int size, Vector3Int minBounds)
        {
            Vector3Int[] positions = data.VoxelPositions;

            BitArray[][] binarySlices = new BitArray[3][];

            binarySlices[0] = GetSlices(positions, Vector3Int.right, size.x, Vector3Int.forward, size.z, Vector3Int.up, size.y, minBounds);
            binarySlices[1] = GetSlices(positions, Vector3Int.up, size.y, Vector3Int.right, size.x, Vector3Int.forward, size.z, minBounds);
            binarySlices[2] = GetSlices(positions, Vector3Int.forward, size.z, Vector3Int.right, size.x, Vector3Int.up, size.y, minBounds);

            BitArray[][] binaryMasks = new BitArray[6][];

            for (int axis = 0; axis < 3; axis++)
            {
                binaryMasks[axis * 2] = new BitArray[binarySlices[axis].Length];
                binaryMasks[axis * 2 + 1] = new BitArray[binarySlices[axis].Length];

                for (int i = 0; i < binarySlices[axis].Length; i++)
                {
                    BitArray slice1 = binarySlices[axis][i].Clone() as BitArray;
                    BitArray rightSlice = binarySlices[axis][i].Clone() as BitArray;
                    rightSlice = rightSlice.LeftShift(1).Not();

                    BitArray slice2 = binarySlices[axis][i].Clone() as BitArray;
                    BitArray leftSlice = binarySlices[axis][i].Clone() as BitArray;
                    leftSlice = leftSlice.RightShift(1).Not();
                    binaryMasks[axis * 2][i] = slice1.And(rightSlice).Clone() as BitArray;
                    binaryMasks[axis * 2 + 1][i] = slice2.And(leftSlice).Clone() as BitArray;
                }
            }

            return binaryMasks;
        }


        private static BitArray[] GetSlices(Vector3Int[] voxelPositions, Vector3Int sliceAxis, int axisSize, Vector3Int xAxis, int xSize, Vector3Int yAxis, int ySize, Vector3Int minBounds)
        {
            //Padding is added at the start and end of a row
            int sliceSize = xSize * ySize;
            BitArray[] binarySlices = new BitArray[sliceSize];

            for (int i = 0; i < sliceSize; i++)
            {
                int x = i % xSize;
                int y = i / xSize;
                binarySlices[i] = new BitArray(axisSize + 2, false);
                for (int axisIndex = 1; axisIndex < axisSize + 1; axisIndex++)
                {
                    Vector3Int position = sliceAxis * (axisIndex - 1) + xAxis * x + yAxis * y;

                    if (voxelPositions.Contains(position + minBounds))
                    {
                        binarySlices[i].Set(axisIndex, true);
                    }
                }
            }

            return binarySlices;
        }

        private static Dictionary<int, BitArray[][][]> FillGreedPlanes(VoxelObjectPackedFrameData data, BitArray[][] binaryMasks, int[] colors, Vector3Int size, Vector3Int minBounds)
        {
            Dictionary<int, BitArray[][][]> greedyPlanes = new Dictionary<int, BitArray[][][]>();

            foreach (int color in colors)
            {
                greedyPlanes[color] = new BitArray[6][][];
            }

            for (int axis = 0; axis < 6; axis++)
            {
                int axisSize = (axis == 0 || axis == 1) ? size.x : (axis == 2 || axis == 3) ? size.y : size.z;
                int xSize = (axis == 0 || axis == 1) ? size.z : (axis == 2 || axis == 3) ? size.x : size.x;
                int ySize = (axis == 0 || axis == 1) ? size.y : (axis == 2 || axis == 3) ? size.z : size.y;
                int sliceSize = xSize * ySize;

                foreach (int color in colors)
                {
                    greedyPlanes[color][axis] = new BitArray[axisSize][];
                }

                for (int axisIndex = 0; axisIndex < axisSize; axisIndex++)
                {
                    foreach (int color in colors)
                    {
                        greedyPlanes[color][axis][axisIndex] = new BitArray[ySize];
                        for (int y = 0; y < ySize; y++)
                        {
                            greedyPlanes[color][axis][axisIndex][y] = new BitArray(xSize);
                        }
                    }

                    for (int i = 0; i < sliceSize; i++)
                    {
                        bool bitValue = binaryMasks[axis][i].Get(axisIndex + 1);
                        if (!bitValue)
                            continue;

                        int x = i % xSize;
                        int y = i / xSize;

                        Vector3Int voxelPosition = new Vector3Int(
                            (axis == 0 || axis == 1) ? axisIndex : (axis == 2 || axis == 3) ? x : x,
                            (axis == 0 || axis == 1) ? y : (axis == 2 || axis == 3) ? axisIndex : y,
                            (axis == 0 || axis == 1) ? x : (axis == 2 || axis == 3) ? y : axisIndex
                        );

                        VoxelData voxelData = data.Data.First(voxel => voxel.Position == voxelPosition + minBounds);
                        int colorIndex = voxelData.ColorIndex;
                        greedyPlanes[colorIndex][axis][axisIndex][y].Set(x, true);
                    }
                }
            }

            return greedyPlanes;
        }

        #region BinaryFbxCreation

        private static Mesh GetBinaryStaticMesh(Dictionary<int, List<Rect>[][]> quads, Vector3Int size, Vector3Int minBounds, string fbxPath, string meshName, out List<Vector4> frameVertices)
        {
            CreateBinaryFBX(fbxPath, quads, size, minBounds, meshName, out frameVertices);
            AssetDatabase.Refresh();
            GameObject meshGameObject = AssetDatabase.LoadAssetAtPath(fbxPath, typeof(GameObject)) as GameObject;
            return meshGameObject.GetComponent<MeshFilter>().sharedMesh;
        }

        private static void CreateBinaryFBX(string fbxPath, Dictionary<int, List<Rect>[][]> quads, Vector3Int size, Vector3Int minBounds, string meshName, out List<Vector4> frameVertices)
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
                    frameVertices = new List<Vector4>();
                    return;
                }

                FbxScene fbxScene = FbxScene.Create(fbxManager, "Voxel Scene");
                FbxDocumentInfo fbxSceneInfo = FbxDocumentInfo.Create(fbxManager, "Voxel Static Mesh");
                fbxSceneInfo.mTitle = meshName;
                fbxSceneInfo.mAuthor = "FoxEdit";
                fbxScene.SetSceneInfo(fbxSceneInfo);

                FbxNode mesh = CreateBinaryStaticMesh(fbxManager, quads, size, minBounds, meshName, out frameVertices);
                fbxScene.GetRootNode().AddChild(mesh);

                fbxExporter.Export(fbxScene);

                fbxScene.Destroy();
                fbxExporter.Destroy();
            }
        }

        private static FbxNode CreateBinaryStaticMesh(FbxManager fbxManager, Dictionary<int, List<Rect>[][]> quads, Vector3Int size, Vector3Int minBounds, string meshName, out List<Vector4> frameVertices)
        {
            FbxMesh fbxMesh = ConvertUnityMeshToBinaryFbxMesh(fbxManager, quads, size, minBounds, meshName, out frameVertices);

            FbxNode meshNode = FbxNode.Create(fbxManager, $"{meshName}");
            meshNode.LclTranslation.Set(new FbxDouble3(0.0, 0.0, 0.0));
            meshNode.LclRotation.Set(new FbxDouble3(0.0, 0.0, 0.0));
            meshNode.LclScaling.Set(new FbxDouble3(1.0, 1.0, 1.0));
            meshNode.SetNodeAttribute(fbxMesh);

            return meshNode;
        }

        private static FbxMesh ConvertUnityMeshToBinaryFbxMesh(FbxManager fbxManager, Dictionary<int, List<Rect>[][]> quads, Vector3Int size, Vector3Int minBounds, string meshName, out List<Vector4> frameVertices)
        {
            frameVertices = new List<Vector4>();

            FbxMesh fbxMesh = FbxMesh.Create(fbxManager, $"SM_{meshName}");
            fbxMesh.InitControlPoints(GetTotalQuadCount(quads) * 4);

            var normalElement = FbxLayerElementNormal.Create(fbxMesh, "Normals");
            normalElement.SetMappingMode(FbxLayerElement.EMappingMode.eByControlPoint);
            normalElement.SetReferenceMode(FbxLayerElement.EReferenceMode.eDirect);
            var normalArray = normalElement.GetDirectArray();

            var uvElement = FbxLayerElementUV.Create(fbxMesh, "UVs");
            uvElement.SetMappingMode(FbxLayerElement.EMappingMode.eByControlPoint);
            uvElement.SetReferenceMode(FbxLayerElement.EReferenceMode.eDirect);
            var uvArray = uvElement.GetDirectArray();

            int vertexIndex = 0;

            foreach (var color in quads.Keys)
            {
                for (int axis = 0; axis < 6; axis++)
                {
                    int axisSize = (axis == 0 || axis == 1) ? size.x : (axis == 2 || axis == 3) ? size.y : size.z;
                    int xSize = (axis == 0 || axis == 1) ? size.z : (axis == 2 || axis == 3) ? size.x : size.x;
                    int ySize = (axis == 0 || axis == 1) ? size.y : (axis == 2 || axis == 3) ? size.z : size.y;

                    for (int slice = 0; slice < axisSize; slice++)
                    {
                        List<Rect> quadList = quads[color][axis][slice];

                        for (int i = 0; i < quadList.Count; i++)
                        {
                            bool baseAxis = axis % 2 == 0;

                            Rect rect = quadList[i];
                            int axisPosition = slice;
                            if (axis == 0 || axis == 1)
                            {
                                axisPosition = axisSize - slice - 1;
                            }

                            int rightPosition = xSize - (int)rect.x - 1;
                            if (axis == 0 || axis == 1)
                            {
                                rightPosition = (int)rect.x;
                            }

                            int upPosition = (int)rect.y;

                            Vector3 voxelPosition = new Vector3
                            (
                                (axis == 0 || axis == 1) ? axisPosition : (axis == 2 || axis == 3) ? rightPosition : rightPosition,
                                (axis == 0 || axis == 1) ? upPosition : (axis == 2 || axis == 3) ? axisPosition : upPosition,
                                (axis == 0 || axis == 1) ? rightPosition : (axis == 2 || axis == 3) ? upPosition : axisPosition
                            ) * 10.0f;

                            if (axis == 5)
                            {
                                voxelPosition.z += 10.0f;
                            }

                            if (axis == 3)
                            {
                                voxelPosition.y += 10.0f;
                            }

                            if (axis == 1)
                            {
                                voxelPosition.x -= 10.0f;
                            }

                            int width = -(int)rect.width;
                            if (axis == 0 || axis == 1)
                            {
                                width = -width;
                            }
                            Vector3 widthVector = new Vector3
                            (
                                (axis == 0 || axis == 1) ? 0 : (axis == 2 || axis == 3) ? width : width,
                                (axis == 0 || axis == 1) ? 0 : (axis == 2 || axis == 3) ? 0 : 0,
                                (axis == 0 || axis == 1) ? width : (axis == 2 || axis == 3) ? 0 : 0
                            ) * 10.0f;

                            int height = (int)rect.height;
                            if (axis == 2)
                            {
                                height = -height;
                            }
                            Vector3 heightVector = new Vector3
                            (
                                (axis == 0 || axis == 1) ? 0 : (axis == 2 || axis == 3) ? 0 : 0,
                                (axis == 0 || axis == 1) ? height : (axis == 2 || axis == 3) ? 0 : height,
                                (axis == 0 || axis == 1) ? 0 : (axis == 2 || axis == 3) ? height : 0
                            ) * 10.0f;

                            if (axis == 2)
                            {
                                voxelPosition.z -= height * 10.0f;
                            }

                            voxelPosition += new Vector3(minBounds.x * 10.0f, minBounds.y * 10.0f, minBounds.z * 10.0f);

                            //Vertices
                            if (axis != 2 && axis != 4 && axis != 1 && axis != 3)
                            {
                                frameVertices.Add(new Vector4(voxelPosition.x * 0.01f, voxelPosition.y * 0.01f, voxelPosition.z * 0.01f, color));
                                fbxMesh.SetControlPointAt(new FbxVector4(voxelPosition.x, voxelPosition.y, voxelPosition.z + 10.0f, 1), vertexIndex + (i * 4));

                                voxelPosition += heightVector;
                                frameVertices.Add(new Vector4(voxelPosition.x * 0.01f, voxelPosition.y * 0.01f, voxelPosition.z * 0.01f, color));
                                fbxMesh.SetControlPointAt(new FbxVector4(voxelPosition.x, voxelPosition.y, voxelPosition.z + 10.0f, 1), vertexIndex + (i * 4) + 1);

                                voxelPosition -= heightVector;
                                voxelPosition += widthVector;
                                frameVertices.Add(new Vector4(voxelPosition.x * 0.01f, voxelPosition.y * 0.01f, voxelPosition.z * 0.01f, color));
                                fbxMesh.SetControlPointAt(new FbxVector4(voxelPosition.x, voxelPosition.y, voxelPosition.z + 10.0f, 1), vertexIndex + (i * 4) + 2);

                                voxelPosition += heightVector;
                                frameVertices.Add(new Vector4(voxelPosition.x * 0.01f, voxelPosition.y * 0.01f, voxelPosition.z * 0.01f, color));
                                fbxMesh.SetControlPointAt(new FbxVector4(voxelPosition.x, voxelPosition.y, voxelPosition.z + 10.0f, 1), vertexIndex + (i * 4) + 3);
                            }
                            else
                            {
                                voxelPosition += widthVector;
                                frameVertices.Add(new Vector4(voxelPosition.x * 0.01f, voxelPosition.y * 0.01f, voxelPosition.z * 0.01f, color));
                                fbxMesh.SetControlPointAt(new FbxVector4(voxelPosition.x, voxelPosition.y, voxelPosition.z + 10.0f, 1), vertexIndex + (i * 4));

                                voxelPosition += heightVector;
                                frameVertices.Add(new Vector4(voxelPosition.x * 0.01f, voxelPosition.y * 0.01f, voxelPosition.z * 0.01f, color));
                                fbxMesh.SetControlPointAt(new FbxVector4(voxelPosition.x, voxelPosition.y, voxelPosition.z + 10.0f, 1), vertexIndex + (i * 4) + 1);

                                voxelPosition -= widthVector;
                                voxelPosition -= heightVector;
                                frameVertices.Add(new Vector4(voxelPosition.x * 0.01f, voxelPosition.y * 0.01f, voxelPosition.z * 0.01f, color));
                                fbxMesh.SetControlPointAt(new FbxVector4(voxelPosition.x, voxelPosition.y, voxelPosition.z + 10.0f, 1), vertexIndex + (i * 4) + 2);

                                voxelPosition += heightVector;
                                frameVertices.Add(new Vector4(voxelPosition.x * 0.01f, voxelPosition.y * 0.01f, voxelPosition.z * 0.01f, color));
                                fbxMesh.SetControlPointAt(new FbxVector4(voxelPosition.x, voxelPosition.y, voxelPosition.z + 10.0f, 1), vertexIndex + (i * 4) + 3);
                            }

                            //Triangles
                            fbxMesh.BeginPolygon();
                            fbxMesh.AddPolygon(vertexIndex + 0 + i * 4);
                            fbxMesh.AddPolygon(vertexIndex + 1 + i * 4);
                            fbxMesh.AddPolygon(vertexIndex + 2 + i * 4);
                            fbxMesh.EndPolygon();

                            fbxMesh.BeginPolygon();
                            fbxMesh.AddPolygon(vertexIndex + 1 + i * 4);
                            fbxMesh.AddPolygon(vertexIndex + 3 + i * 4);
                            fbxMesh.AddPolygon(vertexIndex + 2 + i * 4);
                            fbxMesh.EndPolygon();

                            //if (axis != 2 && axis != 4 && axis != 1 && axis != 3)
                            //{
                            //    fbxMesh.BeginPolygon();
                            //    fbxMesh.AddPolygon(vertexIndex + 0 + i * 4);
                            //    fbxMesh.AddPolygon(vertexIndex + 1 + i * 4);
                            //    fbxMesh.AddPolygon(vertexIndex + 2 + i * 4);
                            //    fbxMesh.EndPolygon();

                            //    fbxMesh.BeginPolygon();
                            //    fbxMesh.AddPolygon(vertexIndex + 1 + i * 4);
                            //    fbxMesh.AddPolygon(vertexIndex + 3 + i * 4);
                            //    fbxMesh.AddPolygon(vertexIndex + 2 + i * 4);
                            //    fbxMesh.EndPolygon();
                            //}
                            //else
                            //{
                            //    fbxMesh.BeginPolygon();
                            //    fbxMesh.AddPolygon(vertexIndex + 2 + i * 4);
                            //    fbxMesh.AddPolygon(vertexIndex + 3 + i * 4);
                            //    fbxMesh.AddPolygon(vertexIndex + 0 + i * 4);
                            //    fbxMesh.EndPolygon();

                            //    fbxMesh.BeginPolygon();
                            //    fbxMesh.AddPolygon(vertexIndex + 3 + i * 4);
                            //    fbxMesh.AddPolygon(vertexIndex + 1 + i * 4);
                            //    fbxMesh.AddPolygon(vertexIndex + 0 + i * 4);
                            //    fbxMesh.EndPolygon();
                            //}

                            //Normals
                            Vector3 normal = new Vector3(
                                (axis == 0 || axis == 1) ? 1 : (axis == 2 || axis == 3) ? 0 : 0,
                                (axis == 0 || axis == 1) ? 0 : (axis == 2 || axis == 3) ? 1 : 0,
                                (axis == 0 || axis == 1) ? 0 : (axis == 2 || axis == 3) ? 0 : 1
                            ) * (axis == 1 || axis == 2 || axis == 4 ? -1 : 1);

                            FbxVector4 fbxNormal = new FbxVector4(normal.x, normal.y, normal.z, 0);
                            normalArray.Add(fbxNormal);
                            normalArray.Add(fbxNormal);
                            normalArray.Add(fbxNormal);
                            normalArray.Add(fbxNormal);

                            //UVs (used for color indices)
                            uvArray.Add(new FbxVector2(color, 0));
                            uvArray.Add(new FbxVector2(color, 0));
                            uvArray.Add(new FbxVector2(color, 0));
                            uvArray.Add(new FbxVector2(color, 0));
                        }

                        vertexIndex += quadList.Count * 4;
                    }
                }
            }
            fbxMesh.GetLayer(0).SetNormals(normalElement);
            fbxMesh.GetLayer(0).SetUVs(uvElement);
            return fbxMesh;
        }

        private static int GetTotalQuadCount(Dictionary<int, List<Rect>[][]> quads)
        {
            int count = 0;

            foreach (var color in quads.Keys)
            {
                for (int axis = 0; axis < 6; axis++)
                {
                    for (int slice = 0; slice < quads[color][axis].Length; slice++)
                    {
                        count += quads[color][axis][slice].Count;
                    }
                }
            }

            return count;
        }

        #endregion BinaryFbxCreation

        //#region SaveObject

        //private static void FillObject(VoxelObject voxelObject, List<VoxelEditorFrame> frameList, VoxelPalette palette, int paletteIndex)
        //{
        //    Vector3Int[] minBounds = new Vector3Int[frameList.Count];
        //    Vector3Int[] maxBounds = new Vector3Int[frameList.Count];

        //    List<Vector4> positionsAndColorIndices = new List<Vector4>();

        //    int[] faceIndices = new int[frameList.Count * 6];
        //    int[] frameFaceIndices = new int[6];
        //    int[] startIndices = new int[frameList.Count];
        //    int[] instanceCounts = new int[frameList.Count];
        //    int[] voxelIndices = new int[0];
        //    VoxelObject.EditorFrameVoxels[] editorVoxelPositions = new VoxelObject.EditorFrameVoxels[frameList.Count];

        //    int startIndex = 0;

        //    voxelObject.VoxelIndices = new int[6];
        //    List<int>[] voxelIndicesByFace = CreateListArray(6);

        //    bool[] isColorTransparent = palette.Colors.Select(material => material.Color.a < 1.0f).ToArray();
        //    for (int frame = 0; frame < frameList.Count; frame++)
        //    {
        //        VoxelObjectPackedFrameData packedData = frameList[frame].GetPackedData(isColorTransparent);

        //        VoxelData[] voxelData = packedData.Data;
        //        editorVoxelPositions[frame].VoxelPositions = packedData.VoxelPositions;
        //        editorVoxelPositions[frame].ColorIndices = packedData.ColorIndices;
        //        minBounds[frame] = packedData.MinBounds;
        //        maxBounds[frame] = packedData.MaxBounds;

        //        int instanceCount = 0;

        //        for (int voxel = 0; voxel < voxelData.Length; voxel++)
        //        {
        //            VoxelData data = voxelData[voxel];

        //            int voxelIndex = StorePositonAndColorIndex(positionsAndColorIndices, data);
        //            int faceCount = StoreIndicesByFace(voxelIndicesByFace, frameFaceIndices, data.GetFaces(), voxelIndex);

        //            instanceCount += faceCount;
        //        }

        //        SortIndices(faceIndices, frameFaceIndices, ref voxelIndices, voxelIndicesByFace, frame);

        //        startIndices[frame] = startIndex;
        //        instanceCounts[frame] = instanceCount;
        //        startIndex += instanceCount;

        //        ClearVoxelIndicesByFace(voxelIndicesByFace);
        //    }

        //    voxelObject.Bounds = CreateBounds(minBounds, maxBounds);
        //    voxelObject.PaletteIndex = paletteIndex;

        //    voxelObject.VoxelPositions = positionsAndColorIndices.Select(position => (Vector3)position).ToArray();
        //    voxelObject.VoxelIndices = voxelIndices;
        //    voxelObject.FaceIndices = faceIndices;
        //    voxelObject.ColorIndices = positionsAndColorIndices.Select(colorIndex => (int)colorIndex.w).ToArray();

        //    voxelObject.FrameCount = frameList.Count;
        //    voxelObject.InstanceCount = instanceCounts;
        //    voxelObject.MaxInstanceCount = instanceCounts.Max();
        //    voxelObject.InstanceStartIndices = startIndices;
        //    voxelObject.EditorVoxelPositions = editorVoxelPositions;
        //}

        //private static List<int>[] CreateListArray(int size)
        //{
        //    List<int>[] listArray = new List<int>[size];
        //    for (int i = 0; i < 6; i++)
        //    {
        //        listArray[i] = new List<int>();
        //    }
        //    return listArray;
        //}

        //private static int StorePositonAndColorIndex(List<Vector4> voxelData, VoxelData data)
        //{
        //    int index = 0;
        //    Vector4 positionAndColorIndex = new Vector4(data.Position.x, data.Position.y, data.Position.z, data.ColorIndex);

        //    if (!voxelData.Contains(positionAndColorIndex))
        //    {
        //        index = voxelData.Count;
        //        voxelData.Add(positionAndColorIndex);
        //    }
        //    else
        //    {
        //        index = voxelData.IndexOf(positionAndColorIndex);
        //    }

        //    return index;
        //}

        //private static int StoreIndicesByFace(List<int>[] voxelIndicesByFace, int[] frameFaceIndices, int[] faces, int voxelIndex)
        //{
        //    for (int index = 0; index < faces.Length; index++)
        //    {
        //        voxelIndicesByFace[faces[index]].Add(voxelIndex);
        //        frameFaceIndices[faces[index]] += 1;
        //    }

        //    return faces.Length;
        //}

        //private static void SortIndices(int[] faceIndices, int[] frameFaceIndices, ref int[] voxelIndices, List<int>[] voxelIndicesByFace, int frameIndex)
        //{
        //    int frameOffset = frameIndex * 6;

        //    for (int i = 0; i < 6; i++)
        //    {
        //        if (i != 0)
        //            frameFaceIndices[i] += faceIndices[i - 1 + frameOffset];
        //        faceIndices[i + frameOffset] = frameFaceIndices[i];
        //        frameFaceIndices[i] = 0;

        //        voxelIndices = voxelIndices.Concat(voxelIndicesByFace[i]).ToArray();
        //    }
        //}

        //private static void ClearVoxelIndicesByFace(List<int>[] voxelIndicesByFace)
        //{
        //    for (int i = 0; i < 6; i++)
        //    {
        //        voxelIndicesByFace[i].Clear();
        //    }
        //}

        //private static Bounds CreateBounds(Vector3Int[] minBounds, Vector3Int[] maxBounds)
        //{
        //    Vector3Int min = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
        //    Vector3Int max = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

        //    for (int i = 0; i < minBounds.Length; i++)
        //    {
        //        min.x = Mathf.Min(min.x, minBounds[i].x);
        //        min.y = Mathf.Min(min.y, minBounds[i].y);
        //        min.z = Mathf.Min(min.z, minBounds[i].z);

        //        max.x = Mathf.Max(max.x, maxBounds[i].x);
        //        max.y = Mathf.Max(max.y, maxBounds[i].y);
        //        max.z = Mathf.Max(max.z, maxBounds[i].z);
        //    }

        //    Bounds bounds = new Bounds();
        //    Vector3 center = (new Vector3(min.x + max.x + 1.0f, min.y + max.y + 1.0f, min.z + max.z + 1.0f) / 2.0f) * 0.1f;
        //    center.x -= 0.05f;
        //    center.z -= 0.05f;
        //    bounds.center = center;

        //    Vector3Int size = max - min;
        //    size.x = Mathf.Abs(size.x) + 1;
        //    size.y = Mathf.Abs(size.y) + 1;
        //    size.z = Mathf.Abs(size.z) + 1;

        //    bounds.extents = new Vector3((float)size.x / 2.0f, (float)size.y / 2.0f, (float)size.z / 2.0f) * 0.1f;

        //    return bounds;
        //}

        //#endregion SaveObject

        //#region FbxCreation

        //private static Mesh GetStaticMesh(VoxelObject voxelObject, string fbxPath, string meshName, ComputeShader computeStaticMesh)
        //{
        //    CreateFBX(fbxPath, voxelObject, meshName, computeStaticMesh);
        //    AssetDatabase.Refresh();
        //    GameObject meshGameObject = AssetDatabase.LoadAssetAtPath(fbxPath, typeof(GameObject)) as GameObject;
        //    return meshGameObject.GetComponent<MeshFilter>().sharedMesh;
        //}

        //private static void CreateFBX(string fbxPath, VoxelObject voxelObject, string meshName, ComputeShader computeStaticMesh)
        //{
        //    using (var fbxManager = FbxManager.Create())
        //    {
        //        FbxIOSettings fbxIOSettings = FbxIOSettings.Create(fbxManager, Globals.IOSROOT);

        //        fbxManager.SetIOSettings(fbxIOSettings);
        //        FbxExporter fbxExporter = FbxExporter.Create(fbxManager, "Exporter");
        //        int fileFormat = fbxManager.GetIOPluginRegistry().FindWriterIDByDescription("FBX ascii (*.fbx)");
        //        bool status = fbxExporter.Initialize(fbxPath, fileFormat, fbxIOSettings);

        //        if (!status)
        //        {
        //            Debug.LogError(string.Format("failed to initialize exporter, reason: {0}", fbxExporter.GetStatus().GetErrorString()));
        //            return;
        //        }

        //        FbxScene fbxScene = FbxScene.Create(fbxManager, "Voxel Scene");
        //        FbxDocumentInfo fbxSceneInfo = FbxDocumentInfo.Create(fbxManager, "Voxel Static Mesh");
        //        fbxSceneInfo.mTitle = $"{meshName}";
        //        fbxSceneInfo.mAuthor = "FoxEdit";
        //        fbxScene.SetSceneInfo(fbxSceneInfo);

        //        FbxNode mesh = CreateStaticMesh(fbxManager, voxelObject, meshName, computeStaticMesh);
        //        fbxScene.GetRootNode().AddChild(mesh);

        //        fbxExporter.Export(fbxScene);

        //        fbxScene.Destroy();
        //        fbxExporter.Destroy();
        //    }
        //}

        //private static FbxNode CreateStaticMesh(FbxManager fbxManager, VoxelObject voxelObject, string meshName, ComputeShader computeStaticMesh)
        //{
        //    Vector3[] positions = null;
        //    Vector3[] normals = null;
        //    ComputeVerticesPositionsAndNormals(out positions, out normals, voxelObject, computeStaticMesh);

        //    FbxMesh fbxMesh = ConvertUnityMeshToFbxMesh(fbxManager, positions, normals, voxelObject, meshName);

        //    FbxNode meshNode = FbxNode.Create(fbxManager, $"{meshName}");
        //    meshNode.LclTranslation.Set(new FbxDouble3(0.0, 0.0, 0.0));
        //    meshNode.LclRotation.Set(new FbxDouble3(0.0, 0.0, 0.0));
        //    meshNode.LclScaling.Set(new FbxDouble3(1.0, 1.0, 1.0));
        //    meshNode.SetNodeAttribute(fbxMesh);

        //    return meshNode;
        //}

        //#endregion FbxCreation

        //#region VerticesMaths

        //private static void ComputeVerticesPositionsAndNormals(out Vector3[] positions, out Vector3[] normals, VoxelObject voxelObject, ComputeShader computeStaticMesh)
        //{
        //    int kernel = computeStaticMesh.FindKernel("VoxelGeneration");

        //    //Voxel
        //    GraphicsBuffer voxelPositionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, voxelObject.VoxelPositions.Length, sizeof(float) * 3);
        //    voxelPositionBuffer.SetData(voxelObject.VoxelPositions);
        //    computeStaticMesh.SetBuffer(kernel, "_VoxelPositions", voxelPositionBuffer);

        //    GraphicsBuffer faceIndicesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, voxelObject.FaceIndices.Length, sizeof(int));
        //    faceIndicesBuffer.SetData(voxelObject.FaceIndices);
        //    computeStaticMesh.SetBuffer(kernel, "_FaceIndices", faceIndicesBuffer);

        //    GraphicsBuffer voxelIndicesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, voxelObject.VoxelIndices.Length, sizeof(int));
        //    voxelIndicesBuffer.SetData(voxelObject.VoxelIndices);
        //    computeStaticMesh.SetBuffer(kernel, "_VoxelIndices", voxelIndicesBuffer);

        //    Matrix4x4[] rotationMatrices = GetRotationMatrices();
        //    GraphicsBuffer rotationMatricesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 6, sizeof(float) * 16);
        //    rotationMatricesBuffer.SetData(rotationMatrices);
        //    computeStaticMesh.SetBuffer(kernel, "_RotationMatrices", rotationMatricesBuffer);

        //    GraphicsBuffer positionsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, voxelObject.InstanceCount[0] * 4, sizeof(float) * 3);
        //    computeStaticMesh.SetBuffer(kernel, "_VertexPosition", positionsBuffer);

        //    GraphicsBuffer normalsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, voxelObject.InstanceCount[0], sizeof(float) * 3);
        //    computeStaticMesh.SetBuffer(kernel, "_VertexNormals", normalsBuffer);

        //    int instanceCount = voxelObject.InstanceCount[0];
        //    computeStaticMesh.SetInt("_InstanceCount", instanceCount);

        //    uint threadGroupSize = 0;
        //    computeStaticMesh.GetKernelThreadGroupSizes(kernel, out threadGroupSize, out _, out _);
        //    int threadGroups = Mathf.CeilToInt((float)instanceCount / threadGroupSize);
        //    computeStaticMesh.Dispatch(kernel, threadGroups, 1, 1);

        //    positions = new Vector3[voxelObject.InstanceCount[0] * 4];
        //    normals = new Vector3[voxelObject.InstanceCount[0]];

        //    positionsBuffer.GetData(positions);
        //    normalsBuffer.GetData(normals);

        //    voxelPositionBuffer.Dispose();
        //    voxelIndicesBuffer.Dispose();
        //    faceIndicesBuffer.Dispose();
        //    rotationMatricesBuffer.Dispose();
        //    positionsBuffer.Dispose();
        //    normalsBuffer.Dispose();
        //}

        //private static Matrix4x4[] GetRotationMatrices()
        //{
        //    float halfPi = Mathf.PI / 2.0f;

        //    Matrix4x4[] rotationMatrices = new Matrix4x4[6];
        //    rotationMatrices[0] = GetRotationMatrixX(0);
        //    rotationMatrices[1] = GetRotationMatrixX(halfPi);
        //    rotationMatrices[2] = GetRotationMatrixX(halfPi * 2);
        //    rotationMatrices[3] = GetRotationMatrixX(-halfPi);
        //    rotationMatrices[4] = GetRotationMatrixZ(halfPi);
        //    rotationMatrices[5] = GetRotationMatrixZ(-halfPi);

        //    return rotationMatrices;
        //}

        //private static Matrix4x4 GetRotationMatrixX(float angle)
        //{
        //    float c = Mathf.Cos(angle);
        //    float s = Mathf.Sin(angle);

        //    return new Matrix4x4
        //    (
        //        new Vector4(1, 0, 0, 0),
        //        new Vector4(0, c, -s, 0),
        //        new Vector4(0, s, c, 0),
        //        new Vector4(0, 0, 0, 1)
        //    );
        //}

        //private static Matrix4x4 GetRotationMatrixZ(float angle)
        //{
        //    float c = Mathf.Cos(angle);
        //    float s = Mathf.Sin(angle);

        //    return new Matrix4x4
        //    (
        //        new Vector4(c, -s, 0, 0),
        //        new Vector4(s, c, 0, 0),
        //        new Vector4(0, 0, 1, 0),
        //        new Vector4(0, 0, 0, 1)
        //    );
        //}

        //private static FbxMesh ConvertUnityMeshToFbxMesh(FbxManager fbxManager, Vector3[] vertices, Vector3[] normals, VoxelObject voxelObject, string meshName)
        //{
        //    FbxMesh fbxMesh = FbxMesh.Create(fbxManager, $"SM_{meshName}");

        //    //Vertices
        //    fbxMesh.InitControlPoints(vertices.Length);
        //    for (int i = 0; i < vertices.Length; i++)
        //    {
        //        fbxMesh.SetControlPointAt(new FbxVector4(vertices[i].x, vertices[i].y, vertices[i].z, 1), i);
        //    }

        //    //Triangles
        //    for (int i = 0; i < vertices.Length / 4; i++)
        //    {
        //        fbxMesh.BeginPolygon();
        //        fbxMesh.AddPolygon(0 + i * 4);
        //        fbxMesh.AddPolygon(1 + i * 4);
        //        fbxMesh.AddPolygon(2 + i * 4);
        //        fbxMesh.EndPolygon();

        //        fbxMesh.BeginPolygon();
        //        fbxMesh.AddPolygon(0 + i * 4);
        //        fbxMesh.AddPolygon(2 + i * 4);
        //        fbxMesh.AddPolygon(3 + i * 4);
        //        fbxMesh.EndPolygon();
        //    }

        //    //Normals
        //    var normalElement = FbxLayerElementNormal.Create(fbxMesh, "Normals");
        //    normalElement.SetMappingMode(FbxLayerElement.EMappingMode.eByControlPoint);
        //    normalElement.SetReferenceMode(FbxLayerElement.EReferenceMode.eDirect);
        //    var normalArray = normalElement.GetDirectArray();
        //    for (int i = 0; i < normals.Length; i++)
        //    {
        //        FbxVector4 fbxNormal = new FbxVector4(normals[i].x, normals[i].y, normals[i].z, 0);
        //        normalArray.Add(fbxNormal);
        //        normalArray.Add(fbxNormal);
        //        normalArray.Add(fbxNormal);
        //        normalArray.Add(fbxNormal);
        //    }
        //    fbxMesh.GetLayer(0).SetNormals(normalElement);

        //    //UVs (used for color indices)
        //    var uvElement = FbxLayerElementUV.Create(fbxMesh, "UVs");
        //    uvElement.SetMappingMode(FbxLayerElement.EMappingMode.eByControlPoint);
        //    uvElement.SetReferenceMode(FbxLayerElement.EReferenceMode.eDirect);
        //    var uvArray = uvElement.GetDirectArray();
        //    for (int i = 0; i < vertices.Length / 4; i++)
        //    {
        //        int voxelIndex = voxelObject.VoxelIndices[i];
        //        int colorIndex = voxelObject.ColorIndices[voxelIndex];
        //        uvArray.Add(new FbxVector2(colorIndex, 0));
        //        uvArray.Add(new FbxVector2(colorIndex, 0));
        //        uvArray.Add(new FbxVector2(colorIndex, 0));
        //        uvArray.Add(new FbxVector2(colorIndex, 0));
        //    }
        //    fbxMesh.GetLayer(0).SetUVs(uvElement);

        //    return fbxMesh;
        //}

        //#endregion VerticesMaths
    }
}
