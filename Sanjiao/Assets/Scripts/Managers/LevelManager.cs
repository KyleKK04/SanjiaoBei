using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Game.Data;
using Unity.VisualScripting;

namespace Game.Core
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance;

        [Header("Levels Configuration")] [Tooltip("将所有的 LevelSO 拖拽到这里")]
        public List<LevelSO> levels = new List<LevelSO>();

        private LevelSO currentLevelData;
        private int currentLevelIndex = 0;

        [Header("Prefabs Mapping")] public GameObject groundPrefab;
        public GameObject wallPrefab;
        public GameObject statuePrefab;
        public GameObject evilStatuePrefab;
        public GameObject scrollPrefab;
        public GameObject doorPrefab;
        public GameObject playerPrefab;
        public GameObject chantPrefab;
        public GameObject obstaclePrefab;

        [Header("Background Settings")] public GameObject backgroundPrefab; // 背景预制体
        public List<Sprite> backgroundSprites = new List<Sprite>();
        private GameObject currentBackgroundInstance; // 【新增】当前场景中的背景实例引用

        private List<StatueController> activeStatues = new List<StatueController>();


        [Header("Settings")] public float cellSize = 1f;
        public float cameraOffsetY = 3f;

        [Header("Text Settings")] private bool isShowedText = false;
        public List<DialogueLine> level1Dialog1 = new List<DialogueLine>();
        public List<DialogueLine> level1Dialog2 = new List<DialogueLine>();
        public List<DialogueLine> level1Dialog3 = new List<DialogueLine>();

        private GridObject[,] gridMap;
        private int width;
        private int height;
        public PlayerMovement playerInstance;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            SetText();
        }

        public async void LoadLevel(int index)
        {
            AudioManager.Instance.StopBGM();
            AudioManager.Instance.PlaySFX("Loading");
            await UIManager.Instance?.SwitchPanelAsync("Select", "Switch");
            if (index < 0 || index >= levels.Count)
            {
                Debug.LogError($"LevelManager: Invalid level index {index}");
                return;
            }

            ClearCurrentLevel();

            currentLevelIndex = index;
            currentLevelData = levels[index];

            GenerateLevel();
            if (index == 2 || index == 3 || index == 4 || index == 5 || index == 1)
            {
                cameraOffsetY = 1f;
            }

            if (index == 1)
            {
                cameraOffsetY = 0f;
            }

            cameraOffsetY = 1.8f;
            // 生成完关卡后，调整相机和背景
            CenterCameraAndBackground();
            await Task.Delay(2500); // 等待切换面板动画
            AudioManager.Instance.StopSFX();
            await UIManager.Instance?.SwitchPanelAsync("Switch", "InGame");
            AudioManager.Instance.PlayBGM("BGM");
            ShowDialog();
        }

        public void LoadNextLevel()
        {
            int nextIndex = currentLevelIndex + 1;
            if (nextIndex < levels.Count)
            {
                LoadLevel(nextIndex);
            }
            else
            {
                Debug.Log("LevelManager: Game Cleared!");
            }
        }

        public void RestartLevel()
        {
            LoadLevel(currentLevelIndex);
        }

        public void ClearCurrentLevel()
        {
            // 1. 清理网格物体
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            // 2. 清理背景
            if (currentBackgroundInstance != null)
            {
                Destroy(currentBackgroundInstance);
                currentBackgroundInstance = null;
            }

            // 3. 清理特效和引用
            // 【核心修改】传入 false，表示只清理特效，不停止音效
            // 这样 Game Over 的 "Fail" 声音就不会被切断了
            StopChant(stopSound: false);

            gridMap = null;
            playerInstance = null;
        }

        private void GenerateLevel()
        {
            if (currentLevelData == null) return;

            width = currentLevelData.mapSize.x;
            height = currentLevelData.mapSize.y;
            gridMap = new GridObject[width, height];

            // --- 1. 临时存储生成的地面物体，方便后续删除 ---
            GameObject[,] groundObjects = new GameObject[width, height];

            if (groundPrefab != null)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        GameObject groundObj = Instantiate(groundPrefab, transform);
                        groundObj.transform.position = new Vector3(x * cellSize, y * cellSize, 0);
                        SpriteRenderer sr = groundObj.GetComponent<SpriteRenderer>();
                        if (sr != null) sr.sortingOrder = -10;

                        // 存入临时数组
                        groundObjects[x, y] = groundObj;
                    }
                }
            }

            // --- 2. 生成物体 ---
            foreach (var element in currentLevelData.elements)
            {
                if (!currentLevelData.IsCoordinateInBounds(element.position)) continue;

                GameObject prefabToSpawn = null;

                // 【新增逻辑】如果是墙壁，且该位置有地面，则销毁地面
                if (element.type == GridObjectType.Wall)
                {
                    int gx = element.position.x;
                    int gy = element.position.y;
                    if (groundObjects[gx, gy] != null)
                    {
                        Destroy(groundObjects[gx, gy]);
                        groundObjects[gx, gy] = null;
                    }

                    prefabToSpawn = wallPrefab;
                }
                else
                {
                    // 其他类型的物体 (保持原样)
                    switch (element.type)
                    {
                        // case GridObjectType.Wall: ... (上面已经处理了)
                        case GridObjectType.Statue: prefabToSpawn = statuePrefab; break;
                        case GridObjectType.GhostStatue: prefabToSpawn = evilStatuePrefab; break;
                        case GridObjectType.Scroll: prefabToSpawn = scrollPrefab; break;
                        case GridObjectType.Door: prefabToSpawn = doorPrefab; break;
                        case GridObjectType.Obstacle: prefabToSpawn = obstaclePrefab; break;
                        case GridObjectType.SpawnPoint:
                            // 1. 在出生点位置生成一个起点门 (BeginDoor)
                            if (doorPrefab != null)
                            {
                                GameObject startDoorObj = Instantiate(doorPrefab, transform);
                                DoorController startDoorCtrl = startDoorObj.GetComponent<DoorController>();
                                if (startDoorCtrl != null)
                                {
                                    startDoorCtrl.SetDoorData(0, DoorType.BeginDoor);
                                    startDoorCtrl.Init(element.position.x, element.position.y, element.initialFacing);
                                    // 设置为起点门，无需等级，不可交互
                                    UpdateGrid(element.position.x, element.position.y, startDoorCtrl);
                                }
                            }

                            // 2. 计算玩家的实际出生位置 (向前一格)
                            GridCoordinates playerSpawnPos = GetNextCoord(element.position, element.initialFacing);

                            // 3. 生成或重置玩家
                            if (playerInstance == null && playerPrefab != null)
                            {
                                GameObject p = Instantiate(playerPrefab, transform);
                                playerInstance = p.GetComponent<PlayerMovement>();
                            }

                            if (playerInstance != null)
                            {
                                // 初始化在前方一格的位置，保持原有朝向
                                playerInstance.Init(playerSpawnPos.x, playerSpawnPos.y, element.initialFacing);
                                UpdateGrid(playerSpawnPos.x, playerSpawnPos.y, playerInstance);
                            }

                            continue; // 继续下一个循环
                        default: continue;
                    }
                }

                if (prefabToSpawn != null)
                {
                    GameObject obj = Instantiate(prefabToSpawn, transform);
                    GridObject gridObj = obj.GetComponent<GridObject>();

                    if (gridObj != null)
                    {
                        gridObj.Init(element.position.x, element.position.y, element.initialFacing);
                        if (gridObj is DoorController door)
                            door.SetDoorData(element.requiredDoorPower, element.doorType);

                        if (gridObj is ScrollController scroll &&
                            !string.IsNullOrEmpty(currentLevelData.scrollDialogue))
                        {
                            // scroll.SetTextContent...
                        }

                        UpdateGrid(element.position.x, element.position.y, gridObj);
                    }
                }
            }

            // 3. 调整摄像机
            CenterCameraAndBackground();
        }

        // 【修改】重命名并完善逻辑：同时调整相机中心和背景位置
        private void CenterCameraAndBackground()
        {
            // 1. 计算地图的几何中心点
            float centerX = (width - 1) * cellSize / 2f;
            float centerY = (height - 1) * cellSize / 2f;

            // 2. 计算相机的目标位置（带偏移）
            float targetCamY = centerY + cameraOffsetY;
            Vector3 cameraTargetPos = new Vector3(centerX, targetCamY, -10f);

            // 3. 设置相机
            if (Camera.main != null)
            {
                Camera.main.transform.position = cameraTargetPos;
            }
            else
            {
                Debug.LogWarning("LevelManager: 场景中找不到 Tag 为 MainCamera 的摄像机！");
            }

            // 4. 【新增】生成并设置背景
            if (backgroundPrefab != null)
            {
                // 如果当前还没有背景实例，就生成一个
                if (currentBackgroundInstance == null)
                {
                    // 生成时不要设为 transform 的子物体，防止被 ClearCurrentLevel 里的 transform.GetChild 误删（虽然我们在 Clear 里单独处理了）
                    // 建议保持独立，或者确保 Clear 逻辑正确
                    currentBackgroundInstance = Instantiate(backgroundPrefab);
                    SpriteRenderer bgSr = currentBackgroundInstance.GetComponent<SpriteRenderer>();
                    if (bgSr != null && backgroundSprites.Count > 0)
                    {
                        Debug.Log(currentLevelIndex);
                        bgSr.sprite = backgroundSprites[currentLevelIndex % 15];
                    }
                }

                // 将背景放置在相机正对的位置，但在 Z 轴上要远离相机 (Z=0 或 Z=5)
                // 假设物体在 Z=0，背景应该在 Z=10 (更远) 或者 Z=0 但 SortingLayer 很低
                currentBackgroundInstance.transform.position = new Vector3(centerX, targetCamY, 10f);
            }
        }

        // ... (以下辅助方法保持不变) ...

        public void UpdateGrid(int x, int y, GridObject obj)
        {
            if (IsBounds(x, y)) gridMap[x, y] = obj;
        }

        public GridObject GetGridObject(int x, int y)
        {
            if (IsBounds(x, y)) return gridMap[x, y];
            return null;
        }

        private bool IsBounds(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;

        public bool RequestMove(PlayerMovement player, Direction moveDir)
        {
            GridCoordinates currentPos = player.gridCoordinates;
            GridCoordinates targetPos = GetNextCoord(currentPos, moveDir);

            if (!IsBounds(targetPos.x, targetPos.y)) return false;

            GridObject targetObj = gridMap[targetPos.x, targetPos.y];

            if (targetObj == null || targetObj.gridObjectType == GridObjectType.Ground)
            {
                MoveObjectInGrid(player, currentPos, targetPos);
                return true;
            }

            if (targetObj.isBlockingMovement)
            {
                if (targetObj.gridObjectType == GridObjectType.Statue)
                {
                    return TryPushStatue((StatueController)targetObj, moveDir, player);
                }

                return false;
            }

            if (targetObj.gridObjectType == GridObjectType.Scroll)
            {
                ((ScrollController)targetObj).OnCollected();
                MoveObjectInGrid(player, currentPos, targetPos);
                return true;
            }

            if (targetObj.gridObjectType == GridObjectType.Door && !targetObj.isBlockingMovement)
            {
                MoveObjectInGrid(player, currentPos, targetPos);
                return true;
            }

            return true;
        }

        private bool TryPushStatue(StatueController statue, Direction pushDir, PlayerMovement player)
        {
            GridCoordinates statueCurrent = statue.gridCoordinates;
            GridCoordinates statueTarget = GetNextCoord(statueCurrent, pushDir);

            if (!IsBounds(statueTarget.x, statueTarget.y)) return false;

            GridObject objBehindStatue = gridMap[statueTarget.x, statueTarget.y];

            if (objBehindStatue == null || objBehindStatue.gridObjectType == GridObjectType.Ground)
            {
                MoveObjectInGrid(statue, statueCurrent, statueTarget);
                MoveObjectInGrid(player, player.gridCoordinates, statueCurrent);

                float pushTime = player.pushDuration;
                Vector3 targetWorldPos = new Vector3(statueTarget.x * cellSize, statueTarget.y * cellSize, 0);
                statue.OnPush(targetWorldPos, pushTime);
                return true;
            }

            return false;
        }

        private void MoveObjectInGrid(GridObject obj, GridCoordinates oldPos, GridCoordinates newPos)
        {
            if (IsBounds(oldPos.x, oldPos.y) && gridMap[oldPos.x, oldPos.y] == obj)
                gridMap[oldPos.x, oldPos.y] = null;

            GridObject targetObj = null;
            if (IsBounds(newPos.x, newPos.y))
                targetObj = gridMap[newPos.x, newPos.y];

            if (IsBounds(newPos.x, newPos.y))
                gridMap[newPos.x, newPos.y] = obj;

            obj.gridCoordinates = newPos;

            if (obj.gridObjectType == GridObjectType.Player)
            {
                // 检测门
                if (targetObj != null && targetObj.gridObjectType == GridObjectType.Door &&
                    !targetObj.isBlockingMovement)
                {
                    DoorController door = targetObj as DoorController;
                    if (door != null)
                    {
                        if (door.doorType == DoorType.EndDoor)
                        {
                            Debug.Log("Player entered End Door -> Normal Win");
                            GameManager.Instance.WinLevel();
                        }
                        // 【新增】第15关真结局检测
                        else if (door.doorType == DoorType.BeginDoor && GetCurrentLevelIndex() == 14)
                        {
                            Debug.Log("Player entered Begin Door -> TRUE ENDING!");
                            GameManager.Instance.RealWin();
                        }
                    }
                }
            }
        }

        private List<GameObject> activeChantEffects = new List<GameObject>();

        public void CastChant(GridCoordinates startPos, Direction startDir)
        {
            StopChant();
            StartCoroutine(PropagateChant(startPos, startDir));
        }

        public void StopChant(bool stopSound = true)
        {
            StopAllCoroutines();

            // 1. 清理咏唱特效
            foreach (var effect in activeChantEffects)
            {
                if (effect != null) Destroy(effect);
            }

            activeChantEffects.Clear();

            // 2. 【新增】重置所有发光的雕像
            foreach (var statue in activeStatues)
            {
                if (statue != null) statue.ResetChantState();
            }

            activeStatues.Clear(); // 清空列表

            if (stopSound)
            {
                AudioManager.Instance.StopSFX();
            }
        }

        IEnumerator PropagateChant(GridCoordinates startPos, Direction startDir)
        {
            AudioManager.Instance.PlaySFX("Chanting");
            int power = 1;
            Direction currentDir = startDir;
            GridCoordinates currentPos = startPos;
            float stepDelay = 0.15f;
            bool isFirstStep = true;

            int safety = 0;
            while (safety < 50)
            {
                safety++;
                GridObject currentObj = GetGridObject(currentPos.x, currentPos.y);
                GridCoordinates nextPos = GetNextCoord(currentPos, currentDir);
                GridObject nextObj = GetGridObject(nextPos.x, nextPos.y);

                bool isCurrentBlocking = currentObj != null && currentObj.isBlockingMovement;
                bool isNextBlocking = !IsBounds(nextPos.x, nextPos.y);
                if (!isNextBlocking && nextObj != null && nextObj.isBlockingMovement) isNextBlocking = true;

                if (chantPrefab != null && !isCurrentBlocking)
                {
                    Vector3 spawnPos = new Vector3(currentPos.x * cellSize, currentPos.y * cellSize, 0);
                    GameObject effect = Instantiate(chantPrefab, spawnPos, Quaternion.identity);
                    ChantEffectController controller = effect.GetComponent<ChantEffectController>();
                    if (controller != null) controller.Init(currentDir, isFirstStep, isNextBlocking);
                    activeChantEffects.Add(effect);
                }

                if (currentObj != null)
                {
                    currentObj.OnChant(power, currentDir);
                    if (currentObj.gridObjectType == GridObjectType.Statue)
                    {
                        // 【新增】将该雕像加入列表，以便后续重置
                        var statueCtrl = currentObj.GetComponent<StatueController>();
                        if (statueCtrl != null && !activeStatues.Contains(statueCtrl))
                        {
                            activeStatues.Add(statueCtrl);
                        }

                        currentDir = currentObj.direction;
                        power++;
                    }
                    else if (currentObj.gridObjectType == GridObjectType.Wall) break;
                    else if (currentObj.gridObjectType == GridObjectType.GhostStatue)
                    {
                        if (power < 3) break;
                    }
                    else if (currentObj.gridObjectType == GridObjectType.Door) break;
                    else if (currentObj.gridObjectType == GridObjectType.Obstacle)
                    {
                        break;
                    }
                }

                currentPos = GetNextCoord(currentPos, currentDir);
                if (!IsBounds(currentPos.x, currentPos.y)) break;
                isFirstStep = false;
                yield return new WaitForSeconds(stepDelay);
            }
        }

        public bool CheckLineOfSight(GridCoordinates start, Direction dir, GridCoordinates target)
        {
            GridCoordinates pos = start;
            int watchdog = 0;
            while (watchdog < 20)
            {
                watchdog++;
                pos = GetNextCoord(pos, dir);
                if (!IsBounds(pos.x, pos.y)) return false;
                if (pos.x == target.x && pos.y == target.y) return true;
                GridObject obj = GetGridObject(pos.x, pos.y);
                if (obj != null && (obj.gridObjectType == GridObjectType.Wall ||
                                    obj.gridObjectType == GridObjectType.Statue ||
                                    obj.gridObjectType == GridObjectType.Door ||
                                    obj.gridObjectType == GridObjectType.GhostStatue ||
                                    obj.gridObjectType == GridObjectType.Obstacle)) return false;
            }

            return false;
        }

        private GridCoordinates GetNextCoord(GridCoordinates cur, Direction dir)
        {
            switch (dir)
            {
                case Direction.up: return new GridCoordinates(cur.x, cur.y + 1);
                case Direction.down: return new GridCoordinates(cur.x, cur.y - 1);
                case Direction.left: return new GridCoordinates(cur.x - 1, cur.y);
                case Direction.right: return new GridCoordinates(cur.x + 1, cur.y);
            }

            return cur;
        }

        public int GetCurrentLevelIndex()
        {
            return currentLevelIndex;
        }

        public void ShowDialog()
        {
            Debug.Log("调用函数了");
            if (currentLevelIndex == 0)
            {
                Debug.Log("是第一关");
                DialogueManager.Instance.ShowDialogue(level1Dialog1);
            }

            isShowedText = true;
        }

        public void ClearAllText()
        {
        }

        public void SetText()
        {
            DialogueLine line1 = new DialogueLine();
            line1.Content = "…嗯…这里…是哪？";
            line1.CharacterSprite = DialogueManager.Instance.angel;
            level1Dialog1.Add(line1);

            DialogueLine line2 = new DialogueLine();
            line2.Content = "等等，这里是教堂？";
            line2.CharacterSprite = DialogueManager.Instance.angel;
            level1Dialog1.Add(line2);

            DialogueLine line3 = new DialogueLine();
            line3.Content = "天哪，天哪…我知道的…即使这是一场梦，神也的确眷顾着我。阿门……";
            line3.CharacterSprite = DialogueManager.Instance.angel;
            level1Dialog1.Add(line3);

            DialogueLine line4 = new DialogueLine();
            line4.Content = "（你可以使用按键WASD进行移动与转向，且可以使用R键随时重置关卡。）";
            line4.CharacterSprite = null;
            level1Dialog1.Add(line4);

            DialogueLine line5 = new DialogueLine();
            line5.Content = "（你获得了神赐予你的力量。你的力量支持你推动单个雕像，但无法推动多个雕像或其它物品）";
            line5.CharacterSprite = null;
            level1Dialog1.Add(line5);

            DialogueLine line6 = new DialogueLine();
            line6.Content = "（长按Q键进行咏唱。咏唱的初始等级为1，朝玩家朝向直线传播。）";
            line6.CharacterSprite = null;
            level1Dialog1.Add(line6);

            DialogueLine line7 = new DialogueLine();
            line7.Content = "（如果咏唱到达普及者雕像所在位置，则会转向雕像面朝的方向传播并等级+1。）";
            line7.CharacterSprite = null;
            level1Dialog1.Add(line7);

            DialogueLine line8 = new DialogueLine();
            line8.Content = "（终点大门荆棘上方显示的数字即为通关所需的最小咏唱等级。）";
            line8.CharacterSprite = null;
            level1Dialog1.Add(line8);

            DialogueLine line9 = new DialogueLine();
            line9.Content = "（拾取卷轴后将达到目标等级的咏唱传递至终点大门荆棘处才能摧毁荆棘并开启大门。）";
            line9.CharacterSprite = null;
            level1Dialog1.Add(line9);
        }

        public void OpenBeginDoor()
        {
            // 遍历网格查找
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    GridObject obj = gridMap[x, y];
                    if (obj != null && obj is DoorController door)
                    {
                        if (door.doorType == DoorType.BeginDoor)
                        {
                            door.ForceOpen(); // 调用刚才在 DoorController 里写的方法
                        }
                    }
                }
            }

            Debug.Log("LevelManager: Begin Door has been opened!");
        }
    }
}