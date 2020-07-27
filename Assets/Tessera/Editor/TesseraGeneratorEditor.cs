using UnityEditor;
using UnityEngine;
using System;
using UnityEditorInternal;
using UnityEditor.IMGUI.Controls;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Collections.Generic;

namespace Tessera
{
    [CustomEditor(typeof(TesseraGenerator))]
    public class TesseraGeneratorEditor : Editor
    {
        private const string ChangeBounds = "Change Bounds";

        private class CustomHandle : BoxBoundsHandle
        {
            public TesseraGenerator generator;

            protected override Bounds OnHandleChanged(
                PrimitiveBoundsHandle.HandleDirection handle,
                Bounds boundsOnClick,
                Bounds newBounds)
            {
                // Enforce minimum size for bounds
                // And ensure it is property quantized
                switch (handle)
                {
                    case HandleDirection.NegativeX:
                    case HandleDirection.NegativeY:
                    case HandleDirection.NegativeZ:
                        newBounds.min = Vector3.Min(newBounds.min, newBounds.max - generator.tileSize);
                        newBounds.min = Round(newBounds.min - newBounds.max) + newBounds.max;
                        break;
                    case HandleDirection.PositiveX:
                    case HandleDirection.PositiveY:
                    case HandleDirection.PositiveZ:
                        newBounds.max = Vector3.Max(newBounds.max, newBounds.min + generator.tileSize);
                        newBounds.max = Round(newBounds.max - newBounds.min) + newBounds.min;
                        break;
                }
                Undo.RecordObject(generator, ChangeBounds);

                generator.bounds = newBounds;

                return newBounds;
            }

            Vector3 Round(Vector3 m)
            {
                m.x = generator.tileSize.x * ((int)Math.Round(m.x / generator.tileSize.x));
                m.y = generator.tileSize.y * ((int)Math.Round(m.y / generator.tileSize.y));
                m.z = generator.tileSize.z * ((int)Math.Round(m.z / generator.tileSize.z));
                return m;
            }
        }

        private const string GenerateTiles = "Generate tiles";

        private ReorderableList rl;

        SerializedProperty list;
        private GUIStyle headerBackground;
        int controlId;

        int selectorIndex = -1;

        const int k_fieldPadding = 2;
        const int k_elementPadding = 5;

        CustomHandle h = new CustomHandle();


        private void OnEnable()
        {
            list = serializedObject.FindProperty("tiles");

            rl = new ReorderableList(serializedObject, list, true, false, true, true);


            rl.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {

                SerializedProperty targetElement = list.GetArrayElementAtIndex(index);
                if (targetElement.hasVisibleChildren)
                    rect.xMin += 10;
                var tileProperty = targetElement.FindPropertyRelative("tile");
                var weightProperty = targetElement.FindPropertyRelative("weight");
                var tile = (TesseraTile)tileProperty.objectReferenceValue;
                var tileName = tile?.gameObject.name ?? "None";

                var tileRect = rect;
                tileRect.height = EditorGUI.GetPropertyHeight(tileProperty);
                var weightRect = rect;
                weightRect.yMin = tileRect.yMax + k_fieldPadding;
                weightRect.height = EditorGUI.GetPropertyHeight(weightProperty);
                EditorGUI.PropertyField(tileRect, tileProperty);
                EditorGUI.PropertyField(weightRect, weightProperty);
            };

            rl.elementHeightCallback = (int index) =>
            {
                SerializedProperty targetElement = list.GetArrayElementAtIndex(index);
                var tileProperty = targetElement.FindPropertyRelative("tile");
                var weightProperty = targetElement.FindPropertyRelative("weight");
                return EditorGUI.GetPropertyHeight(tileProperty) + k_fieldPadding + EditorGUI.GetPropertyHeight(weightProperty) + k_elementPadding;
            };

            rl.drawElementBackgroundCallback = (rect, index, active, focused) =>
            {
                var styleHighlight = GUI.skin.FindStyle("MeTransitionSelectHead");
                if (focused == false)
                    return;
                rect.height = rl.elementHeightCallback(index);
                GUI.Box(rect, GUIContent.none, styleHighlight);
            };

            rl.onAddCallback = l =>
            {
                ++rl.serializedProperty.arraySize;
                rl.index = rl.serializedProperty.arraySize - 1;
                list.GetArrayElementAtIndex(rl.index).FindPropertyRelative("weight").floatValue = 1.0f;
                selectorIndex = rl.index;
                controlId = EditorGUIUtility.GetControlID(FocusType.Passive);
                EditorGUIUtility.ShowObjectPicker<TesseraTile>(this, true, null, controlId);
            };

            var generator = target as TesseraGenerator;

            h.center = generator.center;
            h.size = Vector3.Scale(generator.tileSize, generator.size);
            h.generator = generator;
        }

        public override void OnInspectorGUI()
        {
            var generator = target as TesseraGenerator;

            this.headerBackground = this.headerBackground ?? (GUIStyle)"RL Header";
            serializedObject.Update();

            if (serializedObject.FindProperty("surfaceMesh").objectReferenceValue != null)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_size").FindPropertyRelative("y"), new GUIContent("Layers"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("tileSize").FindPropertyRelative("y"), new GUIContent("Tile Height"));
            }
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_center"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_size"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("tileSize"));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("backtrack"));
            if (!((TesseraGenerator)target).backtrack)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("retries"));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("skyBox"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("surfaceMesh"));
            var surfaceMesh = (Mesh)serializedObject.FindProperty("surfaceMesh").objectReferenceValue;
            if (surfaceMesh != null)
            {
                EditorGUI.indentLevel++;
                GUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("Surface Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("surfaceOffset"), new GUIContent("Offset"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("surfaceSmoothNormals"), new GUIContent("Smooth Normals"));
                if (surfaceMesh.subMeshCount > 1)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("filterSurfaceSubmeshTiles"), new GUIContent("Filter By Submesh"));

                    if (serializedObject.FindProperty("filterSurfaceSubmeshTiles").boolValue)
                    {
                        var surfaceSubmeshTiles = serializedObject.FindProperty("surfaceSubmeshTiles");
                        var currentSize = surfaceSubmeshTiles.arraySize;
                        if (currentSize != surfaceMesh.subMeshCount)
                        {
                            // Update array
                            surfaceSubmeshTiles.arraySize = surfaceMesh.subMeshCount;
                            var tiles = generator.tiles.Select(x => x.tile).ToList();
                            for (var i = currentSize; i < surfaceMesh.subMeshCount; i++)
                            {
                                // Initialize with a full set of tiles.
                                var tilesProperty = surfaceSubmeshTiles.GetArrayElementAtIndex(i).FindPropertyRelative("tiles");
                                tilesProperty.arraySize = tiles.Count;
                                for (var j = 0; j < tiles.Count; j++)
                                {
                                    tilesProperty.GetArrayElementAtIndex(j).objectReferenceValue = tiles[j];
                                }
                            }
                        }
                        for (var i = 0; i < surfaceMesh.subMeshCount; i++)
                        {
                            var tilesProperty = surfaceSubmeshTiles.GetArrayElementAtIndex(i).FindPropertyRelative("tiles");
                            new TileListUtil(generator, $"Submesh {i} filter", tilesProperty, allowSingleTile: false).Draw();
                        }
                    }
                }
                GUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }

            TileListGUI();
            serializedObject.ApplyModifiedProperties();

            Validate();

            EditorUtility.ClearProgressBar();

            var clearable = !GetTileOutput(generator).IsEmpty;

            GUI.enabled = clearable;

            if (GUILayout.Button("Clear"))
            {
                Clear();
            }

            GUI.enabled = true;

            if (GUILayout.Button(clearable ? "Regenerate" : "Generate"))
            {
                // Undo the last generation
                Undo.SetCurrentGroupName(GenerateTiles);
                if (clearable)
                {
                    Clear();
                }

                Generate(generator);
            }
        }

        private static Texture2D s_WarningIcon;


        internal static Texture2D warningIcon
        {
            get
            {
                return s_WarningIcon ?? (s_WarningIcon = (Texture2D)EditorGUIUtility.IconContent("console.warnicon").image);
            }
        }

        private bool HelpBoxWithButton(string message, MessageType type)
        {
            return HelpBoxWithButton(message, "Fix it!", type);
        }

        private bool HelpBoxWithButton(string message, string buttonMessage, MessageType type)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(GUIContent.none, new GUIContent(message, warningIcon));
            var r = GUILayout.Button(buttonMessage);
            EditorGUILayout.EndVertical();
            return r;
        }

        private void SetReadable(GameObject go, Mesh mesh)
        {
            if (mesh == null) return;
            if (mesh.isReadable) return;
            var path = AssetDatabase.GetAssetPath(mesh);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning($"Unable to find asset for a mesh on {go}");
                return;
            }
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                Debug.LogWarning($"Unable to find model importer for asset {path}");
                return;
            }
            Debug.Log($"Updating import settings for asset {path}");
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        private void SetTangents(GameObject go, Mesh mesh)
        {
            if (mesh == null) return;
            var path = AssetDatabase.GetAssetPath(mesh);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning($"Unable to find asset for a mesh on {go}");
                return;
            }
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                Debug.LogWarning($"Unable to find model importer for asset {path}");
                return;
            }
            Debug.Log($"Updating import settings for asset {path}");
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.SaveAndReimport();
        }

        // Should mirror TesseraGenerator.Validate
        private void Validate()
        {
            var generator = target as TesseraGenerator;

            var allTiles = generator.tiles.Select(x => x.tile).Where(x => x != null);
            if (generator.surfaceMesh != null)
            {
                if (generator.surfaceMesh.GetTopology(0) != MeshTopology.Quads)
                {
                    if(HelpBoxWithButton($"Mesh topology {generator.surfaceMesh.GetTopology(0)} not supported. You need to select \"Keep Quads\" in the import options.", MessageType.Warning))
                    {
                        var path = AssetDatabase.GetAssetPath(generator.surfaceMesh);
                        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                        importer.keepQuads = true;
                        importer.SaveAndReimport();
                    }
                }
                if (!generator.surfaceMesh.isReadable)
                {
                    if (HelpBoxWithButton($"Surface mesh needs to be readable.", MessageType.Warning))
                    {
                        SetReadable(generator.gameObject, generator.surfaceMesh);
                    }
                }
                //if (!generator.surfaceSmoothNormals && generator.surfaceMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Tangent))
                if (generator.surfaceSmoothNormals && generator.surfaceMesh.tangents.Length == 0)
                {
                    if (HelpBoxWithButton($"Surface mesh needs tangents to calculate smoothed normals. You need to select \"Calculate\" the tangent field of the import options.", MessageType.Warning))
                    {
                        SetTangents(generator.gameObject, generator.surfaceMesh);
                    }
                }
                var unreadable = allTiles.Where(tile => tile.GetComponentsInChildren<MeshFilter>().Any(mf => !mf.sharedMesh.isReadable)).ToList();
                if (unreadable.Count > 0)
                {
                    if(HelpBoxWithButton($"Some tiles have meshes that are not readable. They will not be transformed to fit the mesh. E.g {unreadable.First().name}", MessageType.Warning))
                    {
                        foreach(var tile in allTiles)
                        {
                            foreach(var mf in tile.GetComponentsInChildren<MeshFilter>())
                            {
                                SetReadable(mf.gameObject, mf.sharedMesh);
                            }
                            foreach (var mc in tile.GetComponentsInChildren<MeshCollider>())
                            {
                                SetReadable(mc.gameObject, mc.sharedMesh);
                            }
                        }
                    }
                }
                if (generator.filterSurfaceSubmeshTiles)
                {
                    for (var i = 0; i < generator.surfaceSubmeshTiles.Count; i++)
                    {
                        if (generator.surfaceSubmeshTiles[i].tiles.Count == 0)
                        {
                            EditorGUILayout.HelpBox($"Submesh {i} is filtered to zero tiles. Generation is impossible", MessageType.Warning);
                        }
                    }
                }

                return;
            }
            var missizedTiles = generator.GetMissizedTiles().ToList();
            if (missizedTiles.Count > 0)
            {
                EditorGUILayout.HelpBox($"Some tiles do not have the same tileSize as the generator, {generator.tileSize}, this can cause unexpected behaviour.\n" +
                    "NB: Big tiles should still share the same value of tileSize\n" +
                    "Affected tiles:\n" +
                    string.Join("\n", missizedTiles),
                    MessageType.Warning);
            }
        }

        private void Clear()
        {
            var generator = target as TesseraGenerator;
            var tileOutput = GetTileOutput(generator);
            tileOutput.ClearTiles();
        }

        private ITesseraTileOutput GetTileOutput(TesseraGenerator generator)
        {
            var tileOutput = generator.GetComponent<ITesseraTileOutput>();

            if (tileOutput is TesseraTilemapOutput tmo)
            {
                return new RegisterUndo(tmo?.tilemap?.gameObject, tmo);
            }
            if (tileOutput is TesseraMeshOutput tmo2)
            {
                // TODO: Manually handle mesh serialization for undo
                // https://answers.unity.com/questions/607527/is-this-possible-to-apply-undo-to-meshfiltermesh.html
                return tmo2;
            }
            if(tileOutput == null)
            {
                if(generator.surfaceMesh != null)
                {
                    // Disable undo/redo in this case, it is very slow
                    return new InstantiateOutput(generator.transform);
                }
                return new EditorTileOutout(generator.transform);
            }
            else
            {
                // Something custom, just use it verbatim
                return tileOutput;
            }
        }

        private class RegisterUndo : ITesseraTileOutput
        {
            private readonly UnityEngine.Object go;
            private readonly ITesseraTileOutput underlying;

            public RegisterUndo(UnityEngine.Object go, ITesseraTileOutput underlying)
            {
                this.go = go;
                this.underlying = underlying;
            }

            public bool IsEmpty => underlying.IsEmpty;

            public bool SupportsIncremental => false;

            public void ClearTiles()
            {
                if (go != null)
                {
                    Undo.RegisterCompleteObjectUndo(go, GenerateTiles);
                }
                underlying.ClearTiles();
            }

            public void UpdateTiles(IEnumerable<TesseraTileInstance> tileInstances)
            {
                if (go != null)
                {
                    Undo.RegisterCompleteObjectUndo(go, GenerateTiles);
                }
                underlying.UpdateTiles(tileInstances);
            }
        }

        private class EditorTileOutout : ITesseraTileOutput
        {
            private readonly Transform transform;

            public EditorTileOutout(Transform transform)
            {
                this.transform = transform;
            }

            public bool IsEmpty => transform.childCount == 0;

            public bool SupportsIncremental => true;

            public void ClearTiles()
            {
                var children = transform.Cast<Transform>().ToList();
                foreach (var child in children)
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }

            public void UpdateTiles(IEnumerable<TesseraTileInstance> tileInstances)
            {

                foreach (var i in tileInstances)
                {
                    foreach (var go in TesseraGenerator.Instantiate(i, transform))
                    {
                        Undo.RegisterCreatedObjectUndo(go, GenerateTiles);
                    }
                }
            }
        }

        // Wraps generation with a progress bar and cancellation button.
        private void Generate(TesseraGenerator generator)
        {
            var cts = new CancellationTokenSource();
            string progressText = "";
            float progress = 0.0f;

            var tileOutput = GetTileOutput(generator);

            void OnComplete(TesseraCompletion completion)
            {
                if (!completion.success)
                {
                    if (completion.contradictionLocation != null)
                    {
                        var loc = completion.contradictionLocation;
                        Debug.LogError($"Failed to complete generation, issue at tile {loc}");
                    }
                    else
                    {
                        Debug.LogError("Failed to complete generation");
                    }
                    return;
                }

                if(tileOutput != null)
                {
                    tileOutput.UpdateTiles(completion.tileInstances);
                }
                else
                {
                }
            }

            var enumerator = generator.StartGenerate(new TesseraGenerateOptions
            {
                onComplete = OnComplete,
                progress = (t, p) => { progressText = t; progress = p; },
                cancellationToken = cts.Token
            });

            var last = DateTime.Now;
            // Update progress this frequently.
            // Too fast and it'll slow down generation.
            var freq = TimeSpan.FromSeconds(0.1);
            try
            {
                while (enumerator.MoveNext())
                {
                    var a = enumerator.Current;
                    if (last + freq < DateTime.Now)
                    {
                        last = DateTime.Now;
                        if (EditorUtility.DisplayCancelableProgressBar("Generating", progressText, progress))
                        {
                            cts.Cancel();
                            EditorUtility.ClearProgressBar();
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Ignore
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
            EditorUtility.ClearProgressBar();
            GUIUtility.ExitGUI();
        }
        [DrawGizmo(GizmoType.Selected)]
        static void DrawGizmo(TesseraGenerator generator, GizmoType gizmoType)
        {
            if (generator.surfaceMesh != null)
            {
                var tf = generator.transform;
                if(generator.GetComponent<TesseraMeshOutput>() is var mo && mo?.targetMeshFilter != null)
                {
                    tf = mo.targetMeshFilter.transform;
                }
                var m = tf.localToWorldMatrix;
                var verticies = generator.surfaceMesh.vertices;
                var normals = generator.surfaceMesh.normals;
                var tileHeight = generator.tileSize.y;
                var meshOffset = generator.surfaceOffset;
                var layerCount = generator.size.y;
                for (var submesh = 0; submesh < generator.surfaceMesh.subMeshCount; submesh++)
                {
                    var indices = generator.surfaceMesh.GetIndices(submesh);
                    for (var i = 0; i < indices.Length; i += 4)
                    {
                        var v1 = verticies[indices[i + 0]];
                        var v2 = verticies[indices[i + 1]];
                        var v3 = verticies[indices[i + 2]];
                        var v4 = verticies[indices[i + 3]];
                        var n1 = normals[indices[i + 0]];
                        var n2 = normals[indices[i + 1]];
                        var n3 = normals[indices[i + 2]];
                        var n4 = normals[indices[i + 3]];
                        v1 = v1 + (meshOffset - 0.5f * tileHeight) * n1;
                        v2 = v2 + (meshOffset - 0.5f * tileHeight) * n2;
                        v3 = v3 + (meshOffset - 0.5f * tileHeight) * n3;
                        v4 = v4 + (meshOffset - 0.5f * tileHeight) * n4;
                        v1 = m.MultiplyPoint3x4(v1);
                        v2 = m.MultiplyPoint3x4(v2);
                        v3 = m.MultiplyPoint3x4(v3);
                        v4 = m.MultiplyPoint3x4(v4);
                        Gizmos.DrawLine(v1, v2);
                        Gizmos.DrawLine(v2, v3);
                        Gizmos.DrawLine(v3, v4);
                        Gizmos.DrawLine(v4, v1);
                    }
                }
            }
        }

        protected virtual void OnSceneGUI()
        {
            var generator = target as TesseraGenerator;

            if (Event.current.type == EventType.MouseDown)
            {
                mouseDown = true;
            }
            if (Event.current.type == EventType.MouseUp)
            {
                mouseDown = false;
            }

            if (generator.surfaceMesh == null)
            {
                EditorGUI.BeginChangeCheck();
                Handles.matrix = generator.gameObject.transform.localToWorldMatrix;
                h.DrawHandle();
                Handles.matrix = Matrix4x4.identity;
                if (EditorGUI.EndChangeCheck())
                {
                }

                if (!mouseDown)
                {
                    h.center = generator.center;
                    h.size = Vector3.Scale(generator.tileSize, generator.size);
                }
            }
        }

        private static GUILayoutOption miniButtonWidth = GUILayout.Width(20f);
        private bool mouseDown;

        private void TileListGUI()
        {
            if (Event.current.commandName == "ObjectSelectorUpdated" && EditorGUIUtility.GetObjectPickerControlID() == controlId)
            {
                if (selectorIndex >= 0)
                {
                    var tileObject = (GameObject)EditorGUIUtility.GetObjectPickerObject();
                    var tile = tileObject.GetComponent<TesseraTile>();
                    list.GetArrayElementAtIndex(selectorIndex).FindPropertyRelative("tile").objectReferenceValue = tile;
                }
            }
            if (Event.current.commandName == "ObjectSelectorClosed" && EditorGUIUtility.GetObjectPickerControlID() == controlId)
            {
                selectorIndex = -1;
            }

            list.isExpanded = EditorGUILayout.Foldout(list.isExpanded, new GUIContent("Tiles"));

            if (list.isExpanded)
            {
                var r1 = GUILayoutUtility.GetLastRect();

                rl.DoLayoutList();

                var r2 = GUILayoutUtility.GetLastRect();

                var r = new Rect(r1.xMin, r1.yMax, r1.width, r2.yMax - r1.yMax);

                if (r.Contains(Event.current.mousePosition))
                {
                    if (Event.current.type == EventType.DragUpdated)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        Event.current.Use();
                    }
                    else if (Event.current.type == EventType.DragPerform)
                    {
                        for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                        {
                            var t = (DragAndDrop.objectReferences[i] as TesseraTile) ?? (DragAndDrop.objectReferences[i] as GameObject)?.GetComponent<TesseraTile>();
                            if (t != null)
                            {
                                ++rl.serializedProperty.arraySize;
                                rl.index = rl.serializedProperty.arraySize - 1;
                                list.GetArrayElementAtIndex(rl.index).FindPropertyRelative("weight").floatValue = 1.0f;
                                list.GetArrayElementAtIndex(rl.index).FindPropertyRelative("tile").objectReferenceValue = t;
                            }
                        }
                        Event.current.Use();
                    }
                }
            }



            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Delete && rl.index >= 0)
            {
                list.DeleteArrayElementAtIndex(rl.index);
                if (rl.index >= list.arraySize - 1)
                {
                    rl.index = list.arraySize - 1;
                }
            }
        }
    }
}