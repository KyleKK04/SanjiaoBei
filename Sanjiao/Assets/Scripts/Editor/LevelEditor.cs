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

        //测试用
        // --- 测试模式相关变量 ---
        private bool isTestMode = false;
        private GridCoordinates playerPos; // 玩家当前网格坐标
        private Direction playerFacing = Direction.down; // 玩家朝向
        private bool isPlayerMoving = false; // 模拟移动状态
        private float moveTimer = 0.2f; // 每次移动所需时间 (模拟步进移动)
        private float currentMoveTime = 0f;
        private const float MoveDuration = 0.2f; // 定义移动一次的时长

// 在 LevelSO 中找到玩家出生点
        private LevelElement spawnElement;

// 玩家在 tempMap 数组中的引用（用于在地图上标记位置）
        private LevelElement playerElementRef;

        [MenuItem("Game/Level Editor")]
        public static void ShowWindow()
        {
            GetWindow<LevelEditor>("Level Editor");
        }

        // LevelEditor.cs (新增方法)
        private void OnInspectorUpdate()
        {
            // 只有在测试模式下才进行模拟更新
            if (isTestMode)
            {
                // 强制重绘，以便 DrawCell 可以实时显示玩家位置
                Repaint();

                // 模拟平滑移动的计时器 (可选，这里简化为立即移动)
                // if (isPlayerMoving) { /* ... 移动逻辑 ... */ }

                // 核心：处理输入
                HandleTestModeInput();
            }
        }

        // LevelEditor.cs (新增方法)

        private void HandleTestModeInput()
        {
            // 注意：我们必须在 OnGUI 外部处理 Event.current，因为它可能会被 DrawCell 消耗掉。
            // 但是 OnInspectorUpdate 无法直接获取 Event.current。
            // 最简单的方式是仍在 OnGUI 内部处理，但放在 DrawGrid() 之前。

            // 为了实现即时响应，我们直接在 OnGUI 中处理按键事件
            if (!isTestMode || Event.current == null || Event.current.type != EventType.KeyDown)
                return;

            // 记录按键，防止穿透
            KeyCode key = Event.current.keyCode;
            Direction moveDir = Direction.down;
            bool shouldMove = false;

            if (key == KeyCode.W)
            {
                moveDir = Direction.up;
                shouldMove = true;
            }
            else if (key == KeyCode.S)
            {
                moveDir = Direction.down;
                shouldMove = true;
            }
            else if (key == KeyCode.A)
            {
                moveDir = Direction.left;
                shouldMove = true;
            }
            else if (key == KeyCode.D)
            {
                moveDir = Direction.right;
                shouldMove = true;
            }
            // 交互键 E
            else if (key == KeyCode.E)
            {
                InteractInTestMode();
                Event.current.Use();
                return;
            }


            if (shouldMove)
            {
                TryMoveInTestMode(moveDir);
                Event.current.Use(); // 消耗事件
            }
        }

        private void TryMoveInTestMode(Direction moveDir)
        {
            // 1. 计算目标坐标
            GridCoordinates targetPos = CalculateTargetGridPosition(playerPos, moveDir);

            // 2. 如果玩家朝向不一致，先转身 (消耗一次操作，不移动)
            if (playerFacing != moveDir)
            {
                playerFacing = moveDir;
                Repaint(); // 刷新箭头显示
                return;
            }
            
            // 3. 边界检查
            if (targetPos.x < 0 || targetPos.x >= mapWidth || targetPos.y < 0 || targetPos.y >= mapHeight)
            {
                Debug.LogWarning("尝试移动到地图边界外！");
                return;
            }

            // 4. 获取目标格子元素
            LevelElement targetElement = tempMap[targetPos.x, targetPos.y];
            GridObjectType targetType = targetElement.type;

            // 5. 阻挡判定 (墙壁)
            if (targetType == GridObjectType.Wall)
            {
                Debug.Log("被墙阻挡，无法移动。");
                return;
            }

            // 6. 掉入虚空 (None)
            if (targetType == GridObjectType.None)
            {
                Debug.LogError("⚠️ 掉入虚空！玩家死亡！ ⚠️");
                ToggleTestMode(false); // 强制退出测试模式
                LoadLevel(); // 重置关卡
                return;
            }

            // 7. 推动雕像逻辑
            if (targetType == GridObjectType.Statue)
            {
                // 计算雕像被推向的下一个格子
                GridCoordinates statueNextPos = CalculateTargetGridPosition(targetPos, moveDir);

                // 7.1 检查雕像推入位置是否越界
                if (statueNextPos.x < 0 || statueNextPos.x >= mapWidth ||
                    statueNextPos.y < 0 || statueNextPos.y >= mapHeight)
                {
                    Debug.LogWarning("雕像前方是地图边界，无法推动。");
                    return;
                }
                
                LevelElement statueNextElement = tempMap[statueNextPos.x, statueNextPos.y];
                GridObjectType statueNextType = statueNextElement.type;

                // 7.2 只有雕像前方是平地 (Ground) 时才能推动 (也不能推到另一个雕像或墙上)
                // 如果需要允许推入虚空，可在此修改逻辑
                if (statueNextType != GridObjectType.Ground && statueNextType != GridObjectType.SpawnPoint)
                {
                    Debug.LogWarning($"雕像前方被 {statueNextType} 阻挡，无法推动。");
                    return;
                }

                // 7.3 执行推动：更新 tempMap 数据
                // A. 移动雕像到新位置
                statueNextElement.type = GridObjectType.Statue;
                statueNextElement.initialFacing = targetElement.initialFacing; // 保持雕像原有朝向

                // B. 原雕像位置 (targetPos) 变为地面，等待玩家进入
                // 注意：这里不需要手动设为 Ground，因为下面 "8. 成功移动" 的逻辑会把玩家移动到这里，
                // 覆盖掉原本的 Statue 类型。但在逻辑上，它确实变成了空地。
                targetElement.type = GridObjectType.Ground; 
                
                Debug.Log("雕像推动成功！");
            }

            // 8. 成功移动：更新数组中的玩家位置

            // a. 清除旧位置的标记 (恢复为 SpawnPoint 或 Ground)
            if (playerElementRef != null)
            {
                // 退出旧位置时，如果是出生点，就恢复出生点类型
                if (spawnElement != null && playerPos.x == spawnElement.position.x && playerPos.y == spawnElement.position.y)
                {
                    playerElementRef.type = GridObjectType.SpawnPoint;
                    playerElementRef.initialFacing = spawnElement.initialFacing; // 恢复出生点朝向
                }
                else
                {
                    playerElementRef.type = GridObjectType.Ground; // 移动后留下地面
                }
            }

            // b. 更新玩家内存中的坐标
            playerPos = targetPos;

            // c. 更新新位置的引用和类型标记
            playerElementRef = tempMap[playerPos.x, playerPos.y];
            playerElementRef.type = GridObjectType.Player;
            playerElementRef.initialFacing = playerFacing;

            // d. 检查拾取卷轴
            if (targetType == GridObjectType.Scroll)
            {
                Debug.Log($"🔔 拾取卷轴！内容: (需在Runtime显示)");
                // 逻辑上卷轴被覆盖消失
            }

            // 9. 强制刷新编辑器界面
            Repaint();
        }

// LevelEditor.cs (从 PlayerMovement 借鉴并修改的辅助方法)
        private GridCoordinates CalculateTargetGridPosition(GridCoordinates currentCoord, Direction dir)
        {
            int targetX = currentCoord.x;
            int targetY = currentCoord.y;

            switch (dir)
            {
                case Direction.up: targetY += 1; break;
                case Direction.down: targetY -= 1; break;
                case Direction.left: targetX -= 1; break;
                case Direction.right: targetX += 1; break;
            }

            return new GridCoordinates(targetX, targetY);
        }

        private void InteractInTestMode()
        {
            // 交互逻辑 (暂未实现，留空)
            Debug.Log($"在位置 ({playerPos.x}, {playerPos.y}) 尝试交互 ({playerFacing} 方向)");
            // TODO: 实现雕像推动和门交互逻辑
            // 交互逻辑：改变周围雕像的朝向
            Debug.Log($"在位置 ({playerPos.x}, {playerPos.y}) 尝试交互 (玩家朝向: {playerFacing})");
            
            bool hasInteracted = false;

            // 定义四个方向的偏移量：上、下、左、右
            GridCoordinates[] offsets = new GridCoordinates[]
            {
                new GridCoordinates(0, 1),  // Up
                new GridCoordinates(0, -1), // Down
                new GridCoordinates(-1, 0), // Left
                new GridCoordinates(1, 0)   // Right
            };

            // 遍历周围四格
            foreach (var offset in offsets)
            {
                int targetX = playerPos.x + offset.x;
                int targetY = playerPos.y + offset.y;

                // 1. 边界检查
                if (targetX < 0 || targetX >= mapWidth || targetY < 0 || targetY >= mapHeight)
                    continue;

                // 2. 获取该位置的元素
                LevelElement targetElement = tempMap[targetX, targetY];

                // 3. 判断是否是雕像 (Statue)
                if (targetElement.type == GridObjectType.Statue)
                {
                    Direction faceToPlayer = Direction.down; // 默认值

                    if (offset.x == 0 && offset.y == 1)       // 雕像在玩家上方
                        faceToPlayer = Direction.down;        // 雕像应朝下看
                    else if (offset.x == 0 && offset.y == -1) // 雕像在玩家下方
                        faceToPlayer = Direction.up;          // 雕像应朝上看
                    else if (offset.x == -1 && offset.y == 0) // 雕像在玩家左侧
                        faceToPlayer = Direction.right;       // 雕像应朝右看
                    else if (offset.x == 1 && offset.y == 0)  // 雕像在玩家右侧
                        faceToPlayer = Direction.left;        // 雕像应朝左看
                    // 4. 修改雕像朝向与玩家相对
                    if (targetElement.initialFacing != faceToPlayer)
                    {
                        targetElement.initialFacing = faceToPlayer;
                        hasInteracted = true;
                        Debug.Log($"雕像 at ({targetX}, {targetY}) 朝向已改为 {faceToPlayer}");
                    }
                }
            }

            // 如果发生改变，强制重绘界面以更新箭头显示
            if (hasInteracted)
            {
                Repaint();
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("关卡编辑器 (Level Editor)", EditorStyles.boldLabel);

            // 1. 顶部栏 (含测试开关)
            DrawTopToolbar(); 
    
            // 如果开启了测试模式，优先截获键盘输入
            if (isTestMode)
            {
                HandleTestModeInput();
                EditorGUILayout.HelpBox("【测试模式中】\n使用 WASD 移动\n推雕像 / 拾卷轴 / 掉落虚空\n点击上方按钮退出", MessageType.Warning);
            }

            if (tempMap == null) return;

            EditorGUILayout.Space();

            // 2. 只有在【非测试模式】才显示笔刷面板
            if (!isTestMode)
            {
                DrawPalette();
            }

            EditorGUILayout.Space();

            // 3. 绘制网格 (内部已处理数据源切换)
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
            // --- 新增：测试模式开关 ---
            GUI.backgroundColor = isTestMode ? Color.yellow : Color.white;
            if (GUILayout.Button(isTestMode ? "退出测试模式" : "进入测试模式"))
            {
                ToggleTestMode(!isTestMode);
            }

            GUI.backgroundColor = Color.white;
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

            // 增加：在测试模式下，如果当前单元格被标记为 GridObjectType.Player
            // 那么它的类型和朝向应该使用玩家当前的状态，而不是 LevelElement 中的旧数据。
            // 这一步确保玩家图标能正确显示。
            if (isTestMode && element.type == GridObjectType.Player)
            {
                // 临时覆盖 element 的显示数据
                element.initialFacing = playerFacing;
            }

            // 1. 先保留颜色设置
            Color defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = GetColorByType(element.type);
            string label = GetLabelText(element);

            // 2. 关键点：不使用 GUILayout.Button 的返回值，而是先申请一块 40x40 的区域
            Rect cellRect = GUILayoutUtility.GetRect(40, 40);

            // 3. 在这个区域画一个按钮样式的盒子（仅用于显示，不负责逻辑）
            GUI.Box(cellRect, label, GUI.skin.button);

            // 4. 手动检测事件
// --- 关键修改：只有在【非测试模式】下才响应鼠标点击 ---
            if (!isTestMode)
            {
                if (Event.current.type == EventType.MouseDown && cellRect.Contains(Event.current.mousePosition))
                {
                    if (Event.current.button == 0) // 左键绘制
                    {
                        element.type = selectedType;
                        // ... 初始化方向逻辑 ...
                        Event.current.Use();
                    }
                    else if (Event.current.button == 1) // 右键旋转
                    {
                        RotateElement(element);
                        Event.current.Use();
                    }

                    Repaint();
                }
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

        // LevelEditor.cs (新增方法)
        private void ToggleTestMode(bool enable)
        {
            isTestMode = enable;
            if (isTestMode)
            {
                // 进入测试模式：寻找出生点，初始化玩家位置
                spawnElement = FindSpawnPoint();
                if (spawnElement != null)
                {
                    playerPos = spawnElement.position;
                    playerFacing = spawnElement.initialFacing;
                    isPlayerMoving = false;

                    // 找到 tempMap 中对应位置的引用，并将类型设为 Player
                    playerElementRef = tempMap[playerPos.x, playerPos.y];
                    playerElementRef.type = GridObjectType.Player;
                    playerElementRef.initialFacing = playerFacing;
                }
                else
                {
                    Debug.LogError("地图上没有找到玩家出生点 (SpawnPoint)！无法进入测试模式。");
                    isTestMode = false;
                }
            }
            else
            {
                // 退出测试模式：将玩家位置恢复为出生点类型
                if (playerElementRef != null)
                {
                    playerElementRef.type = spawnElement.type;
                    playerElementRef.initialFacing = spawnElement.initialFacing;
                    playerElementRef = null;
                }
            }

            Repaint();
        }

        private LevelElement FindSpawnPoint()
        {
            if (tempMap == null) return null;
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    if (tempMap[x, y].type == GridObjectType.SpawnPoint)
                    {
                        // 找到第一个出生点
                        return tempMap[x, y];
                    }
                }
            }

            return null;
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