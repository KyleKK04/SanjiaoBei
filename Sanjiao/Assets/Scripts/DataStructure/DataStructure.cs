namespace Game.Data
{
    public enum Direction
    {
        up,
        down,
        left,
        right
    }

//用于存储X，Y坐标，推箱子游戏
    public struct GridCoordinates
    {
        public int x;
        public int y;

        public GridCoordinates(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

//标识格子上的物体类型（空地、墙壁、普通雕像、恶鬼雕像、卷轴、大门、出生点）。\
    public enum GridObjectType
    {
        ground,
        wall,
        statue,
        ghostStatue,
        scroll,
        door,
        spawnPoint
    }
}