using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace lib
{
    public enum MapType : byte
    {
        Empty,

        Fruit, // 普通的果子
        SuperFruit, // 吃了长5
        HiddenSuperFruitAsShortenFruit, // 长得像变短果子的炒鸡果子
        ShortenFruit, // 变短果
        SuperShortenFruit, // 变短5
        HiddenSuperShortenFruitAsSuperFruit, // 长得像炒鸡果子的炒鸡变短果
        BlindFruit,
        RushFruit,
        SlowFruit,
        HardFruit,
        Stone,
        PowerFruit,
        LastFruitPlaceHolder,

        // 预留
        SnakeStart = 20,
        // 预留
        SnakeHeadStart = 60,
        //.. 预留
        Border = 200,
        Unknow
    }
}
