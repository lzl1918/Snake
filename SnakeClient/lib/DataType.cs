using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib
{
    public enum DataType : byte
    {
        SnakeData,
        StageSizeQuery,
        PingTest,
        ChangeDirection,
        Prepared,
        PlayerStatus,
        PlayerDisconnected,
        GameStart,
        SnakeDied,
        GameEnd,
        PlayerIndex,
        MapData,
        UseInventory,
        AddInventory,
        HardChanged,
        PlayerName,
        Restart,
        SetName,
    }

}
