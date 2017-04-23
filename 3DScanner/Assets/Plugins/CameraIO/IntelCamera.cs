using Intel.RealSense;
using System;
using System.Linq;
using UnityEngine;
using UnityScanner3D.ComputerVision;

namespace UnityScanner3D.CameraIO
{
    public class IntelCamera : ICamera
    {
        private const int WIDTH = 640;
        private const int HEIGHT = 480;
        private const float FPS = 30.0f;

        private Projection imageProj = null;
        Point3DF32[] depthArray = null;
        Point3DF32[] pointArray = null;

        ~IntelCamera()
        {
            if(SampleStream != null)
                SampleStream.Dispose();
            if(SMInstance != null)
                SMInstance.Dispose();
        }

        public Vector3 Get3DPointFromPixel(int x, int y)
        {

            if (depthArray == null || pointArray == null)
                throw new InvalidOperationException("SetImage must be called before a 3D point can be retrieved");

            return pointArray[y * WIDTH + x];
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

            //Extract and map the depth image
            Image initialDepthImage = SampleStream.Sample.Depth;
            Projection proj = SMInstance.CaptureManager.Device.CreateProjection();
            Image depthImage = proj.CreateDepthImageMappedToColor(initialDepthImage, colorImage);
            ImageData depthImageData;
            depthImage.AcquireAccess(ImageAccess.ACCESS_READ, PixelFormat.PIXEL_FORMAT_RGB32, out depthImageData);
            depthImageData.ToTexture2D(0, depthTex);

            //Clean up resources
            colorImage.Dispose();
            depthImage.Dispose();
            initialDepthImage.Dispose();
            proj.Dispose();
            colorImage.ReleaseAccess(colorImageData);
            depthImage.ReleaseAccess(depthImageData);

            //Release the acquired frame
            SMInstance.ReleaseFrame();

            return new ColorDepthImage(colorTex, depthTex);
        }

        public void SetImage(ColorDepthImage image)
        {
            const float MM_UNITY_CONV = 1.0f;
            
            //Creates the projection and array to populate
            imageProj = SMInstance.CaptureManager.Device.CreateProjection();
            depthArray = new Point3DF32[image.DepthImage.height * image.DepthImage.width];

            //Iterates through each of the pixels and places them in the appropriate array
            var dImageArray = image.DepthImage.GetPixels();
            for (int i = 0; i < depthArray.Length; i++)
            {
                depthArray[i].x = i % WIDTH;
                depthArray[i].y = i / WIDTH;
                depthArray[i].z = dImageArray[i].grayscale;
            }

            //Performs the conversion
            pointArray = new Point3DF32[WIDTH * HEIGHT];
            imageProj.ProjectDepthToCamera(depthArray, pointArray);

            //Scales the units
            for(int i = 0; i < pointArray.Length; i++)
            {
                pointArray[i].x *= MM_UNITY_CONV;
                pointArray[i].y *= MM_UNITY_CONV;
                pointArray[i].z *= MM_UNITY_CONV;
            }

            //Releases resources
            imageProj.Dispose();
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
            SampleStream.EnableStream(StreamType.STREAM_TYPE_COLOR, WIDTH, HEIGHT, FPS, StreamOption.STREAM_OPTION_STRONG_STREAM_SYNC);
            SampleStream.EnableStream(StreamType.STREAM_TYPE_DEPTH, WIDTH, HEIGHT, FPS, StreamOption.STREAM_OPTION_STRONG_STREAM_SYNC);
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