using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityScanner3D.CameraIO;

using Point = UnityEngine.Vector3;

namespace UnityScanner3D.ComputerVision
{
    public class ColorTrackingAlgorithm : IAlgorithm
    {
        private const float COLOR_THRESHHOLD = 1.0f;

        public void ClearShapes()
        {
            
        }

        public IEnumerable<Shape> GetShapes()
        {
            while (clumpQueue.Count > 0)
            {
                //Calculate average position
                IEnumerable<Point> clump = clumpQueue.Dequeue();
                Point averagePoint = AveragePoint(clump);

                //Return shape at the given point
                yield return new Shape()
                {
                    Type = ShapeType.Cube,
                    Translation = averagePoint
                };
            }
        }

        public void ProcessImage(ColorDepthImage image)
        {
            //The color image to be used for processing
            Texture2D colorImage = image.ColorImage;
            int colorWidth = colorImage.width;
            int colorHeight = colorImage.height;

            //Sum total of each of the color channels
            float averageRed = 0, averageGreen = 0, averageBlue = 0;
            for (int y = 0; y < colorImage.width; y++)
            {
                for (int x = 0; x < colorImage.height; x++)
                {
                    averageRed += colorImage.GetPixel(x, y).r;
                    averageGreen += colorImage.GetPixel(x, y).g;
                    averageBlue += colorImage.GetPixel(x, y).b;
                }
            }

            //Calculates average color
            float numOfPixels = colorImage.height * colorImage.width;
            averageRed /= numOfPixels;
            averageGreen /= numOfPixels;
            averageBlue /= numOfPixels;
            Color averageColor = new Color(averageRed, averageGreen, averageBlue);

            //TODO: Refine average color by removing outliers

            //Identify clumps
            for(int y = 0; y < colorHeight; y++)
            {
                for(int x = 0; x < colorWidth; x++)
                {
                    //Checks if the difference in color is within the threshold
                    Color thisColor = colorImage.GetPixel(x, y);
                    if(ColorDifference(thisColor, averageColor) > COLOR_THRESHHOLD)
                    {
                        //Perform flood fill
                        IEnumerable<Point> clump = FloodFill(image, x, y, averageColor);

                        //Save clump
                        clumpQueue.Enqueue(clump);
                    }
                }
            }
        }

        private float ColorDifference(Color u, Color v)
        {
            //Calculates the squared difference of each component
            double r = Math.Pow(u.r - v.r, 2.0);
            double g = Math.Pow(u.g - v.g, 2.0);
            double b = Math.Pow(u.b - v.b, 2.0);

            //Returns the square root
            return (float)Math.Sqrt(r + g + b);
        }

        private IEnumerable<Point> FloodFill(ColorDepthImage image, int x, int y, Color stopColor)
        {
            //Gets the color of the current pixel
            Color currentColor = image.ColorImage.GetPixel(x, y);

            //Stop on current color
            if (ColorDifference(currentColor, stopColor) < COLOR_THRESHHOLD)
                yield break;

            Texture2D colorImage = image.ColorImage;
            int leftBound = x - 1;
            int rightBound = x + 1;

            //Get all acceptable pixels to the left
            while(ColorDifference(colorImage.GetPixel(leftBound, y), stopColor) > COLOR_THRESHHOLD)
            {
                yield return new Point(leftBound, y, image.DepthImage.GetPixel(leftBound, y).grayscale);
                leftBound--;
            }

            //Get all acceptable pixels to the right
            while(ColorDifference(colorImage.GetPixel(rightBound, y), stopColor) > COLOR_THRESHHOLD)
            {
                yield return new Point(rightBound, y, image.DepthImage.GetPixel(rightBound, y).grayscale);
                rightBound++;
            }

            //Perform same operation above
            IEnumerable<Point> above = Enumerable.Empty<Point>();
            for (int i = leftBound + 1; i < rightBound; i++)
                above.Concat(FloodFill(image, i, y - 1, stopColor));

            //Perform same operation below
            IEnumerable<Point> below = Enumerable.Empty<Point>();
            for (int i = leftBound; i <= rightBound; i++)
                above.Concat(FloodFill(image, i, y + 1, stopColor));

            //Return the points from above
            foreach (var p in above)
                yield return p;

            //Return the points from below
            foreach (var p in below)
                yield return p;
        }

        private Point AveragePoint(IEnumerable<Point> points)
        {
            float averageX = 0;
            float averageY = 0;
            float averageZ = 0;

            foreach(Point p in points)
            {
                averageX += p.x;
                averageY += p.y;
                averageZ += p.z;
            }

            int count = points.Count();
            averageX /= count;
            averageY /= count;
            averageZ /= count;

            return new Point(averageX, averageY, averageZ);
        }

        private Queue<IEnumerable<Point>> clumpQueue = new Queue<IEnumerable<Point>>();
    }
}