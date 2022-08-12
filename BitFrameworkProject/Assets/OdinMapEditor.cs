using System;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class OdinMapEditor : OdinEditorWindow
{
    [MenuItem("Odin/MapEditor")]
    private static void OpenWindow()
    {
        GetWindow<OdinMapEditor>().Show();
    }

    private List<string> allFilePathList = new List<string>()
    {
        "1",
        "2",
        "3"
    };

    private List<string> enemyList = new List<string>()
    {
        "Enemy1",
        "Enemy2",
        "Enemy3",
        "Enemy4"
    };


    [ValueDropdown("allFilePathList"), InlineButton("DrawMap")]
    public string curMapFile;

    private void DrawMap()
    {
        loadMapData();
    }

    private TileType[,] visibleArray = new TileType[10, 10];

    private GenericMenu menu;

    protected override void Initialize()
    {
        base.Initialize();

        menu = new GenericMenu();
        for (int i = 2; i <= 6; i++)
        {
            menu.AddItem(new GUIContent(i.ToString()), false, SelectCallback, i);
            menu.AddSeparator("");
        }

        LoadIcon();
    }

    private bool mapLoadCompleted;

    private void loadMapData()
    {
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                visibleArray[i, j] = (TileType)Random.Range(0, 7);
            }
        }

        mapLoadCompleted = true;
    }

    private Vector2Int curCell = Vector2Int.zero;

    private void SelectCallback(object data)
    {
        visibleArray[curCell.x, curCell.y] = (TileType)(int)data;
    }

    private bool drawEnemyList;

    protected override void DrawEditor(int index)
    {
        base.DrawEditor(index);
        if (!mapLoadCompleted)
        {
            return;
        }

        Rect rect = EditorGUILayout.GetControlRect(true, 400 + 20);
        rect = rect.AlignCenter(400);

        SirenixEditorGUI.DrawSolidRect(rect.AlignTop(20), new Color(0.5f, 0.5f, 0.5f, 1f));
        SirenixEditorGUI.DrawBorders(rect.AlignTop(20).SetHeight(21).SetWidth(rect.width + 1), 1);

        GUIHelper.PushContentColor(Color.black);
        GUI.Label(rect.AlignTop(20), "A");
        GUIHelper.PopContentColor();

        rect = rect.AlignBottom(rect.height - 20);
        SirenixEditorGUI.DrawSolidRect(rect, new Color(0.7f, 0.7f, 0.7f, 1f));

        for (int i = 0; i < 100; i++)
        {
            Rect tileRect = rect.SplitGrid(40, 40, i);
            SirenixEditorGUI.DrawBorders(tileRect.SetWidth(tileRect.width + 1).SetHeight(tileRect.height + 1), 1);

            int x = i % 10;
            int y = i / 10;
            if (visibleArray[x, y] == TileType.Obstacle)
            {
                SirenixEditorGUI.DrawSolidRect(
                    new Rect(tileRect.x + 1, tileRect.y + 1, tileRect.width - 1, tileRect.height - 1),
                    new Color(0.3f, 0.3f, 0.3f, 1f));
            }

            if (visibleArray[x, y] == TileType.Enemy1)
            {
                GUI.Label(tileRect.AlignCenter(18).AlignMiddle(18), enemyIconList[(int)TileType.Enemy1 - 2]);
            }

            if (visibleArray[x, y] == TileType.Enemy2)
            {
                GUI.Label(tileRect.AlignCenter(18).AlignMiddle(18), enemyIconList[(int)TileType.Enemy2 - 2]);
            }

            if (visibleArray[x, y] == TileType.Enemy3)
            {
                GUI.Label(tileRect.AlignCenter(18).AlignMiddle(18), enemyIconList[(int)TileType.Enemy3 - 2]);
            }

            if (visibleArray[x, y] == TileType.Enemy4)
            {
                GUI.Label(tileRect.AlignCenter(18).AlignMiddle(18), enemyIconList[(int)TileType.Enemy4 - 2]);
            }

            if (visibleArray[x, y] == TileType.Enemy5)
            {
                GUI.Label(tileRect.AlignCenter(18).AlignMiddle(18), enemyIconList[(int)TileType.Enemy5 - 2]);
                GUIHelper.PushColor(Color.black);
                GUI.Label(tileRect.AlignRight(18).AlignTop(18), (x + y).ToString());
                GUIHelper.PopColor();
            }

            if (tileRect.Contains(Event.current.mousePosition) && visibleArray[x, y] != TileType.Obstacle)
            {
                SirenixEditorGUI.DrawSolidRect(
                    new Rect(tileRect.x + 1, tileRect.y + 1, tileRect.width - 1, tileRect.height - 1),
                    new Color(0f, 1f, 0f, 0.3f));

                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    if (visibleArray[x, y] >= TileType.Enemy1)
                    {
                        visibleArray[x, y] = TileType.Empty;
                    }
                    else
                    {
                        DrawEnemyList();
                        curCell = new Vector2Int(x, y);
                    }

                    Event.current.Use();
                }
            }
        }
    }

    private void DrawEnemyList()
    {
        menu.ShowAsContext();
    }

    private List<Texture2D> enemyIconList = new List<Texture2D>();

    private readonly string iconPath = "Assets/PetsPack";

    private void LoadIcon()
    {
        enemyIconList.Clear();
        string[] files = Directory.GetFiles(iconPath, "*.png");
        foreach (string file in files)
        {
            Texture2D texture2D = AssetDatabase.LoadAssetAtPath<Texture2D>(file);
            enemyIconList.Add(texture2D);
        }

        enemyIconList.Sort((a, b) => String.Compare(a.name, b.name, StringComparison.Ordinal));
    }
}

public enum TileType
{
    Empty,
    Obstacle,
    Enemy1,
    Enemy2,
    Enemy3,
    Enemy4,
    Enemy5,
}

public class TileData
{
    public TileType tileType;
    public int id;

    public TileData(TileType type, int id = -1)
    {
        tileType = type;
        this.id = id;
    }
}