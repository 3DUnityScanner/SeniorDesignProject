using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityScanner3D.ComputerVision
{
    public struct Pixel
    {
        public Pixel(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; private set; }
        public int Y { get; private set; }
    }
}
