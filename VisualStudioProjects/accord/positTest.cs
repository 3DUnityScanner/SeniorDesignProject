using System;
using System.Drawing;
using Accord.Imaging;
using Accord.Vision;
using Accord.Math.Geometry;
using Accord.Math;
using Accord.Vision.Detection;
using Accord.Imaging.Filters;
using System.Drawing.Imaging;
using System.Collections.Generic;

namespace accord
{

    class PositTest
    {
        //surf feature detector
        public void doSurf()
        {
            Bitmap image;
            try
            {
                image = (Bitmap)System.Drawing.Image.FromFile("../../block.jpg");
            }
            catch (System.IO.FileNotFoundException e)
            { throw new System.IO.FileNotFoundException("file not found"); }
            //default vals
            float thresh = (float)0.000200;
            int octaves = 5;
            int initial = 2;

            SpeededUpRobustFeaturesDetector surf = new SpeededUpRobustFeaturesDetector(thresh, octaves, initial);
            List<SpeededUpRobustFeaturePoint> points = surf.ProcessImage(image);

            Bitmap edited = image;
            FeaturesMarker features = new FeaturesMarker(points);
            features.Apply(edited);
            edited.Save("../../surf.jpg");
        }

        //do hough transform
        public void hughTrans()
        {
            //Console.WriteLine(System.IO.Directory.GetCurrentDirectory());Console.ReadLine();
            Bitmap image, grayImage;
            //System.Drawing image
            try
            {
                image = (Bitmap)System.Drawing.Image.FromFile("../../block.jpg");
                grayImage = Grayscale.CommonAlgorithms.BT709.Apply(image);//converts to bpp grayscale for the hough transform
            }
            catch (System.IO.FileNotFoundException e)
            { throw new System.IO.FileNotFoundException("file not found"); }

            UnmanagedImage umImage = UnmanagedImage.FromManagedImage(image);//convert to unmanaged for compatibility

            //Hough Line Transformation
            var hough = new HoughLineTransformation();
            hough.ProcessImage(grayImage);
            Bitmap houghImg = hough.ToBitmap();
            BitmapData bmpData = houghImg.LockBits(new Rectangle(0, 0, houghImg.Width, houghImg.Height), ImageLockMode.ReadWrite, houghImg.PixelFormat);//bmp data for drawing lines
            System.Console.WriteLine("lines: " + hough.LinesCount);
            System.Console.WriteLine("local peaks radius: " + hough.LocalPeakRadius);
            System.Console.WriteLine("max intensity: " + hough.MaxIntensity);
            System.Console.WriteLine("min: " + hough.MinLineIntensity);
            System.Console.ReadLine();

            //HoughLine[] lines = hough.GetLinesByRelativeIntensity(0.98);
            HoughLine[] lines = hough.GetMostIntensiveLines(40);
            foreach (HoughLine line in lines) //draw lines
            {
                int radius = line.Radius;
                double theta = line.Theta;
                if (radius < 0) // if the line is in the lower part of the image do polar coord adjustment
                {
                    theta += 180;
                    radius = -radius;
                }
                theta = (theta / 180) * Math.PI;//degrees to rads
                //image center
                int w = image.Width / 2;
                int h = image.Height / 2;
                double x0 = 0, x1 = 1, y0 = 0, y1 = 0;

                if (line.Theta != 0)
                {
                    //not vert
                    x0 = -w;//left
                    x1 = w;//right
                    y0 = (-Math.Cos(theta) * x0 + radius) / Math.Sin(theta);
                    y1 = (-Math.Cos(theta) * x1 + radius) / Math.Sin(theta);
                }
                else
                {
                    // vert
                    x0 = line.Radius;
                    x1 = line.Radius;

                    y0 = h;
                    y1 = -h;
                }
                Drawing.Line(umImage, new Accord.IntPoint((int)x0 + w, h - (int)y0), new Accord.IntPoint((int)x1 + w, h - (int)y1), Color.Red);
            }
            umImage.ToManagedImage().Save("../../hough.jpg");
        }

        //basic posit estimation
        public void runPosit(Vector3[] model, float fl)
        {
            //POSIT object
            Posit posit = new Posit(model, fl);

            //projection points of the cube (this is what we want to generate)
            Accord.Point[] projectedPoints = new Accord.Point[4]
            {
            new Accord.Point(-4,29),
            new Accord.Point(-180,86),
            new Accord.Point(-5,-102),
            new Accord.Point(76,137),
            };

            //estimating pose
            Matrix3x3 rotation;
            Vector3 translation;
            posit.EstimatePose(projectedPoints, out rotation, out translation);
            System.Console.WriteLine("posit rotation:" + rotation.V00+","+rotation.V01 + "," + rotation.V02 + ",\n" 
                + rotation.V10 + "," + rotation.V11 + "," + rotation.V12 + ",\n" 
                + rotation.V20 + "," + rotation.V21 + "," + rotation.V22);
            System.Console.WriteLine("posit translation:" + translation);
            System.Console.ReadLine();
        }
    }
}
