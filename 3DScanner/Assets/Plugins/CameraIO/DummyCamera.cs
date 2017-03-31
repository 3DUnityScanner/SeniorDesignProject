using System;
using UnityEngine;

namespace UnityScanner3D.CameraIO
{
    internal class DummyCamera : ICamera
    {
        public CameraStatus Status { get; private set; }

        public ColorDepthImage GetImage()
        {
            if (Status != CameraStatus.Running)
                throw new Exception("The camera must be running before images can be captured.");

            const int WIDTH = 200;
            const int HEIGHT = 200;

            Texture2D color = new Texture2D(WIDTH, HEIGHT);
            Texture2D depth = new Texture2D(WIDTH, HEIGHT);

            for (int i = 0; i < HEIGHT; i++)
                for (int j = 0; j < WIDTH; j++)
                    color.SetPixel(i, j, isBlack ? Color.yellow : Color.red);

            isBlack = !isBlack;
            return new ColorDepthImage(color, depth);
        }

        public void StartCapture()
        {
            if (Status == CameraStatus.Running)
                throw new Exception("A camera can't be started if it is already running.");
            Status = CameraStatus.Running;
        }

        public void StopCapture()
        {
            if (Status == CameraStatus.Stopped)
                throw new Exception("A camera can't be stopped if it is already stopped.");
            Status = CameraStatus.Stopped;
        }

        private bool isBlack = true;
    }
}