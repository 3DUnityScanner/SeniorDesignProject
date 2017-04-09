using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityScanner3D.CameraIO;

using Point = UnityEngine.Vector3;

namespace UnityScanner3D.ComputerVision
{
    public class ColorTrackingAlgorithm : IAlgorithm
    {
        private const float COLOR_THRESHOLD = 1.0f;

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
                Point averagePoint = AveragePoint(clump);

                //Return shape at the given point
                yield return new Shape()
                {
                    Type = ShapeType.Cube,
                    Translation = averagePoint,
                    Rotation = new Quaternion(0, 0, 0, 0)
                };
            }
        }

        public void ProcessImage(ColorDepthImage image)
        {
            //Save Images
            File.WriteAllBytes("color.jpg", image.ColorImage.EncodeToJPG());
            File.WriteAllBytes("depth.jpg", image.DepthImage.EncodeToJPG());
            File.WriteAllBytes("color.png", image.ColorImage.EncodeToPNG());
            File.WriteAllBytes("depth.png", image.DepthImage.EncodeToPNG());

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
                    if(AreColorsDifferent(thisColor, averageColor))
                    {
                        //Perform flood fill
                        List<Point> clump = FloodFill(image, x, y, averageColor);

                        //Save clump
                        clumpQueue.Enqueue(clump);
                    }
                }
            }
        }

        private bool AreColorsDifferent(Color u, Color v)
        {
            return ColorDifference(u, v) > COLOR_THRESHOLD;
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

        private List<Point> FloodFill(ColorDepthImage image, int x, int y, Color stopColor)
        {
            //Gets the color of the current pixel
            Color currentPixelColor = image.ColorImage.GetPixel(x, y);

            //Stop if the current color is the stop color
            if (!AreColorsDifferent(currentPixelColor, stopColor))
                return null;

            List<Point> toRet = new List<Point>();
            Texture2D colorImage = image.ColorImage;
            int leftBound = x - 1;
            int rightBound = x + 1;

            //Get all pixels to the left which aren't the stop color
            while(AreColorsDifferent(colorImage.GetPixel(leftBound, y), stopColor))
            {
                colorImage.SetPixel(leftBound, y, stopColor);
                toRet.Add(new Point(leftBound, y, image.DepthImage.GetPixel(leftBound, y).grayscale));
                leftBound--;
            }

            //Get all pixels to the right which aren't the stop color
            while(AreColorsDifferent(colorImage.GetPixel(rightBound, y), stopColor))
            {
                colorImage.SetPixel(rightBound, y, stopColor);
                toRet.Add(new Point(rightBound, y, image.DepthImage.GetPixel(rightBound, y).grayscale));
                rightBound++;
            }

            //Perform same operation on the pixels above and below
            for (int i = leftBound + 1; i < rightBound; i++)
            {
                toRet.AddRange(FloodFill(image, i, y - 1, stopColor) ?? Enumerable.Empty<Point>());
                toRet.AddRange(FloodFill(image, i, y + 1, stopColor) ?? Enumerable.Empty<Point>());
            }

            //Return the results
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