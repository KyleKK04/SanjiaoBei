using UnityEngine;
using UnityEditor;
using Game.Data; // 引用我们之前写的数据命名空间
using System.Collections.Generic;

namespace Game.EditorTools
{
    public class LevelEditor : EditorWindow
    {
        // 当前正在编辑的关卡数据 SO
        private LevelSO currentLevelData;

        // 编辑器内部使用的临时二维数组 (比 List 更容易进行网格操作)
        private LevelElement[,] tempMap;

        // 当前选中的笔刷类型
        private GridObjectType selectedType = GridObjectType.Ground;

        // 地图尺寸设置
        private int mapWidth = 10;
        private int mapHeight = 10;

        // GUI 滚动位置
        private Vector2 scrollPosition;

        [MenuItem("Game/Level Editor")]
        public static void ShowWindow()
        {
            GetWindow<LevelEditor>("Level Editor");
        }

        private void OnGUI()
        {
            GUILayout.Label("关卡编辑器 (Level Editor)", EditorStyles.boldLabel);

            // 1. 顶部工具栏：加载/保存/设置
            DrawTopToolbar();

            // 如果没有初始化地图，就不显示下面的内容
            if (tempMap == null) return;

            EditorGUILayout.Space();

            // 2. 笔刷选择区
            DrawPalette();

            EditorGUILayout.Space();

            // 3. 网格绘制区 (模拟游戏画面)
            DrawGrid();
        }

        // --- 1. 顶部工具栏 ---
        private void DrawTopToolbar()
        {
            EditorGUILayout.BeginVertical("box");

            // 选择 ScriptableObject
            currentLevelData =
                (LevelSO)EditorGUILayout.ObjectField("Level Data SO", currentLevelData, typeof(LevelSO), false);

            EditorGUILayout.BeginHorizontal();

            // 宽高设置
            mapWidth = EditorGUILayout.IntField("Width", mapWidth);
            mapHeight = EditorGUILayout.IntField("Height", mapHeight);

            if (GUILayout.Button("重置/新建地图"))
            {
                InitializeNewMap();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("读取数据 (Load)"))
            {
                LoadLevel();
            }

            // 保存按钮变个颜色提醒
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("保存数据 (Save)"))
            {
                SaveLevel();
            }

            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        // --- 2. 笔刷选择 ---
        private void DrawPalette()
        {
            EditorGUILayout.LabelField("笔刷选择 (Brush Selection):", EditorStyles.boldLabel);
            // 使用枚举弹出菜单选择当前要画什么
            selectedType = (GridObjectType)EditorGUILayout.EnumPopup("Object Type", selectedType);

            EditorGUILayout.HelpBox("操作说明:\n左键点击格子: 放置物体\n右键点击格子: 顺时针旋转朝向", MessageType.Info);
        }

        // --- 3. 网格绘制 (核心逻辑) ---
        private void DrawGrid()
        {
            if (tempMap == null) return;

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // 使用垂直布局，居中
            EditorGUILayout.BeginVertical();

            // 注意：Unity GUI 的坐标系 Y 是向下的，但游戏逻辑通常 Y 是向上的。
            // 为了让编辑器看起来和游戏里一样（左下角是 0,0），我们需要倒序遍历 Y 轴。
            for (int y = mapHeight - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace(); // 水平居中

                for (int x = 0; x < mapWidth; x++)
                {
                    DrawCell(x, y);
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void DrawCell(int x, int y)
        {
            LevelElement element = tempMap[x, y];

            // 1. 先保留颜色设置
            Color defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = GetColorByType(element.type);
            string label = GetLabelText(element);

            // 2. 关键点：不使用 GUILayout.Button 的返回值，而是先申请一块 40x40 的区域
            Rect cellRect = GUILayoutUtility.GetRect(40, 40);

            // 3. 在这个区域画一个按钮样式的盒子（仅用于显示，不负责逻辑）
            GUI.Box(cellRect, label, GUI.skin.button);

            // 4. 手动检测事件
            // 只有当鼠标点击 (MouseDown) 且鼠标位置在刚才申请的方块内 (Contains) 时才触发
            if (Event.current.type == EventType.MouseDown && cellRect.Contains(Event.current.mousePosition))
            {
                // 左键点击 (0) -> 放置物体
                if (Event.current.button == 0)
                {
                    element.type = selectedType;

                    // 如果不是那几种需要方向的物体，放置时重置为 down
                    if (selectedType != GridObjectType.Statue &&
                        selectedType != GridObjectType.GhostStatue &&
                        selectedType != GridObjectType.SpawnPoint)
                    {
                        element.initialFacing = Direction.down;
                    }

                    // 标记事件已处理，防止穿透
                    Event.current.Use();
                }
                // 右键点击 (1) -> 旋转
                else if (Event.current.button == 1)
                {
                    RotateElement(element);

                    // 标记事件已处理
                    Event.current.Use();
                }

                // 强制刷新编辑器界面，让变化立刻显示出来
                Repaint();
            }

            // 恢复颜色
            GUI.backgroundColor = defaultColor;
        }

        // --- 逻辑处理方法 ---

        private void InitializeNewMap()
        {
            tempMap = new LevelElement[mapWidth, mapHeight];
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    tempMap[x, y] = new LevelElement();
                    tempMap[x, y].position = new GridCoordinates(x, y);
                    tempMap[x, y].type = GridObjectType.Ground; // 默认为地面
                }
            }
        }

        private void LoadLevel()
        {
            if (currentLevelData == null)
            {
                Debug.LogError("请先将 LevelSO 拖入槽位！");
                return;
            }

            // 从 SO 读取尺寸
            mapWidth = currentLevelData.mapSize.x;
            mapHeight = currentLevelData.mapSize.y;

            // 初始化数组
            InitializeNewMap();

            // 填充数据
            foreach (var savedElement in currentLevelData.elements)
            {
                int x = savedElement.position.x;
                int y = savedElement.position.y;

                if (x >= 0 && x < mapWidth && y >= 0 && y < mapHeight)
                {
                    tempMap[x, y].type = savedElement.type;
                    tempMap[x, y].initialFacing = savedElement.initialFacing;
                }
            }

            Debug.Log($"关卡 {currentLevelData.name} 加载成功！");
        }

        private void SaveLevel()
        {
            if (currentLevelData == null)
            {
                Debug.LogError("没有指定要保存的 LevelSO！");
                return;
            }

            // 1. 更新 SO 的基础设置
            currentLevelData.mapSize = new GridCoordinates(mapWidth, mapHeight);
            currentLevelData.elements.Clear();

            // 2. 将数组转换回 List
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    LevelElement el = tempMap[x, y];

                    // 优化：如果是 None (虚空)，可以选择不保存进列表，节省空间
                    // 这里为了逻辑简单，我们全部保存，或者只保存非 Ground 的物体
                    // 为了演示完整性，我们将所有数据都存入

                    // 需要深拷贝一个新的对象存入 List，防止引用问题
                    LevelElement toSave = new LevelElement();
                    toSave.position = new GridCoordinates(x, y);
                    toSave.type = el.type;
                    toSave.initialFacing = el.initialFacing;

                    currentLevelData.elements.Add(toSave);
                }
            }

            // 3. 标记为已修改 (Dirty)，让 Unity 知道需要写盘
            EditorUtility.SetDirty(currentLevelData);
            AssetDatabase.SaveAssets();
            Debug.Log("关卡保存成功！");
        }

        private void RotateElement(LevelElement element)
        {
            // 简单的顺时针旋转逻辑
            switch (element.initialFacing)
            {
                case Direction.up: element.initialFacing = Direction.right; break;
                case Direction.right: element.initialFacing = Direction.down; break;
                case Direction.down: element.initialFacing = Direction.left; break;
                case Direction.left: element.initialFacing = Direction.up; break;
            }
        }

        // --- 辅助视觉方法 ---

        private Color GetColorByType(GridObjectType type)
        {
            switch (type)
            {
                case GridObjectType.None: return Color.black;
                case GridObjectType.Ground: return Color.gray;
                case GridObjectType.Wall: return new Color(0.3f, 0.3f, 0.3f); // 深灰
                case GridObjectType.Statue: return Color.cyan;
                case GridObjectType.GhostStatue: return Color.red;
                case GridObjectType.Scroll: return Color.yellow;
                case GridObjectType.Door: return Color.magenta;
                case GridObjectType.SpawnPoint: return Color.green;
                default: return Color.white;
            }
        }

        private string GetLabelText(LevelElement element)
        {
            string arrow = "";
            // 只有这些物体需要显示方向
            if (element.type == GridObjectType.Statue || element.type == GridObjectType.Player)
            {
                switch (element.initialFacing)
                {
                    case Direction.up: arrow = "↑"; break;
                    case Direction.down: arrow = "↓"; break;
                    case Direction.left: arrow = "←"; break;
                    case Direction.right: arrow = "→"; break;
                }
            }

            // 简写显示类型
            switch (element.type)
            {
                case GridObjectType.None: return "X";
                case GridObjectType.Ground: return "";
                case GridObjectType.Wall: return "█";
                case GridObjectType.Statue: return "S " + arrow;
                case GridObjectType.GhostStatue: return "E " + arrow; // E for Evil
                case GridObjectType.Scroll: return "Scr";
                case GridObjectType.Door: return "DR";
                case GridObjectType.SpawnPoint: return "P " + arrow;
                case GridObjectType.Player: return "PL" + arrow;
                default: return "?";
            }
        }
    }
}