using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityScanner3D.ComputerVision;

namespace UnityScanner3D.CameraIO
{
    public enum CameraStatus
    {
        Stopped,
        Running
    };

    public interface ICamera
    {
        void StartCapture();
        void StopCapture();
        ColorDepthImage GetImage();
        void SetImage(ColorDepthImage image);
        Vector3 Get3DPointFromPixel(int x, int y);
    }
}
