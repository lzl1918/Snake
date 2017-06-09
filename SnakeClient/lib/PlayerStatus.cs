using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace lib
{
    public enum PlayerStatus : byte
    {
        NotConnected, Connected, Prepared, Playing, Disconnected, Died
    }
}
