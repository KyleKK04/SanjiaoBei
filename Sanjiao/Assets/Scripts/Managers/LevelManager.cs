using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Data;

namespace Game.Core
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance;

        [Header("Levels Configuration")]
        [Tooltip("将所有的 LevelSO 拖拽到这里")]
        public List<LevelSO> levels = new List<LevelSO>(); 
        
        // 当前正在运行的关卡数据（内部使用）
        private LevelSO currentLevelData;
        private int currentLevelIndex = 0;

        [Header("Prefabs Mapping")]
        public GameObject groundPrefab;
        public GameObject wallPrefab;
        public GameObject statuePrefab;
        public GameObject evilStatuePrefab;
        public GameObject scrollPrefab;
        public GameObject doorPrefab;
        public GameObject playerPrefab; 

        [Header("Settings")]
        public float cellSize = 1f; // 网格单元大小

        // 运行时网格数据
        private GridObject[,] gridMap;
        private int width;
        private int height;
        public PlayerMovement playerInstance; // 玩家实例引用

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            // 测试时默认读取第一关
            if (levels.Count > 0)
            {
                LoadLevel(0);
            }
            else
            {
                Debug.LogError("LevelManager: 没有在 Inspector 中配置任何关卡 (Levels 列表为空)！");
            }
        }

        // --- 关卡加载核心逻辑 ---

        public void LoadLevel(int index)
        {
            if (index < 0 || index >= levels.Count)
            {
                Debug.LogError($"LevelManager: 尝试加载无效的关卡索引 {index}");
                return;
            }

            ClearCurrentLevel();

            currentLevelIndex = index;
            currentLevelData = levels[index];

            GenerateLevel();

            Debug.Log($"LevelManager: 已加载关卡 {index} - {currentLevelData.levelName}");
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
                Debug.Log("LevelManager: 已经是最后一关了，恭喜通关游戏！");
                // TODO: 调用通关UI
            }
        }

        public void RestartLevel()
        {
            LoadLevel(currentLevelIndex);
        }

        private void ClearCurrentLevel()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
            gridMap = null;
            playerInstance = null;
        }

        private void GenerateLevel()
        {
            if (currentLevelData == null) return;

            width = currentLevelData.mapSize.x;
            height = currentLevelData.mapSize.y;
            gridMap = new GridObject[width, height];

            // 1. 铺地面
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
                    }
                }
            }
            
            // 2. 生成物体
            foreach (var element in currentLevelData.elements)
            {
                if (!currentLevelData.IsCoordinateInBounds(element.position)) continue;

                GameObject prefabToSpawn = null;
                switch (element.type)
                {
                    case GridObjectType.Wall: prefabToSpawn = wallPrefab; break;
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

                if (prefabToSpawn != null)
                {
                    GameObject obj = Instantiate(prefabToSpawn, transform); 
                    GridObject gridObj = obj.GetComponent<GridObject>();
                    
                    if (gridObj != null)
                    {
                        gridObj.Init(element.position.x, element.position.y, element.initialFacing);
                        if (gridObj is DoorController door)
                            door.SetRequiredPower(element.requiredDoorPower);

                        UpdateGrid(element.position.x, element.position.y, gridObj);
                    }
                }
            }

            // ==========================================
            // 【新增逻辑】3. 调整摄像机对准地图中心
            // ==========================================
            CenterCamera();
        }

        /// <summary>
        /// 将主摄像机移动到当前地图的中心位置
        /// </summary>
        private void CenterCamera()
        {
            if (Camera.main != null)
            {
                // 计算中心坐标
                // 假设地图从 (0,0) 到 (width-1, height-1)
                float centerX = (width - 1) * cellSize / 2f;
                float centerY = (height - 1) * cellSize / 2f;

                // 保持摄像机 Z 轴为 -10，防止裁切
                Camera.main.transform.position = new Vector3(centerX, centerY, -10f);
                
                // 【可选】根据地图大小自动调整摄像机视野 (Orthographic Size)
                // 确保地图能在屏幕内完全显示。+2 是为了留点边距。
                // Camera.main.orthographicSize = (height / 2f) + 2f;
            }
        }

        // --- 以下逻辑保持不变 ---

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
                MoveObjectInGrid(player, currentPos, targetPos);
                return true;
            }

            return false;
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
                statue.OnPush(pushDir); 

                MoveObjectInGrid(player, player.gridCoordinates, statueCurrent);
                return true;
            }
            return false;
        }

        private void MoveObjectInGrid(GridObject obj, GridCoordinates oldPos, GridCoordinates newPos)
        {
            if (IsBounds(oldPos.x, oldPos.y) && gridMap[oldPos.x, oldPos.y] == obj)
                gridMap[oldPos.x, oldPos.y] = null; 
            
            if (IsBounds(newPos.x, newPos.y))
                gridMap[newPos.x, newPos.y] = obj; 

            obj.gridCoordinates = newPos;
        }

        public void CastChant(GridCoordinates startPos, Direction startDir)
        {
            StartCoroutine(PropagateChant(startPos, startDir));
        }

        IEnumerator PropagateChant(GridCoordinates startPos, Direction startDir)
        {
            int power = 1;
            Direction currentDir = startDir;
            GridCoordinates currentPos = startPos;
            
            int safety = 0;
            while (safety < 50)
            {
                safety++;
                
                GridObject obj = GetGridObject(currentPos.x, currentPos.y);
                if (obj != null)
                {
                    obj.OnChant(power, currentDir);

                    if (obj.gridObjectType == GridObjectType.Statue)
                    {
                        currentDir = obj.direction; 
                        power++; 
                    }
                    else if (obj.gridObjectType == GridObjectType.Wall)
                    {
                        break; 
                    }
                    else if (obj.gridObjectType == GridObjectType.GhostStatue)
                    {
                        if (power < 3) break; 
                    }
                    else if (obj.gridObjectType == GridObjectType.Door)
                    {
                        break; 
                    }
                }

                currentPos = GetNextCoord(currentPos, currentDir);
                if (!IsBounds(currentPos.x, currentPos.y)) break;

                yield return new WaitForSeconds(0.1f); 
            }
        }
        
        public bool CheckLineOfSight(GridCoordinates start, Direction dir, GridCoordinates target)
        {
             GridCoordinates pos = start;
             int watchdog = 0;
             while(watchdog < 20)
             {
                 watchdog++;
                 pos = GetNextCoord(pos, dir);
                 if(!IsBounds(pos.x, pos.y)) return false;
                 
                 if(pos.x == target.x && pos.y == target.y) return true;

                 GridObject obj = GetGridObject(pos.x, pos.y);
                 if(obj != null && (obj.gridObjectType == GridObjectType.Wall || obj.gridObjectType == GridObjectType.Statue || obj.gridObjectType == GridObjectType.Door || obj.gridObjectType == GridObjectType.GhostStatue))
                 {
                     return false; 
                 }
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
    }
}