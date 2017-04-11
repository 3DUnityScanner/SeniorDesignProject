using Intel.RealSense;
using System;
using System.IO;
using UnityEngine;

namespace UnityScanner3D.CameraIO
{
    public class IntelCamera : ICamera
    {
        private const int WIDTH = 640;
        private const int HEIGHT = 480;
        private const float FPS = 30.0f;

        ~IntelCamera()
        {
            if(SampleStream != null)
                SampleStream.Dispose();
            if(SMInstance != null)
                SMInstance.Dispose();
        }

        public ColorDepthImage GetImage()
        {
            //Checks to make sure that the camera has been properly initialized
            if (Status == CameraStatus.Stopped)
                throw new InvalidOperationException("The camera cannot produce data unless it is running");

            //Acquires the frame
            Status acquisitionResult = SMInstance.AcquireFrame(true);
            if (acquisitionResult.IsError())
                throw new Exception("Unable to capture frame: " + acquisitionResult);

            Texture2D colorTex = new Texture2D(WIDTH, HEIGHT, TextureFormat.RGBA32, false);
            Texture2D depthTex = new Texture2D(WIDTH, HEIGHT, TextureFormat.RGBA32, false);

            //Extract the color image
            Image colorImage = SampleStream.Sample.Color;
            ImageData colorImageData;
            colorImage.AcquireAccess(ImageAccess.ACCESS_READ, PixelFormat.PIXEL_FORMAT_RGB32, out colorImageData);
            colorImageData.ToTexture2D(0, colorTex);

            //Extract the depth image
            Image depthImage = SampleStream.Sample.Depth;
            ImageData depthImageData;
            depthImage.AcquireAccess(ImageAccess.ACCESS_READ, PixelFormat.PIXEL_FORMAT_RGB32, out depthImageData);
            depthImageData.ToTexture2D(0, depthTex);

            //Clean up image data
            colorImage.Dispose();
            depthImage.Dispose();

            //Release the acquired frame
            SMInstance.ReleaseFrame();

            //Save the images
            File.WriteAllBytes("color.png", colorTex.EncodeToPNG());
            File.WriteAllBytes("depth.png", depthTex.EncodeToPNG());
    
            return new ColorDepthImage(colorTex, depthTex);
        }

        public void StartCapture()
        {
            //Checks to make sure that the camera has not already been started
            if (Status != CameraStatus.Stopped)
                throw new InvalidOperationException("A camera cannot be started if it is already running");

            //Updates the status
            Status = CameraStatus.Running;

            //Initialize the sense manager and streams
            SampleStream = SampleReader.Activate(SMInstance);
            SampleStream.EnableStream(StreamType.STREAM_TYPE_COLOR, WIDTH, HEIGHT, FPS);
            SampleStream.EnableStream(StreamType.STREAM_TYPE_DEPTH, WIDTH, HEIGHT, FPS);
            SMInstance.Init();
        }

        public void StopCapture()
        {
            //Checks to make sure that the camera has not already been stopped before starting it again
            if (Status != CameraStatus.Running)
                throw new InvalidOperationException("A camera cannot be stopped if it is not running");
            
            //Updates the status
            Status = CameraStatus.Stopped;

            //Dispose of the created stream
            SampleStream.Dispose();
            SampleStream = null;

            //Close the sense manager
            SMInstance.Close();
        }

        private SenseManager SMInstance = SenseManager.CreateInstance();
        private SampleReader SampleStream;
        private CameraStatus Status = CameraStatus.Stopped;
    }
}