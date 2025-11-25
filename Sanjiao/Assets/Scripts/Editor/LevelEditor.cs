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

        // --- 测试模式基础变量 ---
        private bool isTestMode = false;
        private GridCoordinates playerPos;
        private Direction playerFacing = Direction.down;
        
        // 引用标记
        private LevelElement spawnElement;
        private LevelElement playerElementRef;

        // --- 咏唱 (Chanting) 相关变量 ---
        private bool isChanting = false;            // 是否正在按住Q
        private double lastChantStepTime;           // 上一次咏唱步进的时间点
        private const double ChantInterval = 0.7;   // 咏唱间隔 (秒)

        // 【新增】定义咏唱节点结构，存储坐标和当前强度
        private struct ChantNode
        {
            public GridCoordinates coord;
            public int power;
        }

        // 【修改】存储咏唱经过的所有节点
        private List<ChantNode> chantPath = new List<ChantNode>(); 
        
        // 当前咏唱波头的行进方向
        private Direction currentWaveDir; 
        
        // 咏唱是否被阻挡/结束
        private bool isChantBlocked = false; 

        [MenuItem("Game/Level Editor")]
        public static void ShowWindow()
        {
            GetWindow<LevelEditor>("Level Editor");
        }

        private void OnInspectorUpdate()
        {
            // 只有在测试模式下才进行逻辑更新
            if (isTestMode)
            {
                // 处理咏唱的时间步进逻辑
                HandleChantLogic();
                
                // 强制重绘，保证动画流畅
                Repaint();
            }
        }

        // --- 核心修改：咏唱逻辑 ---
        private void HandleChantLogic()
        {
            // 如果没有在咏唱，或者咏唱已经被阻挡结束，就不做任何事
            if (!isChanting || isChantBlocked) return;

            // 检查时间间隔 (0.7s)
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
            
            // 咏唱起始点是玩家当前位置，初始强度设为 1
            chantPath.Add(new ChantNode { coord = playerPos, power = 1 });
            
            // 初始方向是玩家朝向
            currentWaveDir = playerFacing;
            
            // 记录时间
            lastChantStepTime = EditorApplication.timeSinceStartup;
            
            Debug.Log(">>> 开始咏唱 (Power: 1)");
        }

        private void StopChant()
        {
            isChanting = false;
            isChantBlocked = false;
            chantPath.Clear();
            Debug.Log("<<< 停止咏唱");
        }

        private void AdvanceChantWave()
        {
            // 获取当前波头（List中最后一个元素）
            ChantNode currentNode = chantPath[chantPath.Count - 1];
            GridCoordinates currentHeadPos = currentNode.coord;
            
            // 默认下一格的强度继承当前强度
            int nextPower = currentNode.power;

            // 1. 检测当前波头所在的格子
            // 注意：我们先看当前格子是什么，决定下一格去哪，以及强度是否变化
            LevelElement currentElement = tempMap[currentHeadPos.x, currentHeadPos.y];
            
            // 如果波头位置是普通雕像
            if (currentElement.type == GridObjectType.Statue)
            {
                // A. 改变方向：模拟雕像转向逻辑
                currentWaveDir = currentElement.initialFacing;
                
                // B. 增强强度：经过雕像后，后续波的强度 +1
                nextPower++;
                
                Debug.Log($"咏唱波经过雕像 ({currentHeadPos.x},{currentHeadPos.y}) -> 转向: {currentWaveDir}, 强度增强至: {nextPower}");
            }

            // 2. 计算下一个格子的坐标
            GridCoordinates nextPos = CalculateTargetGridPosition(currentHeadPos, currentWaveDir);

            // 3. 边界与阻挡检测
            
            // A. 地图边界检测
            if (nextPos.x < 0 || nextPos.x >= mapWidth || nextPos.y < 0 || nextPos.y >= mapHeight)
            {
                Debug.Log("咏唱波到达地图边缘，消散。");
                isChantBlocked = true;
                return;
            }

            // B. 障碍物检测
            LevelElement nextElement = tempMap[nextPos.x, nextPos.y];
            GridObjectType nextType = nextElement.type;

            // 墙壁阻挡
            if (nextType == GridObjectType.Wall)
            {
                Debug.Log("咏唱波撞墙湮灭。");
                isChantBlocked = true;
                return;
            }
            
            // 4. 成功延伸，加入新节点
            chantPath.Add(new ChantNode { coord = nextPos, power = nextPower });
        }

        // --- 输入处理 ---
        private void HandleTestModeInput()
        {
            if (!isTestMode || Event.current == null) return;

            // 1. 处理咏唱按键 (Q)
            // KeyDown: 开始咏唱
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Q)
            {
                StartChant();
                Event.current.Use();
                return;
            }
            // KeyUp: 停止咏唱
            if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Q)
            {
                StopChant();
                Event.current.Use();
                return;
            }

            // 如果正在咏唱，禁止移动
            if (isChanting) return;

            // 2. 处理移动按键
            if (Event.current.type == EventType.KeyDown)
            {
                KeyCode key = Event.current.keyCode;
                Direction moveDir = Direction.down;
                bool shouldMove = false;

                if (key == KeyCode.W) { moveDir = Direction.up; shouldMove = true; }
                else if (key == KeyCode.S) { moveDir = Direction.down; shouldMove = true; }
                else if (key == KeyCode.A) { moveDir = Direction.left; shouldMove = true; }
                else if (key == KeyCode.D) { moveDir = Direction.right; shouldMove = true; }
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
            GridCoordinates targetPos = CalculateTargetGridPosition(playerPos, moveDir);

            if (playerFacing != moveDir)
            {
                playerFacing = moveDir;
                Repaint();
                return;
            }

            if (targetPos.x < 0 || targetPos.x >= mapWidth || targetPos.y < 0 || targetPos.y >= mapHeight) return;

            LevelElement targetElement = tempMap[targetPos.x, targetPos.y];
            GridObjectType targetType = targetElement.type;

            if (targetType == GridObjectType.Wall) return;

            if (targetType == GridObjectType.None)
            {
                Debug.LogError("⚠️ 掉入虚空！玩家死亡！ ⚠️");
                ToggleTestMode(false); 
                LoadLevel(); 
                return;
            }

            if (targetType == GridObjectType.Statue)
            {
                GridCoordinates statueNextPos = CalculateTargetGridPosition(targetPos, moveDir);
                if (statueNextPos.x < 0 || statueNextPos.x >= mapWidth || statueNextPos.y < 0 || statueNextPos.y >= mapHeight) return;

                LevelElement statueNextElement = tempMap[statueNextPos.x, statueNextPos.y];
                if (statueNextElement.type != GridObjectType.Ground && statueNextElement.type != GridObjectType.SpawnPoint) return;

                statueNextElement.type = GridObjectType.Statue;
                statueNextElement.initialFacing = targetElement.initialFacing;
                targetElement.type = GridObjectType.Ground;
            }

            if (playerElementRef != null)
            {
                if (spawnElement != null && playerPos.x == spawnElement.position.x && playerPos.y == spawnElement.position.y)
                {
                    playerElementRef.type = GridObjectType.SpawnPoint;
                    playerElementRef.initialFacing = spawnElement.initialFacing;
                }
                else
                {
                    playerElementRef.type = GridObjectType.Ground;
                }
            }

            playerPos = targetPos;
            playerElementRef = tempMap[playerPos.x, playerPos.y];
            playerElementRef.type = GridObjectType.Player;
            playerElementRef.initialFacing = playerFacing;

            if (targetType == GridObjectType.Scroll)
            {
                Debug.Log($"🔔 拾取卷轴！");
            }

            Repaint();
        }

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
             // 简单的交互逻辑：改变周围雕像朝向
            GridCoordinates[] offsets = { new GridCoordinates(0, 1), new GridCoordinates(0, -1), new GridCoordinates(-1, 0), new GridCoordinates(1, 0) };
            bool hasInteracted = false;
            foreach (var offset in offsets)
            {
                int tx = playerPos.x + offset.x;
                int ty = playerPos.y + offset.y;
                if (tx >= 0 && tx < mapWidth && ty >= 0 && ty < mapHeight)
                {
                    if (tempMap[tx, ty].type == GridObjectType.Statue)
                    {
                        // 让雕像面向玩家
                        Direction faceToPlayer = Direction.down;
                        if (offset.x == 0 && offset.y == 1) faceToPlayer = Direction.down;
                        else if (offset.x == 0 && offset.y == -1) faceToPlayer = Direction.up;
                        else if (offset.x == -1 && offset.y == 0) faceToPlayer = Direction.right;
                        else if (offset.x == 1 && offset.y == 0) faceToPlayer = Direction.left;
                        
                        tempMap[tx, ty].initialFacing = faceToPlayer;
                        hasInteracted = true;
                    }
                }
            }
            if (hasInteracted) Repaint();
        }

        // --- GUI 绘制部分 ---
        private void OnGUI()
        {
            GUILayout.Label("关卡编辑器 (Level Editor)", EditorStyles.boldLabel);

            DrawTopToolbar();

            if (isTestMode)
            {
                HandleTestModeInput(); // 优先处理输入
                
                // 绘制测试模式下的 HUD
                string status = isChanting ? $"咏唱中... (长度: {chantPath.Count})" : "等待咏唱";
                EditorGUILayout.HelpBox($"【测试模式】 WASD移动 Q咏唱 E交互 R重置\n状态: {status}", MessageType.Warning);
            }

            if (tempMap == null) return;

            EditorGUILayout.Space();

            if (!isTestMode) DrawPalette();

            EditorGUILayout.Space();
            DrawGrid();
        }

        private void DrawTopToolbar()
        {
            EditorGUILayout.BeginVertical("box");
            currentLevelData = (LevelSO)EditorGUILayout.ObjectField("Level Data SO", currentLevelData, typeof(LevelSO), false);
            EditorGUILayout.BeginHorizontal();
            mapWidth = EditorGUILayout.IntField("Width", mapWidth);
            mapHeight = EditorGUILayout.IntField("Height", mapHeight);
            if (GUILayout.Button("重置/新建地图")) InitializeNewMap();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("读取数据 (Load)")) LoadLevel();
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("保存数据 (Save)")) SaveLevel();
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            
            // 测试模式开关
            GUI.backgroundColor = isTestMode ? Color.yellow : Color.white;
            if (GUILayout.Button(isTestMode ? "退出测试模式" : "进入测试模式")) ToggleTestMode(!isTestMode);
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndVertical();
        }

        private void DrawPalette()
        {
            EditorGUILayout.LabelField("笔刷选择:", EditorStyles.boldLabel);
            selectedType = (GridObjectType)EditorGUILayout.EnumPopup("Object Type", selectedType);
            EditorGUILayout.HelpBox("左键: 放置 | 右键: 旋转", MessageType.Info);
        }

        private void DrawGrid()
        {
            if (tempMap == null) return;
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
            if (isTestMode && element.type == GridObjectType.Player)
                element.initialFacing = playerFacing;

            // 1. 获取基础颜色
            Color cellColor = GetColorByType(element.type);
            string label = GetLabelText(element);
            
            // --- 核心修改：咏唱波的可视化与数值显示 ---
            bool isChantCell = false;
            int currentPower = 0;

            if (isTestMode && isChanting)
            {
                // 遍历查找当前格子是否在咏唱路径中
                foreach (var node in chantPath)
                {
                    if (node.coord.x == x && node.coord.y == y)
                    {
                        isChantCell = true;
                        currentPower = node.power; // 获取该节点的强度
                        break;
                    }
                }
            }

            if (isChantCell)
            {
                // 混合颜色：原本颜色 + 蓝色
                cellColor = Color.Lerp(cellColor, Color.blue, 0.5f);
                
                // 【修改】在Label中显示强度
                // 格式示例： (( S↑ : 2 )) 或 (( : 1 ))
                if (string.IsNullOrEmpty(label))
                    label = $"{currentPower} ";
                else
                    label = $"{label} : {currentPower}";
            }

            GUI.backgroundColor = cellColor;
            Rect cellRect = GUILayoutUtility.GetRect(40, 40);
            GUI.Box(cellRect, label, GUI.skin.button);

            // 点击逻辑 (仅非测试模式)
            if (!isTestMode)
            {
                if (Event.current.type == EventType.MouseDown && cellRect.Contains(Event.current.mousePosition))
                {
                    if (Event.current.button == 0) { element.type = selectedType; Event.current.Use(); }
                    else if (Event.current.button == 1) { RotateElement(element); Event.current.Use(); }
                    Repaint();
                }
            }

            GUI.backgroundColor = Color.white;
        }

        // --- 辅助方法 (保持不变) ---
        private void InitializeNewMap()
        {
            tempMap = new LevelElement[mapWidth, mapHeight];
            for (int x = 0; x < mapWidth; x++) for (int y = 0; y < mapHeight; y++)
            {
                tempMap[x, y] = new LevelElement();
                tempMap[x, y].position = new GridCoordinates(x, y);
                tempMap[x, y].type = GridObjectType.Ground;
            }
        }

        private void LoadLevel()
        {
            if (currentLevelData == null) { Debug.LogError("请拖入 LevelSO"); return; }
            mapWidth = currentLevelData.mapSize.x;
            mapHeight = currentLevelData.mapSize.y;
            InitializeNewMap();
            foreach (var el in currentLevelData.elements)
            {
                if (el.position.x >= 0 && el.position.x < mapWidth && el.position.y >= 0 && el.position.y < mapHeight)
                {
                    tempMap[el.position.x, el.position.y].type = el.type;
                    tempMap[el.position.x, el.position.y].initialFacing = el.initialFacing;
                }
            }
            if (isTestMode) ToggleTestMode(false); // 加载时重置测试模式
            Debug.Log("加载成功");
        }

        private void SaveLevel()
        {
            if (currentLevelData == null) return;
            currentLevelData.mapSize = new GridCoordinates(mapWidth, mapHeight);
            currentLevelData.elements.Clear();
            for (int x = 0; x < mapWidth; x++) for (int y = 0; y < mapHeight; y++)
            {
                LevelElement el = tempMap[x, y];
                LevelElement toSave = new LevelElement { position = new GridCoordinates(x, y), type = el.type, initialFacing = el.initialFacing };
                currentLevelData.elements.Add(toSave);
            }
            EditorUtility.SetDirty(currentLevelData);
            AssetDatabase.SaveAssets();
            Debug.Log("保存成功");
        }

        private void ToggleTestMode(bool enable)
        {
            isTestMode = enable;
            StopChant(); // 切换模式时重置咏唱
            
            if (isTestMode)
            {
                spawnElement = FindSpawnPoint();
                if (spawnElement != null)
                {
                    playerPos = spawnElement.position;
                    playerFacing = spawnElement.initialFacing;
                    playerElementRef = tempMap[playerPos.x, playerPos.y];
                    playerElementRef.type = GridObjectType.Player;
                    playerElementRef.initialFacing = playerFacing;
                }
                else
                {
                    isTestMode = false;
                    Debug.LogError("未找到出生点");
                }
            }
            else
            {
                if (playerElementRef != null && spawnElement != null)
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
            for (int x = 0; x < mapWidth; x++) for (int y = 0; y < mapHeight; y++) if (tempMap[x, y].type == GridObjectType.SpawnPoint) return tempMap[x, y];
            return null;
        }

        private void RotateElement(LevelElement element)
        {
            switch (element.initialFacing)
            {
                case Direction.up: element.initialFacing = Direction.right; break;
                case Direction.right: element.initialFacing = Direction.down; break;
                case Direction.down: element.initialFacing = Direction.left; break;
                case Direction.left: element.initialFacing = Direction.up; break;
            }
        }

        private Color GetColorByType(GridObjectType type)
        {
            switch (type)
            {
                case GridObjectType.None: return Color.black;
                case GridObjectType.Ground: return Color.gray;
                case GridObjectType.Wall: return new Color(0.3f, 0.3f, 0.3f);
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
            if (element.type == GridObjectType.Statue || element.type == GridObjectType.Player || element.type == GridObjectType.GhostStatue || element.type == GridObjectType.SpawnPoint)
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