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

        public IEnumerable<Vector3> GetCloudPointData()
        {
            for (int x = 0; x < DepthImage.width; x++)
                for (int y = 0; y < DepthImage.height; y++)
                    yield return new Vector3(x, y, DepthImage.GetPixel(x, y).grayscale);
        }

        public Texture2D ColorImage { get; private set; }
        public Texture2D DepthImage { get; private set; }
    }
}