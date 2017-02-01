using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    }
}
