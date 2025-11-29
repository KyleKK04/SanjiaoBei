using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Data;

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

        [Header("Background Settings")] public GameObject backgroundPrefab; // 背景预制体
        private GameObject currentBackgroundInstance; // 【新增】当前场景中的背景实例引用

        [Header("Settings")] public float cellSize = 1f;
        public float cameraOffsetY = 3f;

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
            /*if (levels.Count > 0)
            {
                LoadLevel(0);
            }
            else
            {
                Debug.LogError("LevelManager: Levels list is empty!");
            }*/
        }

        public void LoadLevel(int index)
        {
            if (index < 0 || index >= levels.Count)
            {
                Debug.LogError($"LevelManager: Invalid level index {index}");
                return;
            }

            ClearCurrentLevel();

            currentLevelIndex = index;
            currentLevelData = levels[index];

            GenerateLevel();

            // 生成完关卡后，调整相机和背景
            CenterCameraAndBackground();

            Debug.Log($"LevelManager: Loaded Level {index}");
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

            // 2. 【新增】清理背景
            if (currentBackgroundInstance != null)
            {
                Destroy(currentBackgroundInstance);
                currentBackgroundInstance = null;
            }

            // 3. 清理特效和引用
            StopChant(); // 确保停止之前的协程和特效
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
                        case GridObjectType.SpawnPoint:
                            if (playerInstance == null && playerPrefab != null)
                            {
                                GameObject p = Instantiate(playerPrefab, transform);
                                playerInstance = p.GetComponent<PlayerMovement>();
                            }

                            if (playerInstance != null)
                            {
                                playerInstance.Init(element.position.x, element.position.y, element.initialFacing);
                                UpdateGrid(element.position.x, element.position.y, playerInstance);
                            }

                            continue;
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
                if (targetObj != null && targetObj.gridObjectType == GridObjectType.Door &&
                    !targetObj.isBlockingMovement)
                {
                    Debug.Log("Player entered the door!");
                    GameManager.Instance.WinLevel();
                }
            }
        }

        private List<GameObject> activeChantEffects = new List<GameObject>();

        public void CastChant(GridCoordinates startPos, Direction startDir)
        {
            StopChant();
            StartCoroutine(PropagateChant(startPos, startDir));
        }

        public void StopChant()
        {
            StopAllCoroutines();
            foreach (var effect in activeChantEffects)
            {
                if (effect != null) Destroy(effect);
            }

            activeChantEffects.Clear();
        }

        IEnumerator PropagateChant(GridCoordinates startPos, Direction startDir)
        {
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
                        currentDir = currentObj.direction;
                        power++;
                    }
                    else if (currentObj.gridObjectType == GridObjectType.Wall) break;
                    else if (currentObj.gridObjectType == GridObjectType.GhostStatue)
                    {
                        if (power < 3) break;
                    }
                    else if (currentObj.gridObjectType == GridObjectType.Door) break;
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
                                    obj.gridObjectType == GridObjectType.GhostStatue)) return false;
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
    }
}