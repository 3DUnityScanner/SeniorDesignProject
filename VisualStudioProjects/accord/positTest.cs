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
        String input = "../../block.jpg";//test image
        String outputHough = "../../hough.jpg";//output of hough transform
        String outputSURF = "../../surf.jpg";//output of surf features
        String outputHarris = "../../harris.jpg";//output of Harris corners        
        String outputCombo = "../../combo.jpg";//surf and hough
        String outputCombo2 = "../../all.jpg";//all

        /**
         *  do hough transform
         **/
        public void houghTrans()
        {//TODO: hough circles for cylinder detection
            Bitmap image, grayImage;

            //System.Drawing image
            try
            {
                image = (Bitmap)System.Drawing.Image.FromFile(input);
                grayImage = Grayscale.CommonAlgorithms.BT709.Apply(image);//converts to bpp grayscale for the hough transform
            }
            catch (System.IO.FileNotFoundException e)
            { throw new System.IO.FileNotFoundException("file not found"); }

            UnmanagedImage umImage = UnmanagedImage.FromManagedImage(image);//convert to unmanaged for compatibility

            //Hough Line Transformation
            var hough = new HoughLineTransformation();
            hough.ProcessImage(grayImage);
            Bitmap houghImg = hough.ToBitmap();
            System.Console.WriteLine("lines: " + hough.LinesCount);
            System.Console.WriteLine("local peaks radius: " + hough.LocalPeakRadius);
            System.Console.WriteLine("max intensity: " + hough.MaxIntensity);
            System.Console.WriteLine("min: " + hough.MinLineIntensity);
            System.Console.ReadLine();

            //HoughLine[] lines = hough.GetLinesByRelativeIntensity(0.7);//gets all lines with an intensity above the threshold
            HoughLine[] lines = hough.GetMostIntensiveLines(40);//gets the n most intensive lines in the set

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
            umImage.ToManagedImage().Save(outputHough);
        }

        /** 
         * harris corner detector
         **/
        public List<Accord.IntPoint> doHarris()
        {
            Bitmap image, combo;
            try
            {
                image = (Bitmap)System.Drawing.Image.FromFile(input);
                combo = (Bitmap)System.Drawing.Image.FromFile(outputCombo);

            }
            catch (System.IO.FileNotFoundException e)
            { throw new System.IO.FileNotFoundException("file not found"); }

            HarrisCornersDetector harrisDetect = new HarrisCornersDetector();
            List < Accord.IntPoint > corners = harrisDetect.ProcessImage(image);
            PointsMarker points = new PointsMarker(corners);
            points.MarkerColor = Color.Black; points.Width = 4;
            Bitmap cornerImg = points.Apply(image);
            cornerImg.Save(outputHarris);
            Bitmap comboImg = points.Apply(combo);
            comboImg.Save(outputCombo2);

            return corners;
        }

        /**
         * surf feature detector
         **/
        public List<SpeededUpRobustFeaturePoint> doSurf()
        {
            Bitmap image, hough;
            try
            {
                image = (Bitmap)System.Drawing.Image.FromFile(input);
                hough = (Bitmap)System.Drawing.Image.FromFile(outputHough);

            }
            catch (System.IO.FileNotFoundException e)
            { throw new System.IO.FileNotFoundException("file not found"); }
            //default vals, TODO: find how parameters affect features
            float thresh = (float)0.000200;
            int octaves = 5;
            int initial = 2;

            SpeededUpRobustFeaturesDetector surf = new SpeededUpRobustFeaturesDetector(thresh, octaves, initial);
            List<SpeededUpRobustFeaturePoint> points = surf.ProcessImage(image);

            //SURF feature set
            FeaturesMarker features = new FeaturesMarker(points);
            //just SURF features
            Bitmap edited1 = features.Apply(image);
            edited1.Save(outputSURF);
            //Hough and SURF
            Bitmap edited2 = features.Apply(hough);
            edited2.Save(outputCombo);

            return points;
        }


        /** 
         * Combination of techniques to get interest points for POSIT
         **/
        public List<Accord.Point> getPoints()
        {
            List<Accord.Point> interestPoints = new List<Accord.Point>(); 
            List<Accord.IntPoint> cornerPoints = doHarris();
            List <SpeededUpRobustFeaturePoint> surfPoints = doSurf();
            int index=0;

            for (int x = 0; x < surfPoints.Count; x++) { System.Console.WriteLine(surfPoints[x].Response+"\n"); }

            double thresh = 2.5;//SURF response threshold

            //for every harris point
            for (int i=0; i<cornerPoints.ToArray().Length; i++)
            {
                //check for surf features near it w/ a respoinse above a certain threshold
                for (int j = 0; j < surfPoints.ToArray().Length; j++)
                {
                    if (Math.Abs(cornerPoints[i].X - surfPoints[j].X) <= 8 && Math.Abs(cornerPoints[i].Y - surfPoints[j].Y) <= 8)
                    {
                        if (surfPoints[j].Scale >= thresh)//should be response but i'm getting zeros for whatever reason
                        {
                            interestPoints.Insert(index,cornerPoints[i]); index++;
                        }
                    }
                }
            }

            return interestPoints;
        }

        /** 
         * basic posit estimation
         **/
        public float[] runPosit(Vector3[] model, float fl)
        {//TODO: account for other object types/scales
            //POSIT object
            Posit posit = new Posit(model, fl);

            List<Accord.Point> interestPoints = getPoints();
            if (interestPoints == null)
            {
                System.Console.WriteLine("ERR (POSIT): no points found");
                System.Console.ReadLine();
                return null;
            }
            List<Accord.Point> positPoints;int[] indexes = {0,1,2,3};
            positPoints = interestPoints.Get(indexes);//get the first four since posit only does four points... 

            //estimating pose
            Matrix3x3 rotation;
            Vector3 translation;
            posit.EstimatePose(positPoints.ToArray(), out rotation, out translation);
            System.Console.WriteLine("posit rotation:" + rotation.V00+","+rotation.V01 + "," + rotation.V02 + ",\n" 
                + rotation.V10 + "," + rotation.V11 + "," + rotation.V12 + ",\n" 
                + rotation.V20 + "," + rotation.V21 + "," + rotation.V22);
            System.Console.WriteLine("posit translation:" + translation);
            System.Console.ReadLine();
            //will return list of doubles including rotation vals and translation vals length and object type[13] 
            return rotation.ToArray().Concatenate(translation.ToArray().Concatenate(0));
        }
    }
}
