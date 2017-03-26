﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityScanner3D.CameraIO;

namespace UnityScanner3D.CameraIO
{
    class DummyCamera : ICamera
    {
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
                    color.SetPixel(i, j, isBlack ? Color.black : Color.white);

            isBlack = !isBlack;
            return new ColorDepthImage(color, depth);
        }

        public void StartCapture()
        {
            if (Status == CameraStatus.Running)
                throw new Exception("A camera can't be started if it is already running.");
        }

        public void StopCapture()
        {
            if (Status == CameraStatus.Stopped)
                throw new Exception("A camera can't be stopped if it is already stopped.");

        }

        public CameraStatus Status { get; private set; }


        private bool isBlack = true;
    }
}