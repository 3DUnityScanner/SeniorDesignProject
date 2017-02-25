using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityScanner3D.CameraIO
{
    public struct ColorDepthImage
    {
        public ColorDepthImage(Texture2D color, Texture2D depth)
        {
            ColorImage = color;
            DepthImage = depth;
        }

        public Texture2D ColorImage;
        public Texture2D DepthImage;
    }
}