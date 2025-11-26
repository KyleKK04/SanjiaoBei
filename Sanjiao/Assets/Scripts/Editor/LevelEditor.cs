using UnityEngine;
using UnityEditor;
using Game.Data;
using System.Collections.Generic;

namespace Game.EditorTools
{
    public class LevelEditor : EditorWindow
    {
        // --- 核心数据 ---
        private LevelSO currentLevelData;
        private LevelElement[,] tempMap;
        private GridObjectType selectedType = GridObjectType.Ground;
        private int mapWidth = 10;
        private int mapHeight = 10;
        private Vector2 scrollPosition;

        // --- 笔刷设置 ---
        private int brushDoorPower = 3; // 绘制大门时默认需要的咏唱等级

        // --- 测试模式基础变量 ---
        private bool isTestMode = false;
        private GridCoordinates playerPos;
        private Direction playerFacing = Direction.down;

        // 引用标记
        private LevelElement spawnElement;
        private LevelElement playerElementRef; // 玩家当前占据的格子数据的引用

        // --- 游戏状态标记 (测试模式用) ---
        private bool hasCollectedScroll = false; // 是否持有卷轴
        private HashSet<Vector2Int> poweredDoors = new HashSet<Vector2Int>(); // 存储已充能的大门坐标

        // --- 咏唱 (Chanting) 相关变量 ---
        private bool isChanting = false;
        private double lastChantStepTime;
        private const double ChantInterval = 0.5; // 加快一点节奏

        private struct ChantNode
        {
            public GridCoordinates coord;
            public int power;
        }

        private List<ChantNode> chantPath = new List<ChantNode>();
        private Direction currentWaveDir;
        private bool isChantBlocked = false;

        [MenuItem("Game/Level Editor")]
        public static void ShowWindow()
        {
            GetWindow<LevelEditor>("Level Editor");
        }

        private void OnInspectorUpdate()
        {
            // 测试模式下的实时逻辑更新
            if (isTestMode)
            {
                // 1. 恶鬼雕像威胁检测 (每帧检测)
                CheckEvilStatueLogic();

                // 2. 咏唱波推进逻辑
                HandleChantLogic();

                Repaint();
            }
        }

        // =========================================================
        //                 核心逻辑：恶鬼雕像威胁检测
        // =========================================================
        private void CheckEvilStatueLogic()
        {
            if (tempMap == null) return;

            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    LevelElement el = tempMap[x, y];
                    if (el.type == GridObjectType.GhostStatue)
                    {
                        // 1. 检测周围四格 (曼哈顿距离=1)
                        if (Mathf.Abs(x - playerPos.x) + Mathf.Abs(y - playerPos.y) == 1)
                        {
                            GameOver($"你太靠近恶鬼雕像了！({x},{y})");
                            return;
                        }

                        // 2. 检测视线 (射线)
                        if (IsPlayerInSight(el))
                        {
                            GameOver($"被恶鬼雕像发现了！({x},{y})");
                            return;
                        }
                    }
                }
            }
        }

        private bool IsPlayerInSight(LevelElement statue)
        {
            GridCoordinates dirVec = DirectionToGridVector(statue.initialFacing);
            int checkX = statue.position.x;
            int checkY = statue.position.y;

            while (true)
            {
                checkX += dirVec.x;
                checkY += dirVec.y;

                // 越界检测
                if (checkX < 0 || checkX >= mapWidth || checkY < 0 || checkY >= mapHeight) break;

                // 玩家检测
                if (checkX == playerPos.x && checkY == playerPos.y) return true;

                // 阻挡检测
                LevelElement target = tempMap[checkX, checkY];
                // 规则：会被普及者雕像阻挡。通常墙壁和大门也会阻挡视线。
                if (target.type == GridObjectType.Statue ||
                    target.type == GridObjectType.Wall ||
                    target.type == GridObjectType.Door ||
                    target.type == GridObjectType.GhostStatue)
                {
                    break;
                }
            }

            return false;
        }

        private void GameOver(string reason)
        {
            Debug.LogError("GAME OVER: " + reason);
            ToggleTestMode(false);
            EditorUtility.DisplayDialog("失败", reason, "重置");
            LoadLevel(); // 重新加载以重置地图状态
        }

        private void WinGame()
        {
            Debug.Log("LEVEL CLEAR!");
            ToggleTestMode(false);
            EditorUtility.DisplayDialog("通关", "恭喜你打开了大门！", "OK");
        }

        // =========================================================
        //                 核心逻辑：咏唱与物体交互
        // =========================================================
        private void HandleChantLogic()
        {
            if (!isChanting || isChantBlocked) return;

            double currentTime = EditorApplication.timeSinceStartup;
            if (currentTime - lastChantStepTime >= ChantInterval)
            {
                AdvanceChantWave();
                lastChantStepTime = currentTime;
            }
        }

        private void StartChant()
        {
            if (isChanting) return;
            isChanting = true;
            isChantBlocked = false;
            chantPath.Clear();
            chantPath.Add(new ChantNode { coord = playerPos, power = 1 });
            currentWaveDir = playerFacing;
            lastChantStepTime = EditorApplication.timeSinceStartup;
            Debug.Log(">>> 开始咏唱");
        }

        private void StopChant()
        {
            isChanting = false;
            isChantBlocked = false;
            chantPath.Clear();
        }

        private void AdvanceChantWave()
        {
            ChantNode currentNode = chantPath[chantPath.Count - 1];
            GridCoordinates currentHeadPos = currentNode.coord;

            // 默认继承强度
            int nextPower = currentNode.power;

            // 1. 当前格子的转向/增强处理
            LevelElement currentElement = tempMap[currentHeadPos.x, currentHeadPos.y];
            if (currentElement.type == GridObjectType.Statue)
            {
                currentWaveDir = currentElement.initialFacing;
                nextPower++; // 普及者雕像增强咏唱
            }

            // 2. 计算下一格位置
            GridCoordinates nextPos = currentHeadPos + DirectionToGridVector(currentWaveDir);

            // 3. 边界检测
            if (nextPos.x < 0 || nextPos.x >= mapWidth || nextPos.y < 0 || nextPos.y >= mapHeight)
            {
                isChantBlocked = true;
                return;
            }

            // 4. 障碍物与交互检测
            LevelElement nextElement = tempMap[nextPos.x, nextPos.y];
            GridObjectType nextType = nextElement.type;

            // --- 墙壁 ---
            if (nextType == GridObjectType.Wall)
            {
                isChantBlocked = true;
                return;
            }

            // --- 恶鬼雕像 (GhostStatue) ---
            if (nextType == GridObjectType.GhostStatue)
            {
                if (nextPower < 3)
                {
                    Debug.Log($"咏唱(Lv.{nextPower}) 被恶鬼雕像阻挡。");
                    isChantBlocked = true;
                    return;
                }
                else
                {
                    Debug.Log($"咏唱(Lv.{nextPower}) 摧毁了恶鬼雕像！");
                    // 摧毁逻辑：将格子变为 Ground
                    nextElement.type = GridObjectType.Ground;
                    // 咏唱继续传播，不停止
                }
            }

            // --- 终点大门 (Door) ---
            if (nextType == GridObjectType.Door)
            {
                // 【核心修改】增加了 && hasCollectedScroll 判断
                // 只有在【已拾取卷轴】且【强度足够】时，大门才会被激活
                if (hasCollectedScroll && nextPower >= nextElement.requiredDoorPower)
                {
                    Debug.Log($"大门充能成功！(当前:{nextPower}, 需求:{nextElement.requiredDoorPower})");
                    // 记录该门已被充能
                    poweredDoors.Add(new Vector2Int(nextPos.x, nextPos.y));
                }
                else
                {
                    // 这里没有任何反应，只是打印调试信息
                    if (!hasCollectedScroll)
                        Debug.Log($"大门毫无反应：虽然被击中，但你尚未拾取卷轴。");
                    else
                        Debug.Log($"大门毫无反应：充能不足 (当前:{nextPower}, 需求:{nextElement.requiredDoorPower})");
                }

                // 大门视为实体，无论是否激活都会阻挡咏唱继续传播
                isChantBlocked = true;
                return;
            }

            // 5. 成功延伸
            chantPath.Add(new ChantNode { coord = nextPos, power = nextPower });
        }

        // =========================================================
        //                 玩家移动与交互逻辑
        // =========================================================
        private void HandleTestModeInput()
        {
            if (!isTestMode || Event.current == null) return;

            // 咏唱输入 (Q)
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Q)
            {
                StartChant();
                Event.current.Use();
                return;
            }

            if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Q)
            {
                StopChant();
                Event.current.Use();
                return;
            }

            if (isChanting) return;

            // 移动与交互
            if (Event.current.type == EventType.KeyDown)
            {
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
                else if (key == KeyCode.E)
                {
                    InteractInTestMode();
                    Event.current.Use();
                }
                else if (key == KeyCode.R)
                {
                    LoadLevel(); // 重开
                }

                if (shouldMove)
                {
                    TryMoveInTestMode(moveDir);
                    Event.current.Use();
                }
            }
        }

        private void TryMoveInTestMode(Direction moveDir)
        {
            GridCoordinates targetPos = playerPos + DirectionToGridVector(moveDir);

            // 转向逻辑
            if (playerFacing != moveDir)
            {
                playerFacing = moveDir;
                if (playerElementRef != null)
                {
                    playerElementRef.initialFacing = playerFacing;
                }

                Repaint();
                return;
            }

            // 边界检查
            if (targetPos.x < 0 || targetPos.x >= mapWidth || targetPos.y < 0 || targetPos.y >= mapHeight) return;

            LevelElement targetElement = tempMap[targetPos.x, targetPos.y];
            GridObjectType targetType = targetElement.type;

            // 阻挡物检查
            if (targetType == GridObjectType.Wall) return;
            if (targetType == GridObjectType.Door) return;
            if (targetType == GridObjectType.GhostStatue) return;

            // 虚空检查
            if (targetType == GridObjectType.None)
            {
                GameOver("掉入虚空！");
                return;
            }

            // 推动雕像逻辑
            if (targetType == GridObjectType.Statue)
            {
                GridCoordinates pushPos = targetPos + DirectionToGridVector(moveDir);
                if (pushPos.x >= 0 && pushPos.x < mapWidth && pushPos.y >= 0 && pushPos.y < mapHeight)
                {
                    LevelElement pushTarget = tempMap[pushPos.x, pushPos.y];
                    if (pushTarget.type == GridObjectType.Ground || pushTarget.type == GridObjectType.SpawnPoint)
                    {
                        pushTarget.type = GridObjectType.Statue;
                        pushTarget.initialFacing = targetElement.initialFacing;
                        targetElement.type = GridObjectType.Ground;
                    }
                    else return;
                }
                else return;
            }

            // 移动成功：更新玩家位置
            // 注意：这一步已经把 targetPos 的格子类型改成了 Player，原本的 Scroll 被覆盖了
            MovePlayerTo(targetPos);

            // 拾取卷轴逻辑
            if (targetType == GridObjectType.Scroll)
            {
                hasCollectedScroll = true;
                Debug.Log($"🔔 拾取卷轴！");

                // 【已删除】 tempMap[playerPos.x, playerPos.y].type = GridObjectType.Ground;
                // 不需要这行了，MovePlayerTo 已经把这里变成了 Player。
                // 等玩家下次移动离开这里时，还原逻辑会自动把它变成 Ground。
            }

            Repaint();
        }

        private void MovePlayerTo(GridCoordinates newPos)
        {
            // 恢复原位置的类型 (出生点或地面)
            if (playerElementRef != null)
            {
                if (spawnElement != null && playerPos.x == spawnElement.position.x &&
                    playerPos.y == spawnElement.position.y)
                {
                    playerElementRef.type = GridObjectType.SpawnPoint;
                    playerElementRef.initialFacing = spawnElement.initialFacing;
                }
                else
                {
                    playerElementRef.type = GridObjectType.Ground;
                }
            }

            playerPos = newPos;
            playerElementRef = tempMap[playerPos.x, playerPos.y];
            playerElementRef.type = GridObjectType.Player;
            playerElementRef.initialFacing = playerFacing;
        }

        private void InteractInTestMode()
        {
            // --- 逻辑 1：恢复原本的雕像交互 (周围四格让雕像看向玩家) ---
            // 定义四周偏移量
            GridCoordinates[] offsets =
            {
                new GridCoordinates(0, 1), // 上
                new GridCoordinates(0, -1), // 下
                new GridCoordinates(-1, 0), // 左
                new GridCoordinates(1, 0) // 右
            };

            foreach (var offset in offsets)
            {
                int tx = playerPos.x + offset.x;
                int ty = playerPos.y + offset.y;

                // 边界检查
                if (tx >= 0 && tx < mapWidth && ty >= 0 && ty < mapHeight)
                {
                    // 如果周围是普通雕像，让它转头面向玩家
                    if (tempMap[tx, ty].type == GridObjectType.Statue)
                    {
                        Direction faceToPlayer = Direction.down;
                        // offset 是 (雕像 - 玩家)，所以反过来推导雕像应该朝哪看
                        if (offset.x == 0 && offset.y == 1) faceToPlayer = Direction.down; // 雕像在玩家上方 -> 朝下看
                        else if (offset.x == 0 && offset.y == -1) faceToPlayer = Direction.up; // 雕像在玩家下方 -> 朝上看
                        else if (offset.x == -1 && offset.y == 0) faceToPlayer = Direction.right; // 雕像在玩家左侧 -> 朝右看
                        else if (offset.x == 1 && offset.y == 0) faceToPlayer = Direction.left; // 雕像在玩家右侧 -> 朝左看

                        tempMap[tx, ty].initialFacing = faceToPlayer;
                        Debug.Log($"雕像 ({tx},{ty}) 转向了玩家");
                    }
                }
            }

            // --- 逻辑 2：大门交互 (针对玩家正前方) ---
            GridCoordinates frontPos = playerPos + DirectionToGridVector(playerFacing);
            if (frontPos.x >= 0 && frontPos.x < mapWidth && frontPos.y >= 0 && frontPos.y < mapHeight)
            {
                LevelElement frontElement = tempMap[frontPos.x, frontPos.y];
                if (frontElement.type == GridObjectType.Door)
                {
                    bool isPowered = poweredDoors.Contains(new Vector2Int(frontPos.x, frontPos.y));
                    if (hasCollectedScroll && isPowered)
                    {
                        WinGame();
                    }
                    else
                    {
                        string tips = "无法打开大门：";
                        if (!hasCollectedScroll) tips += "[未拾取卷轴] ";
                        if (!isPowered) tips += "[大门未充能] ";
                        Debug.Log(tips);
                    }
                }
            }

            Repaint();
        }

        // =========================================================
        //                 GUI 绘制与辅助方法
        // =========================================================
        private void OnGUI()
        {
            GUILayout.Label("关卡编辑器 (Level Editor)", EditorStyles.boldLabel);
            DrawTopToolbar();

            if (isTestMode)
            {
                HandleTestModeInput();
                string status = $"【测试中】 卷轴: {(hasCollectedScroll ? "YES" : "NO")} | 按Q咏唱 | E交互";
                EditorGUILayout.HelpBox(status, MessageType.Info);
            }
            else
            {
                DrawPalette();
            }

            if (tempMap == null) return;
            EditorGUILayout.Space();
            DrawGrid();
        }

        private void DrawTopToolbar()
        {
            EditorGUILayout.BeginVertical("box");
            currentLevelData = (LevelSO)EditorGUILayout.ObjectField("Data", currentLevelData, typeof(LevelSO), false);

            EditorGUILayout.BeginHorizontal();
            mapWidth = EditorGUILayout.IntField("W", mapWidth);
            mapHeight = EditorGUILayout.IntField("H", mapHeight);
            if (GUILayout.Button("New")) InitializeNewMap();
            if (GUILayout.Button("Load")) LoadLevel();
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Save")) SaveLevel();
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            GUI.backgroundColor = isTestMode ? Color.yellow : Color.white;
            if (GUILayout.Button(isTestMode ? "退出测试" : "开始测试")) ToggleTestMode(!isTestMode);
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();
        }

        private void DrawPalette()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("笔刷:", GUILayout.Width(40));
            selectedType = (GridObjectType)EditorGUILayout.EnumPopup(selectedType);

            // 如果选中大门，显示所需的等级设置
            if (selectedType == GridObjectType.Door)
            {
                EditorGUILayout.LabelField("需等级:", GUILayout.Width(45));
                brushDoorPower = EditorGUILayout.IntField(brushDoorPower, GUILayout.Width(30));
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("左键放置/设置 | 右键旋转", MessageType.None);
        }

        private void DrawGrid()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.BeginVertical();
            for (int y = mapHeight - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
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
            Color cellColor = GetColorByType(element.type);
            string label = GetLabelText(element);

            // --- 特殊状态可视化 ---

            // 1. 咏唱波显示
            if (isTestMode && isChanting)
            {
                foreach (var node in chantPath)
                {
                    if (node.coord.x == x && node.coord.y == y)
                    {
                        cellColor = Color.Lerp(cellColor, Color.blue, 0.6f);
                        label += $"\n{node.power}";
                        break;
                    }
                }
            }

            // 2. 大门状态显示
            if (element.type == GridObjectType.Door)
            {
                bool isPowered = isTestMode && poweredDoors.Contains(new Vector2Int(x, y));
                if (isPowered)
                {
                    cellColor = Color.cyan; // 激活后发光
                    label += " [ON]";
                }
                else
                {
                    label += $"{element.requiredDoorPower}";
                }
            }

            GUI.backgroundColor = cellColor;
            Rect cellRect = GUILayoutUtility.GetRect(45, 45);
            GUI.Box(cellRect, label, GUI.skin.button);

            // 编辑操作 (仅非测试模式)
            if (!isTestMode)
            {
                if (Event.current.type == EventType.MouseDown && cellRect.Contains(Event.current.mousePosition))
                {
                    if (Event.current.button == 0)
                    {
                        // 放置
                        element.type = selectedType;
                        // 如果是门，应用笔刷的等级设置
                        if (selectedType == GridObjectType.Door) element.requiredDoorPower = brushDoorPower;
                        Event.current.Use();
                    }
                    else if (Event.current.button == 1)
                    {
                        // 旋转
                        element.initialFacing = RotateDirection(element.initialFacing);
                        Event.current.Use();
                    }

                    Repaint();
                }
            }

            GUI.backgroundColor = Color.white;
        }

        // --- 辅助工具方法 ---

        private GridCoordinates DirectionToGridVector(Direction dir)
        {
            switch (dir)
            {
                case Direction.up: return new GridCoordinates(0, 1);
                case Direction.down: return new GridCoordinates(0, -1);
                case Direction.left: return new GridCoordinates(-1, 0);
                case Direction.right: return new GridCoordinates(1, 0);
                default: return new GridCoordinates(0, 0);
            }
        }

        private Direction RotateDirection(Direction dir)
        {
            if (dir == Direction.up) return Direction.right;
            if (dir == Direction.right) return Direction.down;
            if (dir == Direction.down) return Direction.left;
            return Direction.up;
        }

        private void InitializeNewMap()
        {
            tempMap = new LevelElement[mapWidth, mapHeight];
            for (int x = 0; x < mapWidth; x++)
            for (int y = 0; y < mapHeight; y++)
            {
                tempMap[x, y] = new LevelElement();
                tempMap[x, y].position = new GridCoordinates(x, y);
                tempMap[x, y].type = GridObjectType.Ground;
            }
        }

        private void LoadLevel()
        {
            if (currentLevelData == null) return;
            mapWidth = currentLevelData.mapSize.x;
            mapHeight = currentLevelData.mapSize.y;
            InitializeNewMap();
            foreach (var el in currentLevelData.elements)
            {
                if (el.position.x >= 0 && el.position.x < mapWidth && el.position.y >= 0 && el.position.y < mapHeight)
                {
                    LevelElement mapEl = tempMap[el.position.x, el.position.y];
                    mapEl.type = el.type;
                    mapEl.initialFacing = el.initialFacing;
                    mapEl.requiredDoorPower = el.requiredDoorPower; // 读取门等级
                }
            }

            if (isTestMode) ToggleTestMode(false);
        }

        private void SaveLevel()
        {
            if (currentLevelData == null) return;
            currentLevelData.mapSize = new GridCoordinates(mapWidth, mapHeight);
            currentLevelData.elements.Clear();
            for (int x = 0; x < mapWidth; x++)
            for (int y = 0; y < mapHeight; y++)
            {
                LevelElement el = tempMap[x, y];
                if (el.type != GridObjectType.Ground && el.type != GridObjectType.None)
                {
                    LevelElement toSave = new LevelElement
                    {
                        position = new GridCoordinates(x, y),
                        type = el.type,
                        initialFacing = el.initialFacing,
                        requiredDoorPower = el.requiredDoorPower
                    };
                    currentLevelData.elements.Add(toSave);
                }
            }

            EditorUtility.SetDirty(currentLevelData);
            AssetDatabase.SaveAssets();
            Debug.Log("Saved.");
        }

        private void ToggleTestMode(bool enable)
        {
            isTestMode = enable;
            StopChant();
            hasCollectedScroll = false;
            poweredDoors.Clear();

            if (isTestMode)
            {
                spawnElement = null;
                // 查找出生点
                foreach (var el in tempMap)
                    if (el.type == GridObjectType.SpawnPoint)
                        spawnElement = el;

                if (spawnElement != null)
                {
                    playerPos = spawnElement.position;
                    playerFacing = spawnElement.initialFacing;
                    MovePlayerTo(playerPos); // 初始化玩家视觉位置
                }
                else
                {
                    isTestMode = false;
                    Debug.LogError("地图中没有玩家出生点 (SpawnPoint)！");
                }
            }
            else
            {
                LoadLevel(); // 退出时重置地图状态（比如复活恶鬼雕像）
            }
        }

        private Color GetColorByType(GridObjectType type)
        {
            switch (type)
            {
                case GridObjectType.None: return Color.black;
                case GridObjectType.Ground: return new Color(0.8f, 0.8f, 0.8f);
                case GridObjectType.Wall: return new Color(0.3f, 0.3f, 0.3f);
                case GridObjectType.Statue: return Color.cyan;
                case GridObjectType.GhostStatue: return new Color(0.8f, 0f, 0f); // 深红
                case GridObjectType.Scroll: return Color.yellow;
                case GridObjectType.Door: return new Color(0.5f, 0f, 0.5f); // 紫色
                case GridObjectType.SpawnPoint: return Color.green;
                case GridObjectType.Player: return Color.white;
                default: return Color.white;
            }
        }

        private string GetLabelText(LevelElement element)
        {
            string arrow = "";
            if (element.type == GridObjectType.Statue || element.type == GridObjectType.Player ||
                element.type == GridObjectType.GhostStatue || element.type == GridObjectType.SpawnPoint)
            {
                switch (element.initialFacing)
                {
                    case Direction.up: arrow = "↑"; break;
                    case Direction.down: arrow = "↓"; break;
                    case Direction.left: arrow = "←"; break;
                    case Direction.right: arrow = "→"; break;
                }
            }

            switch (element.type)
            {
                case GridObjectType.None: return "X";
                case GridObjectType.Ground: return "";
                case GridObjectType.Wall: return "█";
                case GridObjectType.Statue: return "S " + arrow;
                case GridObjectType.GhostStatue: return "E " + arrow;
                case GridObjectType.Scroll: return "Scr";
                case GridObjectType.Door: return "DR";
                case GridObjectType.SpawnPoint: return "P " + arrow;
                case GridObjectType.Player: return "PL" + arrow;
                default: return "?";
            }
        }
    }
}