using Intel.RealSense;
using System;

namespace UnityScanner3D.CameraIO
{
    public class IntelCamera : ICamera
    {
        ~IntelCamera()
        {
            if(SampleStream != null)
                SampleStream.Dispose();
            if(SMInstance != null)
                SMInstance.Dispose();
        }

        public ColorDepthImage GetImage()
        {
            if (Status == CameraStatus.Stopped)
                throw new InvalidOperationException("The camera cannot produce data unless it is running");

            if (SMInstance.AcquireFrame(true).IsError())
                throw new Exception("Unable to captuer frame");

            ColorDepthImage toRet = new ColorDepthImage();

            //Extract the color image
            Image colorImage = SampleStream.Sample.Color;
            ImageData colorImageData;
            colorImage.AcquireAccess(ImageAccess.ACCESS_READ, out colorImageData);
            colorImageData.ToIntArray(0, toRet.ColorImage);

            //Extract the depth image
            Image depthImage = SampleStream.Sample.Depth;
            ImageData depthImageData;
            depthImage.AcquireAccess(ImageAccess.ACCESS_READ, out depthImageData);
            depthImageData.ToIntArray(0, toRet.DepthImage);

            //Clean up image data
            colorImage.Dispose();
            depthImage.Dispose();
    
            return toRet;
        }

        public void StartCapture()
        {
            if (Status != CameraStatus.Stopped)
                throw new InvalidOperationException("A camera cannot be started if it is already running");

            Status = CameraStatus.Running;

            //Initialize the sense manager and streams
            SampleStream = SampleReader.Activate(SMInstance);
            SampleStream.EnableStream(StreamType.STREAM_TYPE_COLOR);
            SampleStream.EnableStream(StreamType.STREAM_TYPE_DEPTH);
            SMInstance.Init();
        }

        public void StopCapture()
        {
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