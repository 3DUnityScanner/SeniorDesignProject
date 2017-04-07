using pointmatcher.net;
using System.Collections.Generic;
using UnityScanner3D.CameraIO;

namespace UnityScanner3D.ComputerVision
{
    interface IAlgorithm
    {
        void ProcessImage(ColorDepthImage image);
        IEnumerable<Shape> GetShapes();
        void ClearShapes();
        void ProcessPLY(string filename);
        void getPLYFromCloud(DataPoints cloud);
    }
}
