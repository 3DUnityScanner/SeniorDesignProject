using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityScanner3D.CameraIO;

namespace UnityScanner3D.ComputerVision
{
    public class ColorTrackingAlgorithm : IAlgorithm
    {
        public const int OBJSCALE = 70;//scale of spawned GameObjects
        public const float STDEV = 2.0f;//standard deviation for
        public const int CLUMPTHRESH = 1700;//minimum size of clumps
        private const float DIFFERENCE_THRESHOLD = 0.3f;//threshold for color difference

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

        public void ClearShapes()
        {
            clumpQueue.Clear();
        }

        public IEnumerable<Shape> GetShapes()
        {
            float angle = Vector3.Angle(normalVector, new Vector3(0, 0, -1));
            while (clumpQueue.Count > 0)
            {
                //Calculate average position
                List<Vector3> clump = clumpQueue.Dequeue();
                //check for little clumps to ignore
                if (clump.Count >= CLUMPTHRESH)
                {
                    Vector3 averagePoint = ConvertCoordinates(AveragePoint(clump), angle);
                
                    //set all poses to lie on the ground (y = 0.5) times the scale
                    averagePoint.y = OBJSCALE*0.5f;

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
            //Calculate the initial average color and standard deviation
            Color averageColor = CalculateAverageColor(image.ColorImage);
            float stdDev = CalculateStandardColorDeviation(image.ColorImage, averageColor);

            //Refine average color
            averageColor = CalculateAverageColor(image.ColorImage, averageColor, stdDev, STDEV);

            //Maximize contrast in image and save the new image
            Contrastify(image.ColorImage, averageColor);
            File.WriteAllBytes("contrast.png", image.ColorImage.EncodeToPNG());

            //Identify clumps
            normalVector = CalculateTableNormal(image);
            Clumpify(image);
        }

        public Texture2D PreviewImage(ColorDepthImage image)
        {
            Texture2D workingCopy = Texture2D.Instantiate(image.ColorImage);
            var averageColor = CalculateAverageColor(workingCopy);
            //var stdDev = CalculateStandardColorDeviation(workingCopy, averageColor);
            //averageColor = CalculateAverageColor(workingCopy, averageColor, stdDev, STDEV);
            Contrastify(workingCopy, averageColor);
            return workingCopy;
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

        private Vector3 CalculateTableNormal(ColorDepthImage image)
        {
            //Calculates the normal vector of the table
            Vector3 toRet;
            float averageX = 0;
            float averageY = 0;
            float averageZ = 0;

            //Picks an arbitrary starting point
            Pixel tail = GetTablePixel(image.ColorImage);

            //Constructs vectors
            List<Vector3> vectors = new List<Vector3>(10);
            for (int i = 0; i < 10; i++)
            {
                Pixel head = GetTablePixel(image.ColorImage);
                float headZ = image.DepthImage.GetPixel(head.X, head.Y).grayscale;
                float tailZ = image.DepthImage.GetPixel(tail.X, tail.Y).grayscale;

                vectors.Add(new Vector3(
                    (head.X - tail.X),
                    (head.Y - tail.Y),
                    headZ - tailZ));
                
            }

            Vector3 camView = new Vector3(0, 0, 1);
            foreach(Vector3 u in vectors)
            {
                foreach(Vector3 v in vectors)
                {
                    //Calculates the normal
                    Vector3 n = Vector3.Cross(u, v);

                    //Ensures the normal is pointing above the table
                    if (Vector3.Dot(n, camView) < 0)
                        n *= -1;

                    //Increments the average
                    averageX += n.x;
                    averageY += n.y;
                    averageZ += n.z;
                }
            }

            toRet = new Vector3(averageX, averageY, averageZ);
            toRet.Normalize();
            return toRet;
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

        private List<Vector3> FloodFill(ColorDepthImage image, int x, int y, Color stopColor)
        {
            //Images
            Texture2D color = image.ColorImage;
            Texture2D depth = image.DepthImage;

            //Collections
            List<Vector3> toRet = new List<Vector3>();
            Queue<Pixel> pixelQueue = new Queue<Pixel>();
            pixelQueue.Enqueue(new Pixel(x, y));

            do
            {
                //Get the pixel out of the queue
                Pixel p = pixelQueue.Dequeue();

                //Check if the current pixel is the stop color
                if (color.GetPixel(p.X, p.Y) != stopColor)
                {
                    //Add the current pixel as a point to return
                    float Z = depth.GetPixel(p.X, p.Y).grayscale;
                    toRet.Add(new Vector3(p.X, p.Y, Z));

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

        private Vector3 AveragePoint(List<Vector3> points)
        {
            float averageX = 0;
            float averageY = 0;
            float averageZ = 0;

            foreach(Vector3 p in points)
            {
                averageX += p.x;
                averageY += p.y;
                averageZ += p.z;
            }

            averageX /= points.Count;
            averageY /= points.Count;
            averageZ /= points.Count;

            return new Vector3(averageX, averageY, averageZ);
        }

        private Queue<List<Vector3>> clumpQueue = new Queue<List<Vector3>>();

        private Vector3 normalVector;

        private void Contrastify(Texture2D colorImage, Color averageColor)
        {
            for (int y = 0; y < colorImage.height; y++)
            {
                for (int x = 0; x < colorImage.width; x++)
                {
                    Color currColor = colorImage.GetPixel(x, y);
                    Color newColor = !AreColorsDifferent(averageColor, currColor) ? Color.white : Color.black;
                    colorImage.SetPixel(x, y, newColor);
                }
            }
        }

        private void Clumpify(ColorDepthImage image)
        {
            for (int y = 0; y < image.ColorImage.height; y++)
            {
                for (int x = 0; x < image.ColorImage.width; x++)
                {
                    //Checks if the difference in color is within the threshold
                    Color thisColor = image.ColorImage.GetPixel(x, y);
                    if (AreColorsDifferent(thisColor, Color.white))
                    {
                        //Perform flood fill
                        List<Vector3> clump = FloodFill(image, x, y, Color.white);

                        //Save clump
                        clumpQueue.Enqueue(clump);
                    }
                }
            }
        }

        private Vector3 ConvertCoordinates(Vector3 point, float angle)
        {
            float newX, newY, newZ;
            newX = point.x;
            newY = (float) (Math.Sin(angle) * point.y + Math.Cos(angle) * point.z);
            newZ = (float) (Math.Cos(angle) * point.y - Math.Sin(angle) * point.z);

            return new Vector3(newX, newY, newZ);
        }

        private Pixel GetTablePixel(Texture2D colorImage)
        {
            Pixel p = new Pixel(-1, -1);

            while (p.X == -1 && p.Y == -1)
            {
                int ranX = (int)Math.Round((double)UnityEngine.Random.Range(0, colorImage.width));
                int ranY = (int)Math.Round((double)UnityEngine.Random.Range(0, colorImage.height));

                if (colorImage.GetPixel(ranX, ranY) == Color.white)
                    p = new Pixel(ranX, ranY);
            }

            return p;
        }
    }
}