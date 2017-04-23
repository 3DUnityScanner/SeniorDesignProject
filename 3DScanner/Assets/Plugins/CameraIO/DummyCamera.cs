using System;
using UnityEngine;

namespace UnityScanner3D.CameraIO
{
    internal class DummyCamera : ICamera
    {
        public CameraStatus Status { get; private set; }

        public Vector3 Get3DPointFromPixel(int x, int y)
        {
            throw new NotImplementedException();
        }

        public ColorDepthImage GetImage()
        {
            if (Status != CameraStatus.Running)
                throw new Exception("The camera must be running before images can be captured.");

            const int WIDTH = 640;
            const int HEIGHT = 480;

            Texture2D color = new Texture2D(WIDTH, HEIGHT);
            Texture2D depth = new Texture2D(WIDTH, HEIGHT);

            for (int i = 0; i < WIDTH; i++)
                for (int j = 0; j < HEIGHT; j++)
                {
                    color.SetPixel(i, j, Color.black);
                    depth.SetPixel(i, j, Color.black);
                }


            int n = UnityEngine.Random.Range(0, WIDTH - 150);

                    for (int i = n; i < n + 100; i++)
            {
                for (int j = n; j < n + 100; j++)
                {
                    if(n > 300)
                        color.SetPixel(i, j, Color.blue);
                    else
                        color.SetPixel(i, j, Color.red);
                    depth.SetPixel(i, j, Color.white);
                }
            }

            return new ColorDepthImage(color, depth);
        }

        public void SetImage(ColorDepthImage image)
        {
            throw new NotImplementedException();
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
    }
}