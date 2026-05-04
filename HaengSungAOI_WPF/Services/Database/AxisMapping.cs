using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaengSungAOI_WPF.Services.Database
{
    public class AxisMapping
    {
        //Infeed Robot
        public const ushort AxisX1 = 0;
        public const ushort AxisY1 = 1;
        public const ushort AxisC1 = 2;
        //Transfer Robot
        public const ushort AxisX2 = 3;
        public const ushort AxisZ2 = 4;
        //Outfeed Robot
        public const ushort NGConveyor = 6;
        public const ushort AxisX3 = 7;
        public const ushort AxisY3 = 8;
        //Rotation 1
        public const ushort AxisC2 = 13;
        //Rotation 2
        public const ushort AxisC3 = 5;
        //Camera 2 Focus
        public const ushort AxisZ3 = 10;
        //Camera 3 Focus
        public const ushort AxisZ4 = 9;
        //Outfeed racks
        public const ushort AxisZ5 = 11;
        public const ushort AxisZ6 = 12;
    }
}



