using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityScanner3D.ComputerVision
{
    class Clump
    {
        public Clump(IEnumerable<Vector3> points)
        {
            Points = points;
        }

        public Color Color { get; set; }
        public List<Vector3> Points { get; private set; }

        public IEnumerable<Color> GetClumpPixels(Texture2D image)
        {
            foreach (var p in Points)
                yield return image.GetPixel(p.x, p.y);
        }
    }
}
