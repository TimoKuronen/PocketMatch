using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(MapData))]
public class MapEditor : Editor
{
    private TilePower selectedPower = TilePower.None;
    private bool toggleDestroyable = false;
    private bool toggleBlocked = false;

    // Foldout states
    private bool showGridSection = true;
    private bool showVictorySection = true;

    public override void OnInspectorGUI()
    {
        MapData map = (MapData)target;
        serializedObject.Update();

        // ---------------- GRID SECTION ----------------
        showGridSection = EditorGUILayout.Foldout(showGridSection, "Grid Editor", true);
        if (showGridSection)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("Grid Size", EditorStyles.boldLabel);
            map.width = EditorGUILayout.IntField("Width", map.width);
            map.height = EditorGUILayout.IntField("Height", map.height);

            if (GUILayout.Button("Initialize Grid"))
            {
                map.tiles = new TileDataEditorView[map.width * map.height];
                for (int i = 0; i < map.tiles.Length; i++)
                    map.tiles[i] = new TileDataEditorView();
            }

            EditorGUILayout.Space();

            selectedPower = (TilePower)EditorGUILayout.EnumPopup("Power Type", selectedPower);
            toggleDestroyable = EditorGUILayout.Toggle("Destroyable", toggleDestroyable);
            toggleBlocked = EditorGUILayout.Toggle("Blocked (Wall)", toggleBlocked);

            EditorGUILayout.Space();

            if (map.tiles == null || map.tiles.Length != map.width * map.height)
            {
                EditorGUILayout.HelpBox("Initialize grid to match size.", MessageType.Warning);
            }
            else
            {
                // Draw grid visualization
                for (int visualY = 0; visualY < map.height; visualY++)
                {
                    int y = map.height - 1 - visualY; // Invert Y for correct display
                    EditorGUILayout.BeginHorizontal();

                    for (int x = 0; x < map.width; x++)
                    {
                        var tile = map.GetTile(x, y);
                        GUI.backgroundColor = tile.isBlocked ? Color.gray : Color.green;

                        Rect rect = GUILayoutUtility.GetRect(40, 40);
                        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                        {
                            if (Event.current.button == 1) // Right click: reset
                            {
                                tile.isBlocked = false;
                                tile.tilePower = TilePower.None;
                                tile.isDestroyable = false;
                                tile.hitPoints = 1;

                                map.SetTile(x, y, tile);
                                EditorUtility.SetDirty(map);
                                Event.current.Use();
                            }
                            else if (Event.current.button == 0) // Left click: apply selection
                            {
                                tile.tilePower = selectedPower;
                                tile.isDestroyable = toggleDestroyable;
                                tile.isBlocked = toggleBlocked;

                                if (toggleDestroyable)
                                    tile.hitPoints = 3; // default HP

                                map.SetTile(x, y, tile);
                                EditorUtility.SetDirty(map);
                                Event.current.Use();
                            }
                        }

                        GUI.Box(rect, TileLabel(tile));
                    }

                    EditorGUILayout.EndHorizontal();
                }

                GUI.backgroundColor = Color.white;
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // ---------------- VICTORY SECTION ----------------
        showVictorySection = EditorGUILayout.Foldout(showVictorySection, "Victory Conditions", true);
        if (showVictorySection)
        {
            EditorGUI.indentLevel++;

            SerializedProperty victoryConditions = serializedObject.FindProperty("VictoryConditions");

            EditorGUILayout.PropertyField(victoryConditions.FindPropertyRelative("MoveLimit"));
            EditorGUILayout.PropertyField(victoryConditions.FindPropertyRelative("DestroyableTileCount"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("AllowedTileColors"), true);
            EditorGUILayout.PropertyField(victoryConditions.FindPropertyRelative("RequiredColorMatchCount"), true);

            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private string TileLabel(TileDataEditorView tile)
    {
        if (tile.isBlocked)
            return "X";

        string label = "N";

        if (tile.isDestroyable)
            label += "D";

        if (tile.tilePower != TilePower.None)
            label += tile.tilePower.ToString().Substring(0, 1);

        return label;
    }
}
#endif