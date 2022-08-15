using System;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Serialization;
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


    [ValueDropdown("allFilePathList"), InlineButton("DrawMap")]
    public string curMapFile;

    private void DrawMap()
    {
        LoadMapData();
    }


    #region 加载构建必要资源

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

    private GenericMenu enemyMenu;

    protected override void Initialize()
    {
        base.Initialize();

        enemyMenu = new GenericMenu();
        for (int i = 2; i <= 6; i++)
        {
            enemyMenu.AddItem(new GUIContent(i.ToString()), false, SelectCallback, i);
            enemyMenu.AddSeparator("");
        }

        LoadIcon();
    }

    #endregion

    private bool mapLoadCompleted;

    private TileData[,] mapDataArray;

    private void LoadMapData()
    {
        // TODO: 根据选中的地图文件生成地图数据

        mapDataArray = new TileData[10, 10];
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                mapDataArray[i, j] = new TileData((TileType)Random.Range(0, 7));
            }
        }

        mapLoadCompleted = true;
    }

    private Vector2Int curOperateCell = Vector2Int.zero;

    private Vector2Int preEnterCell = Vector2Int.down;

    private void SelectCallback(object data)
    {
        mapDataArray[curOperateCell.x, curOperateCell.y].tileType = (TileType)(int)data;
    }

    private string modifyText = "-1";
    private bool isRightClick;

    private bool middleBtnTrigger;
    private Vector2 changeRect = Vector2.zero;

    protected override void DrawEditor(int index)
    {
        base.DrawEditor(index);
        if (!mapLoadCompleted)
        {
            return;
        }

        Rect rect = EditorGUILayout.GetControlRect(true, 400 + 20);
        rect = rect.AlignCenter(400);
        rect.position += changeRect;
        if (Event.current.type == EventType.MouseDown && Event.current.button == 2)
        {
            middleBtnTrigger = true;
        }

        if (Event.current.type == EventType.MouseUp && Event.current.button == 2)
        {
            middleBtnTrigger = false;
        }

        if (middleBtnTrigger && Event.current.type == EventType.MouseDrag)
        {
            Vector2 delta = Event.current.delta;
            changeRect += delta;
        }

        SirenixEditorGUI.DrawSolidRect(rect.AlignTop(20), new Color(0.5f, 0.5f, 0.5f, 1f));
        SirenixEditorGUI.DrawBorders(rect.AlignTop(20).SetHeight(21).SetWidth(rect.width + 1), 1);

        GUIHelper.PushContentColor(Color.black);
        GUI.Label(rect.AlignTop(20).AlignCenter(0).SetWidth(400), "地图名称");
        GUIHelper.PopContentColor();

        rect = rect.AlignBottom(rect.height - 20);
        SirenixEditorGUI.DrawSolidRect(rect, new Color(0.7f, 0.7f, 0.7f, 1f));

        for (int i = 0; i < 100; i++)
        {
            Rect tileRect = rect.SplitGrid(40, 40, i);
            SirenixEditorGUI.DrawBorders(tileRect.SetWidth(tileRect.width + 1).SetHeight(tileRect.height + 1), 1);

            int x = i % 10;
            int y = i / 10;
            if (mapDataArray[x, y].tileType == TileType.Obstacle)
            {
                SirenixEditorGUI.DrawSolidRect(
                    new Rect(tileRect.x + 1, tileRect.y + 1, tileRect.width - 1, tileRect.height - 1),
                    new Color(0.3f, 0.3f, 0.3f, 1f));
            }

            if (mapDataArray[x, y].tileType >= TileType.Enemy1)
            {
                if (mapDataArray[x, y].id > -1)
                {
                    GUIHelper.PushColor(Color.black);
                    GUI.Label(tileRect.AlignLeft(40).AlignTop(18), mapDataArray[x, y].id.ToString());
                    GUIHelper.PopColor();
                }

                GUI.Label(tileRect.AlignCenter(18).AlignMiddle(18),
                    enemyIconList[(int)mapDataArray[x, y].tileType - 2]);
            }

            if (mapDataArray[x, y].tileType != TileType.Obstacle && tileRect.Contains(Event.current.mousePosition))
            {
                if (preEnterCell.x != x || preEnterCell.y != y)
                {
                    preEnterCell = new Vector2Int(x, y);
                    isRightClick = false;
                }

                SirenixEditorGUI.DrawSolidRect(
                    new Rect(tileRect.x + 1, tileRect.y + 1, tileRect.width - 1, tileRect.height - 1),
                    new Color(0f, 1f, 0f, 0.3f));

                if (!isRightClick && Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    if (mapDataArray[x, y].tileType >= TileType.Enemy1)
                    {
                        mapDataArray[x, y].tileType = TileType.Empty;
                    }
                    else
                    {
                        DrawEnemyMenu();
                        curOperateCell = new Vector2Int(x, y);
                    }

                    Event.current.Use();
                }
            }

            if (isRightClick && curOperateCell.x == x && curOperateCell.y == y)
            {
                GUI.SetNextControlName("IdInputText");
                modifyText = GUI.TextArea(tileRect.AlignCenter(40).AlignMiddle(40), modifyText);
                GUI.FocusControl("IdInputText");
                if (GUI.changed)
                {
                    for (int j = 0; j < modifyText.Length; j++)
                    {
                        if (modifyText[j] == 10)
                        {
                            isRightClick = false;
                            break;
                        }
                    }

                    modifyText = modifyText.Trim();
                    if (int.TryParse(modifyText, out int idValue))
                    {
                        mapDataArray[curOperateCell.x, curOperateCell.y].id = idValue;
                    }
                }
            }

            if (mapDataArray[x, y].tileType >= TileType.Enemy1 && tileRect.Contains(Event.current.mousePosition) &&
                Event.current.type == EventType.MouseDown && Event.current.button == 1)
            {
                // 右键修改存在怪物的id
                isRightClick = true;
                curOperateCell = new Vector2Int(x, y);
                Event.current.Use();
            }
        }
    }

    private void DrawEnemyMenu()
    {
        enemyMenu.ShowAsContext();
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