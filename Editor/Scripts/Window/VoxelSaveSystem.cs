using Autodesk.Fbx;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static FoxEdit.VoxelObject;

//TODO:
//Debug les bounds pour etre sure

namespace FoxEdit
{
    internal class VoxelSaveSystem
    {
        internal static void Save(string meshName, string saveDirectory, VoxelRenderer voxelRenderer, VoxelPalette palette, int paletteIndex, List<VoxelEditorAnimation> animationList, ComputeShader computeStaticMesh)
        {
            VoxelObject voxelObject = GetVoxelObject(voxelRenderer, meshName, saveDirectory);
            voxelObject = BinaryFillObject(voxelObject, animationList, palette, paletteIndex, saveDirectory, meshName);

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

        private static VoxelObject GetVoxelObject(VoxelRenderer voxelRenderer, string meshName, string saveDirectory)
        {
            VoxelObject voxelObject = AssetDatabase.LoadAssetAtPath<VoxelObject>(GetAssetPath(meshName, saveDirectory, "asset"));

            if (voxelObject == null)
                voxelObject = CreateVoxelObject(voxelRenderer, meshName, saveDirectory);

            return voxelObject;
        }

        private static VoxelObject CreateVoxelObject(VoxelRenderer voxelRenderer, string meshName, string saveDirectory)
        {
            string assetPath = GetAssetPath(meshName, saveDirectory, "asset");

            VoxelObject voxelObject = ScriptableObject.CreateInstance<VoxelObject>();
            AssetDatabase.CreateAsset(voxelObject, assetPath);
            voxelRenderer.VoxelObject = voxelObject;

            FoxEditSettings foxEditSettings = FoxEditSettings.GetSettings();

            Material animatedMaterialInstance = GameObject.Instantiate(foxEditSettings.Materials.animatedMaterial);
            AssetDatabase.CreateAsset(animatedMaterialInstance, GetAssetPath($"M_{meshName}_Animated", saveDirectory, "mat"));
            voxelObject.AnimatedMaterial = animatedMaterialInstance;

            Material staticMaterialInstance = GameObject.Instantiate(foxEditSettings.Materials.staticMaterial);
            AssetDatabase.CreateAsset(staticMaterialInstance, GetAssetPath($"M_{meshName}_Static", saveDirectory, "mat"));
            voxelObject.StaticMaterial = staticMaterialInstance;

            MeshRenderer staticRenderer = voxelRenderer.GetComponent<MeshRenderer>();
            staticRenderer.material = staticMaterialInstance;

            EditorUtility.SetDirty(staticRenderer);
            EditorUtility.SetDirty(voxelRenderer);
            EditorUtility.SetDirty(voxelObject);
            AssetDatabase.SaveAssets();

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
                    instanceCounts.Add(frameVertices.Count / 4);
                    startIndices.Add(vertices.Count / 4);
                    //TODO: modifier la liste direct pour ne pas append
                    vertices = vertices.Concat(frameVertices).ToList();

                    //TODO: generer que le static mesh de la frame 0 (ou une option pour choisir la frame ?)
                    if (frame == 0 && animation == 0)
                        voxelObject.StaticMesh = greedyMesh;
                }
            }

            voxelObject.Bounds = CreateBounds(minBounds, maxBounds);
            voxelObject.PaletteIndex = paletteIndex;

            voxelObject.AnimationIndices = animationIndices;
            voxelObject.InstanceStartIndices = startIndices.ToArray();
            voxelObject.InstanceCount = instanceCounts.ToArray();
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

            BitArray[][] binaryMasks = FillBinaryMasks(data, size);

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

        private static BitArray[][] FillBinaryMasks(VoxelObjectPackedFrameData data, Vector3Int size)
        {
            Vector3Int[] positions = data.VoxelPositions;
            BitArray[][] binarySlices = new BitArray[3][];

            binarySlices[0] = GetSlices(positions, Vector3Int.right, size.x, Vector3Int.forward, size.z, Vector3Int.up, size.y, data.MinBounds);
            binarySlices[1] = GetSlices(positions, Vector3Int.up, size.y, Vector3Int.right, size.x, Vector3Int.forward, size.z, data.MinBounds);
            binarySlices[2] = GetSlices(positions, Vector3Int.forward, size.z, Vector3Int.right, size.x, Vector3Int.up, size.y, data.MinBounds);

            BitArray[][] binaryMasks = new BitArray[6][];

            for (int axis = 0; axis < 3; axis++)
            {
                int length = binarySlices[axis].Length;
                binaryMasks[axis * 2] = new BitArray[length];
                binaryMasks[axis * 2 + 1] = new BitArray[length];

                for (int i = 0; i < length; i++)
                {
                    BitArray baseSlice = binarySlices[axis][i];
                    binaryMasks[axis * 2][i] = (baseSlice.Clone() as BitArray).LeftShift(1).Not().And(baseSlice);
                    binaryMasks[axis * 2 + 1][i] = (baseSlice.Clone() as BitArray).RightShift(1).Not().And(baseSlice);
                }
            }

            return binaryMasks;
        }


        private static BitArray[] GetSlices(Vector3Int[] voxelPositions, Vector3Int sliceAxis, int axisSize, Vector3Int xAxis, int xSize, Vector3Int yAxis, int ySize, Vector3Int minBounds)
        {
            //TODO : mettre tout en linéaire puis mettre un seul 0 de padding
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
                        binarySlices[i].Set(axisIndex, true);
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
                            Rect rect = quadList[i];
                            int axisPosition = slice;
                            int rightPosition = (int)rect.x;
                            int upPosition = (int)rect.y;

                            Vector3 voxelPosition = new Vector3
                            (
                                size.x - ((axis == 0 || axis == 1) ? axisPosition : (axis == 2 || axis == 3) ? rightPosition : rightPosition),
                                (axis == 0 || axis == 1) ? upPosition : (axis == 2 || axis == 3) ? axisPosition : upPosition,
                                (axis == 0 || axis == 1) ? rightPosition : (axis == 2 || axis == 3) ? upPosition : axisPosition
                            ) + minBounds + new Vector3(axis == 1 ? -1.0f : 0, axis == 3 ? 1.0f : 0, axis == 5 ? 1.0f : 0) + new Vector3(1.5f, 0.0f, -0.5f);
                            voxelPosition *= 10.0f;

                            int width = (int)rect.width;
                            Vector3 widthVector = new Vector3
                            (
                                -((axis == 0 || axis == 1) ? 0 : (axis == 2 || axis == 3) ? width : width),
                                (axis == 0 || axis == 1) ? 0 : (axis == 2 || axis == 3) ? 0 : 0,
                                (axis == 0 || axis == 1) ? width : (axis == 2 || axis == 3) ? 0 : 0
                            ) * 10.0f;

                            int height = (int)rect.height;
                            Vector3 heightVector = new Vector3
                            (
                                (axis == 0 || axis == 1) ? 0 : (axis == 2 || axis == 3) ? 0 : 0,
                                (axis == 0 || axis == 1) ? height : (axis == 2 || axis == 3) ? 0 : height,
                                (axis == 0 || axis == 1) ? 0 : (axis == 2 || axis == 3) ? height : 0
                            ) * 10.0f;


                            //Vertices
                            if (axis == 0 || axis == 2 || axis == 5)
                            {
                                frameVertices.Add(new Vector4(-voxelPosition.x * 0.01f, voxelPosition.y * 0.01f, voxelPosition.z * 0.01f, color));
                                fbxMesh.SetControlPointAt(new FbxVector4(voxelPosition.x, voxelPosition.y, voxelPosition.z, 1), vertexIndex + (i * 4));

                                voxelPosition += heightVector;
                                frameVertices.Add(new Vector4(-voxelPosition.x * 0.01f, voxelPosition.y * 0.01f, voxelPosition.z * 0.01f, color));
                                fbxMesh.SetControlPointAt(new FbxVector4(voxelPosition.x, voxelPosition.y, voxelPosition.z, 1), vertexIndex + (i * 4) + 1);

                                voxelPosition -= heightVector;
                                voxelPosition += widthVector;
                                frameVertices.Add(new Vector4(-voxelPosition.x * 0.01f, voxelPosition.y * 0.01f, voxelPosition.z * 0.01f, color));
                                fbxMesh.SetControlPointAt(new FbxVector4(voxelPosition.x, voxelPosition.y, voxelPosition.z, 1), vertexIndex + (i * 4) + 2);

                                voxelPosition += heightVector;
                                frameVertices.Add(new Vector4(-voxelPosition.x * 0.01f, voxelPosition.y * 0.01f, voxelPosition.z * 0.01f, color));
                                fbxMesh.SetControlPointAt(new FbxVector4(voxelPosition.x, voxelPosition.y, voxelPosition.z, 1), vertexIndex + (i * 4) + 3);
                            }
                            else
                            {
                                voxelPosition += widthVector;
                                frameVertices.Add(new Vector4(-voxelPosition.x * 0.01f, voxelPosition.y * 0.01f, voxelPosition.z * 0.01f, color));
                                fbxMesh.SetControlPointAt(new FbxVector4(voxelPosition.x, voxelPosition.y, voxelPosition.z, 1), vertexIndex + (i * 4));

                                voxelPosition += heightVector;
                                frameVertices.Add(new Vector4(-voxelPosition.x * 0.01f, voxelPosition.y * 0.01f, voxelPosition.z * 0.01f, color));
                                fbxMesh.SetControlPointAt(new FbxVector4(voxelPosition.x, voxelPosition.y, voxelPosition.z, 1), vertexIndex + (i * 4) + 1);

                                voxelPosition -= widthVector;
                                voxelPosition -= heightVector;
                                frameVertices.Add(new Vector4(-voxelPosition.x * 0.01f, voxelPosition.y * 0.01f, voxelPosition.z * 0.01f, color));
                                fbxMesh.SetControlPointAt(new FbxVector4(voxelPosition.x, voxelPosition.y, voxelPosition.z, 1), vertexIndex + (i * 4) + 2);

                                voxelPosition += heightVector;
                                frameVertices.Add(new Vector4(-voxelPosition.x * 0.01f, voxelPosition.y * 0.01f, voxelPosition.z * 0.01f, color));
                                fbxMesh.SetControlPointAt(new FbxVector4(voxelPosition.x, voxelPosition.y, voxelPosition.z, 1), vertexIndex + (i * 4) + 3);
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
    }
}
