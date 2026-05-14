using Autodesk.Fbx;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.VFX;
using static FoxEdit.VoxelObject;

namespace FoxEdit
{
    internal class VoxelSaveSystem
    {
        internal static void Save(string meshName, string saveDirectory, VoxelRenderer voxelRenderer, VoxelPalette palette, int paletteIndex, List<VoxelEditorAnimation> animationList, ComputeShader computeStaticMesh)
        {
            VoxelObject voxelObject = GetVoxelObject(voxelRenderer, meshName, saveDirectory);
            voxelObject = FillObject(voxelObject, animationList, palette, paletteIndex, saveDirectory, meshName);

            voxelRenderer.VoxelObject = voxelObject;
            EditorUtility.SetDirty(voxelRenderer);
            EditorUtility.SetDirty(voxelObject);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = voxelObject;
        }

        #region VoxelObjectCreation

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

        private static VoxelObject FillObject(VoxelObject voxelObject, List<VoxelEditorAnimation> editorAnimations, VoxelPalette palette, int paletteIndex, string saveDirectory, string meshName)
        {
            List<EditorFrameVoxels> editorVoxelPositions = new List<EditorFrameVoxels>();

            bool[] isColorTransparent = palette.Colors.Select(material => material.Color.a < 1.0f).ToArray();

            List<int> startIndices = new List<int>();
            List<int> instanceCounts = new List<int>();
            AnimationFrames[] animations = new AnimationFrames[editorAnimations.Count];

            for (int animationIndex = 0; animationIndex < editorAnimations.Count; animationIndex++)
            {
                animations[animationIndex] = new AnimationFrames
                {
                    AnimName = editorAnimations[animationIndex].Name,
                    FrameCount = editorAnimations[animationIndex].FramesCount,
                };

                List<Vector3Int> minBounds = new List<Vector3Int>();
                List<Vector3Int> maxBounds = new List<Vector3Int>();
                List<Vector3> animationVertices = new List<Vector3>();
                List<int> animationQuads = new List<int>();

                for (int frame = 0; frame < editorAnimations[animationIndex].FramesCount; frame++)
                {
                    VoxelObjectPackedFrameData packedData = editorAnimations[animationIndex][frame].GetPackedData(isColorTransparent);

                    VoxelData[] voxelData = packedData.Data;
                    EditorFrameVoxels editorVoxel = new VoxelObject.EditorFrameVoxels
                    {
                        VoxelPositions = packedData.VoxelPositions,
                        ColorIndices = packedData.ColorIndices
                    };
                    editorVoxelPositions.Add(editorVoxel);
                    minBounds.Add(packedData.MinBounds);
                    maxBounds.Add(packedData.MaxBounds);

                    startIndices.Add(animationQuads.Count() / 5);
                    int newQuadsCount = GreedyMeshing(packedData, isColorTransparent, ref animationVertices, ref animationQuads);
                    instanceCounts.Add(newQuadsCount);

                    if (frame == 0 && animationIndex == 0)
                        voxelObject.StaticMesh = CreateBinaryFBX(saveDirectory, $"SM_{meshName}", animationVertices, animationQuads);
                }

                animations[animationIndex].InstanceStartIndices = startIndices.ToArray();
                animations[animationIndex].InstanceCount = instanceCounts.ToArray();
                animations[animationIndex].Vertices = animationVertices.ToArray();
                animations[animationIndex].Quads = animationQuads.ToArray();
                animations[animationIndex].Bounds = CreateBounds(minBounds, maxBounds);
            }

            voxelObject.PaletteIndex = paletteIndex;
            voxelObject.Animations = animations;
            voxelObject.EditorVoxelPositions = editorVoxelPositions.ToArray();

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

        #endregion VoxelObjectCreation

        #region GreedyMeshing

        private static int GreedyMeshing(VoxelObjectPackedFrameData data, bool[] isColorTransparent, ref List<Vector3> vertices, ref List<int> quads)
        {
            Vector3Int size = data.MinBounds - data.MaxBounds;
            size.x = Mathf.Abs(size.x) + 1;
            size.y = Mathf.Abs(size.y) + 1;
            size.z = Mathf.Abs(size.z) + 1;
            int[] colors = data.ColorIndices.GroupBy(color => color).Select(group => group.Key).ToArray();

            BitArray[] binaryMasks = FillBinaryMasks(data, isColorTransparent, size);
            Dictionary<int, BitArray[][][]> greedyPlanes = FillGreedyPlanes(data, binaryMasks, colors, size, data.MinBounds);
            Dictionary<int, List<Rect>[][]> orderedQuads = Combine(greedyPlanes, colors);

            return CreateQuads(orderedQuads, size, data.MinBounds, ref vertices, ref quads);
        }

        private static BitArray[] FillBinaryMasks(VoxelObjectPackedFrameData data, bool[] isColorTransparent, Vector3Int size)
        {
            BitArray[] opaqueSlices = new BitArray[3];
            BitArray[] opaqueAndTransparentSlices = new BitArray[3];

            BitArray opaqueSlice;
            BitArray opaqueAndTransparentSlice;

            GetSlice(data, isColorTransparent, Vector3Int.right, size.x, Vector3Int.forward, size.z, Vector3Int.up, size.y, data.MinBounds, out opaqueSlice, out opaqueAndTransparentSlice);
            opaqueSlices[0] = opaqueSlice;
            opaqueAndTransparentSlices[0] = opaqueAndTransparentSlice;
            GetSlice(data, isColorTransparent, Vector3Int.up, size.y, Vector3Int.right, size.x, Vector3Int.forward, size.z, data.MinBounds, out opaqueSlice, out opaqueAndTransparentSlice);
            opaqueSlices[1] = opaqueSlice;
            opaqueAndTransparentSlices[1] = opaqueAndTransparentSlice;
            GetSlice(data, isColorTransparent, Vector3Int.forward, size.z, Vector3Int.right, size.x, Vector3Int.up, size.y, data.MinBounds, out opaqueSlice, out opaqueAndTransparentSlice);
            opaqueSlices[2] = opaqueSlice;
            opaqueAndTransparentSlices[2] = opaqueAndTransparentSlice;

            BitArray[] binaryMasks = new BitArray[6];

            for (int axis = 0; axis < 3; axis++)
            {
                int length = opaqueSlices[axis].Length;
                BitArray baseOpaqueSlice = opaqueSlices[axis];
                BitArray baseOpaqueAndTransparentSlice = opaqueAndTransparentSlices[axis];

                binaryMasks[axis * 2] = (baseOpaqueSlice.Clone() as BitArray).LeftShift(1).Not().And(baseOpaqueSlice).Or((baseOpaqueAndTransparentSlice.Clone() as BitArray).LeftShift(1).Not().And(baseOpaqueAndTransparentSlice));
                binaryMasks[axis * 2 + 1] = (baseOpaqueSlice.Clone() as BitArray).RightShift(1).Not().And(baseOpaqueSlice).Or((baseOpaqueAndTransparentSlice.Clone() as BitArray).RightShift(1).Not().And(baseOpaqueAndTransparentSlice));
            }

            return binaryMasks;
        }

        private static void GetSlice(VoxelObjectPackedFrameData data, bool[] isColorTransparent, Vector3Int sliceAxis, int axisSize, Vector3Int xAxis, int xSize, Vector3Int yAxis, int ySize, Vector3Int minBounds, out BitArray opaqueSlice, out BitArray opaqueAndTransparentSlice)
        {
            int axisSizeWithPadding = axisSize + 1;
            int totalSize = xSize * ySize * axisSizeWithPadding;
            int sliceSize = xSize * axisSizeWithPadding;
            opaqueSlice = new BitArray(totalSize + 1, false);
            opaqueAndTransparentSlice = new BitArray(totalSize + 1, false);

            for (int i = 1; i < totalSize; i++)
            {
                int sliceIndex = i % axisSizeWithPadding;
                if (sliceIndex == 0)
                    continue;

                int x = (i / axisSizeWithPadding) % xSize;
                int y = i / sliceSize;

                Vector3Int position = sliceAxis * (sliceIndex - 1) + xAxis * x + yAxis * y + minBounds;
                VoxelData voxelData = data.Data.FirstOrDefault(voxel => voxel.Position == position);
                if (voxelData != null)
                {
                    if (!isColorTransparent[voxelData.ColorIndex])
                        opaqueSlice.Set(i, true);
                    opaqueAndTransparentSlice.Set(i, true);
                }
            }
        }

        private static Dictionary<int, BitArray[][][]> FillGreedyPlanes(VoxelObjectPackedFrameData data, BitArray[] binaryMasks, int[] colors, Vector3Int size, Vector3Int minBounds)
        {
            Dictionary<int, BitArray[][][]> greedyPlanes = new Dictionary<int, BitArray[][][]>();

            foreach (int color in colors)
            {
                greedyPlanes[color] = new BitArray[6][][];
            }

            for (int axisIndex = 0; axisIndex < 6; axisIndex++)
            {
                int axisSize = (axisIndex == 0 || axisIndex == 1) ? size.x : (axisIndex == 2 || axisIndex == 3) ? size.y : size.z;
                int xSize = (axisIndex == 0 || axisIndex == 1) ? size.z : (axisIndex == 2 || axisIndex == 3) ? size.x : size.x;
                int ySize = (axisIndex == 0 || axisIndex == 1) ? size.y : (axisIndex == 2 || axisIndex == 3) ? size.z : size.y;
                int sliceSize = xSize * ySize;

                foreach (int color in colors)
                {
                    greedyPlanes[color][axisIndex] = new BitArray[axisSize][];
                }

                axisSize += 1;
                for (int sliceIndex = 0; sliceIndex < axisSize - 1; sliceIndex++)
                {
                    foreach (int color in colors)
                    {
                        greedyPlanes[color][axisIndex][sliceIndex] = new BitArray[ySize];
                        for (int y = 0; y < ySize; y++)
                        {
                            greedyPlanes[color][axisIndex][sliceIndex][y] = new BitArray(xSize);
                        }
                    }

                    for (int i = 0; i < sliceSize; i++)
                    {
                        bool hasFace = binaryMasks[axisIndex].Get(i * axisSize + sliceIndex + 1);
                        if (!hasFace)
                            continue;

                        int x = i % xSize;
                        int y = i / xSize;

                        Vector3Int voxelPosition = new Vector3Int(
                            (axisIndex == 0 || axisIndex == 1) ? sliceIndex : (axisIndex == 2 || axisIndex == 3) ? x : x,
                            (axisIndex == 0 || axisIndex == 1) ? y : (axisIndex == 2 || axisIndex == 3) ? sliceIndex : y,
                            (axisIndex == 0 || axisIndex == 1) ? x : (axisIndex == 2 || axisIndex == 3) ? y : sliceIndex
                        );

                        VoxelData voxelData = data.Data.First(voxel => voxel.Position == voxelPosition + minBounds);
                        int colorIndex = voxelData.ColorIndex;
                        greedyPlanes[colorIndex][axisIndex][sliceIndex][y].Set(x, true);
                    }
                }
            }

            return greedyPlanes;
        }

        private static Dictionary<int, List<Rect>[][]> Combine(Dictionary<int, BitArray[][][]> greedyPlanes, int[] colors)
        {
            Dictionary<int, List<Rect>[][]> orderedQuads = new Dictionary<int, List<Rect>[][]>();

            foreach (var colorIndex in colors)
            {
                orderedQuads[colorIndex] = new List<Rect>[6][];
                for (int axisIndex = 0; axisIndex < 6; axisIndex++)
                {
                    orderedQuads[colorIndex][axisIndex] = new List<Rect>[greedyPlanes[colorIndex][axisIndex].Length];
                    for (int sliceIndex = 0; sliceIndex < greedyPlanes[colorIndex][axisIndex].Length; sliceIndex++)
                    {
                        orderedQuads[colorIndex][axisIndex][sliceIndex] = new List<Rect>();
                        for (int y = 0; y < greedyPlanes[colorIndex][axisIndex][sliceIndex].Length; y++)
                        {
                            int x = 0;
                            int length = greedyPlanes[colorIndex][axisIndex][sliceIndex][y].Length;
                            while (x < length)
                            {
                                BitArray clone = greedyPlanes[colorIndex][axisIndex][sliceIndex][y].Clone() as BitArray;
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
                                while (y + height < greedyPlanes[colorIndex][axisIndex][sliceIndex].Length)
                                {
                                    BitArray nextRow = greedyPlanes[colorIndex][axisIndex][sliceIndex][y + height].Clone() as BitArray;
                                    nextRow = nextRow.RightShift(x);
                                    nextRow = nextRow.And(widthMask);
                                    if (!BitEqual(nextRow, widthMask))
                                        break;

                                    greedyPlanes[colorIndex][axisIndex][sliceIndex][y + height] = greedyPlanes[colorIndex][axisIndex][sliceIndex][y + height].And(mask);
                                    height += 1;
                                }

                                orderedQuads[colorIndex][axisIndex][sliceIndex].Add(new Rect(x, y, width, height));
                                x += width;
                            }
                        }
                    }
                }
            }

            return orderedQuads;
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

        private static int CreateQuads(Dictionary<int, List<Rect>[][]> orderedQuads, Vector3Int size, Vector3Int minBounds, ref List<Vector3> vertices, ref List<int> quads)
        {
            int quadsCount = 0;

            foreach (var color in orderedQuads.Keys)
            {
                for (int axis = 0; axis < 6; axis++)
                {
                    int axisSize = (axis == 0 || axis == 1) ? size.x : (axis == 2 || axis == 3) ? size.y : size.z;
                    int xSize = (axis == 0 || axis == 1) ? size.z : (axis == 2 || axis == 3) ? size.x : size.x;
                    int ySize = (axis == 0 || axis == 1) ? size.y : (axis == 2 || axis == 3) ? size.z : size.y;

                    for (int slice = 0; slice < axisSize; slice++)
                    {
                        List<Rect> quadList = orderedQuads[color][axis][slice];

                        for (int i = 0; i < quadList.Count; i++)
                        {
                            Rect rect = quadList[i];
                            int axisPosition = slice;
                            int rightPosition = (int)rect.x;
                            int upPosition = (int)rect.y;

                            Vector3 voxelPosition = new Vector3
                            (
                                (axis == 0 || axis == 1) ? axisPosition : (axis == 2 || axis == 3) ? rightPosition : rightPosition,
                                (axis == 0 || axis == 1) ? upPosition : (axis == 2 || axis == 3) ? axisPosition : upPosition,
                                (axis == 0 || axis == 1) ? rightPosition : (axis == 2 || axis == 3) ? upPosition : axisPosition
                            ) + minBounds + new Vector3(axis == 1 ? 0.5f : -0.5f, axis == 3 ? 1.0f : 0.0f, axis == 5 ? 0.5f : -0.5f);
                            voxelPosition *= 0.1f;

                            int width = (int)rect.width;
                            Vector3 widthVector = new Vector3
                            (
                                (axis == 0 || axis == 1) ? 0 : (axis == 2 || axis == 3) ? width : width,
                                (axis == 0 || axis == 1) ? 0 : (axis == 2 || axis == 3) ? 0 : 0,
                                (axis == 0 || axis == 1) ? width : (axis == 2 || axis == 3) ? 0 : 0
                            ) * 0.1f;

                            int height = (int)rect.height;
                            Vector3 heightVector = new Vector3
                            (
                                (axis == 0 || axis == 1) ? 0 : (axis == 2 || axis == 3) ? 0 : 0,
                                (axis == 0 || axis == 1) ? height : (axis == 2 || axis == 3) ? 0 : height,
                                (axis == 0 || axis == 1) ? 0 : (axis == 2 || axis == 3) ? height : 0
                            ) * 0.1f;

                            if (axis == 0 || axis == 2 || axis == 5)
                            {
                                AddVertex(voxelPosition, ref vertices, ref quads);

                                voxelPosition += heightVector;
                                AddVertex(voxelPosition, ref vertices, ref quads);

                                voxelPosition -= heightVector;
                                voxelPosition += widthVector;
                                AddVertex(voxelPosition, ref vertices, ref quads);

                                voxelPosition += heightVector;
                                AddVertex(voxelPosition, ref vertices, ref quads);
                            }
                            else
                            {
                                voxelPosition += widthVector;
                                AddVertex(voxelPosition, ref vertices, ref quads);

                                voxelPosition += heightVector;
                                AddVertex(voxelPosition, ref vertices, ref quads);

                                voxelPosition -= widthVector;
                                voxelPosition -= heightVector;
                                AddVertex(voxelPosition, ref vertices, ref quads);

                                voxelPosition += heightVector;
                                AddVertex(voxelPosition, ref vertices, ref quads);
                            }

                            quads.Add(color);
                            quadsCount += 1;
                        }
                    }
                }
            }
            return quadsCount;
        }

        private static void AddVertex(Vector3 vertex, ref List<Vector3> vertices, ref List<int> quads)
        {
            int index = vertices.IndexOf(vertex);
            if (index != -1)
            {
                quads.Add(index);
            }
            else
            {
                vertices.Add(vertex);
                quads.Add(vertices.Count - 1);
            }
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

        #endregion GreedyMeshing

        #region BinaryFbxCreation

        private static Mesh CreateBinaryFBX(string saveDirectory, string meshName, List<Vector3> frameVertices, List<int> frameQuads)
        {
            string fbxPath = GetAssetPath(meshName, saveDirectory, "fbx");

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
                    return null;
                }

                FbxScene fbxScene = FbxScene.Create(fbxManager, "Voxel Scene");
                FbxDocumentInfo fbxSceneInfo = FbxDocumentInfo.Create(fbxManager, "Voxel Static Mesh");
                fbxSceneInfo.mTitle = meshName;
                fbxSceneInfo.mAuthor = "FoxEdit";
                fbxScene.SetSceneInfo(fbxSceneInfo);

                FbxNode mesh = CreateFbxMesh(fbxManager, meshName, frameVertices, frameQuads);
                fbxScene.GetRootNode().AddChild(mesh);

                fbxExporter.Export(fbxScene);

                fbxScene.Destroy();
                fbxExporter.Destroy();
            }

            AssetDatabase.Refresh();
            GameObject meshGameObject = AssetDatabase.LoadAssetAtPath(fbxPath, typeof(GameObject)) as GameObject;
            return meshGameObject.GetComponent<MeshFilter>().sharedMesh;
        }

        private static FbxNode CreateFbxMesh(FbxManager fbxManager, string meshName, List<Vector3> frameVertices, List<int> frameQuads)
        {
            FbxMesh fbxMesh = FbxMesh.Create(fbxManager, meshName);
            fbxMesh.InitControlPoints(frameVertices.Count);

            var normalElement = FbxLayerElementNormal.Create(fbxMesh, "Normals");
            normalElement.SetMappingMode(FbxLayerElement.EMappingMode.eByControlPoint);
            normalElement.SetReferenceMode(FbxLayerElement.EReferenceMode.eDirect);
            var normalArray = normalElement.GetDirectArray();

            var uvElement = FbxLayerElementUV.Create(fbxMesh, "UVs");
            uvElement.SetMappingMode(FbxLayerElement.EMappingMode.eByControlPoint);
            uvElement.SetReferenceMode(FbxLayerElement.EReferenceMode.eDirect);
            var uvArray = uvElement.GetDirectArray();

            int vertexIndex = 0;
            for (int i = 0; i < frameQuads.Count; i += 5)
            {
                FbxVector4 fbxNormal = GetFaceNormal(frameVertices[frameQuads[i]], frameVertices[frameQuads[i + 1]], frameVertices[frameQuads[i + 2]]);
                int color = frameQuads[i + 4];

                for (int y = 0; y < 4; y++)
                {
                    int quadIndex = i + y;
                    Vector3 voxelPosition = frameVertices[frameQuads[quadIndex]];
                    voxelPosition *= 100.0f;

                    fbxMesh.SetControlPointAt(new FbxVector4(-voxelPosition.x, voxelPosition.y, voxelPosition.z), vertexIndex + y);
                    normalArray.Add(fbxNormal);
                    uvArray.Add(new FbxVector2(color, 0));
                }

                fbxMesh.BeginPolygon();
                fbxMesh.AddPolygon(vertexIndex);
                fbxMesh.AddPolygon(vertexIndex + 1);
                fbxMesh.AddPolygon(vertexIndex + 2);
                fbxMesh.EndPolygon();

                fbxMesh.BeginPolygon();
                fbxMesh.AddPolygon(vertexIndex + 1);
                fbxMesh.AddPolygon(vertexIndex + 3);
                fbxMesh.AddPolygon(vertexIndex + 2);
                fbxMesh.EndPolygon();

                vertexIndex += 4;
            }

            fbxMesh.GetLayer(0).SetNormals(normalElement);
            fbxMesh.GetLayer(0).SetUVs(uvElement);

            FbxNode meshNode = FbxNode.Create(fbxManager, meshName);
            meshNode.LclTranslation.Set(new FbxDouble3(0.0, 0.0, 0.0));
            meshNode.LclRotation.Set(new FbxDouble3(0.0, 0.0, 0.0));
            meshNode.LclScaling.Set(new FbxDouble3(1.0, 1.0, 1.0));
            meshNode.SetNodeAttribute(fbxMesh);

            return meshNode;
        }

        private static FbxVector4 GetFaceNormal(Vector4 point1, Vector4 point2, Vector4 point3)
        {
            Vector3 tangeant = point2 - point1;
            tangeant.x = -tangeant.x;
            Vector3 bitangeant = point3 - point1;
            bitangeant.x = -bitangeant.x;
            Vector3 normal = Vector3.Normalize(Vector3.Cross(tangeant, bitangeant));
            return new FbxVector4(normal.x, normal.y, normal.z);
        }

        #endregion BinaryFbxCreation
    }
}
