using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteControlSimulator
{
    public enum ConnectioinState
    {
        [Description("已连接")] Connected,
        [Description("未连接")] Disconnected,
        [Description("连接中")] Connecting
    }
}
