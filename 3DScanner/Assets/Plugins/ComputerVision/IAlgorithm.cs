using System.Collections.Generic;
using UnityEngine;
using UnityScanner3D.CameraIO;

namespace UnityScanner3D.ComputerVision
{
    interface IAlgorithm
    {
        void ProcessImage(ICamera cam, ColorDepthImage image);
        Texture2D PreviewImage(ColorDepthImage image);
        IEnumerable<GameObject> GetShapes();
        void ClearShapes();
        void DrawSettings();
    }
}