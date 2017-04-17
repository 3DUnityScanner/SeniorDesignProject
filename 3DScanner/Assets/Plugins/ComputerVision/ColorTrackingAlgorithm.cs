using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityScanner3D.CameraIO;

using Point = UnityEngine.Vector3;

namespace UnityScanner3D.ComputerVision
{
    public class ColorTrackingAlgorithm : IAlgorithm
    {
        public const float STDEV = 2.0f;
        public const int CLUMPTHRESH = 10;
        private struct Pixel
        {
            public Pixel(int x, int y)
            {
                X = x;
                Y = y;
            }

            public int X { get; private set; }
            public int Y { get; private set; }
        }

        private const float DIFFERENCE_THRESHOLD = 0.4f;

        public void ClearShapes()
        {
            clumpQueue.Clear();
        }

        public IEnumerable<Shape> GetShapes()
        {

            while (clumpQueue.Count > 0)
            {
                //Calculate average position
                List<Point> clump = clumpQueue.Dequeue();
                //check for little clumps to ignore
                if (clump.Count >= CLUMPTHRESH)
                {
                        Point averagePoint = AveragePoint(clump);
                
                    //set all poses to lie on the ground (y = 0.5)
                    averagePoint.y = 0.5f;

                    //Return shape at the given point
                    yield return new Shape()
                    {
                        Type = ShapeType.Cube,
                        Translation = averagePoint,
                        Rotation = new Quaternion(0, 0, 0, 0)
                    };
                }
            }
        }

        public void ProcessImage(ColorDepthImage image)
        {
            Texture2D colorImage = image.ColorImage;
            int colorHeight = colorImage.height;
            int colorWidth = colorImage.width;

            //Calculate the initial average color
            Color averageColor = CalculateAverageColor(image.ColorImage);

            //Calculate the standard deviation of color
            float stdDev = CalculateStandardColorDeviation(image.ColorImage, averageColor);

            //Refine average color
            averageColor = CalculateAverageColor(colorImage, averageColor, stdDev, STDEV);

            //Maximize contrast in image
            for(int y = 0; y < colorHeight; y++)
            {
                for(int x = 0; x < colorWidth; x++)
                {
                    Color currColor = colorImage.GetPixel(x, y);
                    Color newColor = !AreColorsDifferent(averageColor, currColor) ? Color.white : Color.black;
                    colorImage.SetPixel(x, y, newColor);
                }
            }

            //Save image
            File.WriteAllBytes("contrast.png", colorImage.EncodeToPNG());
            
            //Identify clumps
            for(int y = 0; y < colorHeight; y++)
            {
                for(int x = 0; x < colorWidth; x++)
                {
                    //Checks if the difference in color is within the threshold
                    Color thisColor = colorImage.GetPixel(x, y);
                    if(AreColorsDifferent(thisColor, Color.white))
                    {
                        //Perform flood fill
                        List<Point> clump = FloodFill(image, x, y, Color.white);

                        //Save clump
                        clumpQueue.Enqueue(clump);
                    }
                }
            }
        }

        private bool AreColorsDifferent(Color u, Color v)
        {
            return ColorDifference(u, v) > DIFFERENCE_THRESHOLD;
        }

        private Color CalculateAverageColor(Texture2D colorImage)
        {
            int colorWidth = colorImage.width;
            int colorHeight = colorImage.height;
            float numOfPixels = colorHeight * colorWidth;

            //Sum total of each of the color channels
            float averageRed = 0, averageGreen = 0, averageBlue = 0;
            for (int y = 0; y < colorImage.height; y++)
            {
                for (int x = 0; x < colorImage.width; x++)
                {
                    averageRed += colorImage.GetPixel(x, y).r;
                    averageGreen += colorImage.GetPixel(x, y).g;
                    averageBlue += colorImage.GetPixel(x, y).b;
                }
            }

            //Calculates average color
            averageRed /= numOfPixels;
            averageGreen /= numOfPixels;
            averageBlue /= numOfPixels;
            return new Color(averageRed, averageGreen, averageBlue);
        }

        private Color CalculateAverageColor(Texture2D colorImage, Color oldAverage, float stdDev, float range)
        {
            float r = 0.0f;
            float g = 0.0f;
            float b = 0.0f;

            int numPixels = 0;

            for (int y = 0; y < colorImage.height; y++)
            {
                for (int x = 0; x < colorImage.width; x++)
                {
                    Color thisColor = colorImage.GetPixel(x, y);
                    if (ColorDifference(oldAverage, thisColor) < range * stdDev)
                    {
                        r += thisColor.r;
                        g += thisColor.g;
                        b += thisColor.b;
                        numPixels++;
                    }
                }
            }

            r /= numPixels;
            g /= numPixels;
            b /= numPixels;

            return new Color(r, g, b);
        }

        private float CalculateStandardColorDeviation(Texture2D colorImage, Color average)
        {
            int colorHeight = colorImage.height;
            int colorWidth = colorImage.width;
            int numOfPixels = colorHeight * colorWidth;

            //Calculate the standard deviation
            double stdDev = 0;
            for (int y = 0; y < colorHeight; y++)
            {
                for (int x = 0; x < colorWidth; x++)
                {
                    //Get the color of the pixel
                    Color currentColor = colorImage.GetPixel(x, y);

                    //Calculate the difference
                    float diff = ColorDifference(currentColor, average);

                    //Update the stdDev
                    stdDev += Math.Pow(diff, 2);
                }
            }

            stdDev /= numOfPixels;
            return (float) stdDev;
        }

        private float ColorDifference(Color u, Color v)
        {
            //Calculates the squared difference of each component
            double r = Math.Pow(u.r - v.r, 2.0);
            double g = Math.Pow(u.g - v.g, 2.0);
            double b = Math.Pow(u.b - v.b, 2.0);

            //Returns the square root
            float distance = (float)Math.Sqrt(r + g + b);
            return distance;
        }

        private List<Point> FloodFill(ColorDepthImage image, int x, int y, Color stopColor)
        {
            //Images
            Texture2D color = image.ColorImage;
            Texture2D depth = image.DepthImage;

            //Collections
            List<Point> toRet = new List<Point>();
            Queue<Pixel> pixelQueue = new Queue<Pixel>();
            pixelQueue.Enqueue(new Pixel(x, y));

            do
            {
                //Get the pixel out of the queue
                Pixel p = pixelQueue.Dequeue();

                //Check if the current pixel is the stop color
                if (AreColorsDifferent(color.GetPixel(p.X, p.Y), stopColor))
                {
                    //Add the current pixel as a point to return
                    float Z = depth.GetPixel(p.X, p.Y).grayscale;
                    toRet.Add(new Point(p.X, p.Y, Z));

                    //Mark it as visited
                    color.SetPixel(p.X, p.Y, stopColor);

                    Pixel left = new Pixel(p.X - 1, p.Y);
                    Pixel right = new Pixel(p.X + 1, p.Y);
                    Pixel up = new Pixel(p.X, p.Y - 1);
                    Pixel down = new Pixel(p.X, p.Y + 1);

                    //Add its neighbors to the queue of pixels to be processed
                    if (p.X - 1 >= 0 && !pixelQueue.Contains(left))
                        pixelQueue.Enqueue(left);

                    if (p.X + 1 < color.width && !pixelQueue.Contains(right))
                        pixelQueue.Enqueue(right);

                    if (p.Y - 1 >= 0 && !pixelQueue.Contains(up))
                        pixelQueue.Enqueue(up);

                    if (p.Y + 1 < color.height && !pixelQueue.Contains(down))
                        pixelQueue.Enqueue(down);
                }

            } while (pixelQueue.Count > 0);

            return toRet;
        }

        private Point AveragePoint(List<Point> points)
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

            averageX /= points.Count;
            averageY /= points.Count;
            averageZ /= points.Count;

            return new Point(averageX, averageY, averageZ);
        }

        private Queue<List<Point>> clumpQueue = new Queue<List<Point>>();
    }
}