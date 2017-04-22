using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityScanner3D.CameraIO;

namespace UnityScanner3D.ComputerVision
{
    public class ColorTrackingAlgorithm : IAlgorithm
    {
        public const int OBJSCALE = 25;
        public float STDEV = 2.0f;
        public const float STDEV_D = 2.0f;
        public int CLUMPTHRESH = 1700;
        public const int CLUMPTHRESH_D = 1700;
        private const float DIFFERENCE_THRESHOLD = 0.3f;
        private const int HOPSCOTCH_VALUE = 10;

        private Texture2D whiteImage;
        private Vector3 normalVector;
        private Color averageColor;
        private Queue<Clump> clumpQueue = new Queue<Clump>();
        private string logText = "";

        private Texture2D contrastImage = null;
        private Texture2D colorImage = null;
        private Texture2D depthImage = null;

        string redObjString = "", greenObjString = "", blueObjString = "";
        string redFilename = "", greenFilename = "", blueFilename = "";
        private bool enableRedObj = false, enableGreenObj = false, enableBlueObj = false;

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
                Clump clump = clumpQueue.Dequeue();
                
                //check for little clumps to ignore
                if (clump.Points.Count >= CLUMPTHRESH)
                {
                    Vector3 averagePoint = ConvertCoordinates(AveragePoint(clump.Points), angle);
                
                    //set all poses to lie on the ground (y = 0.5) times the scale
                    averagePoint.y = OBJSCALE*0.5f;

                    //Return shape at the given point
                    yield return new Shape()
                    {
                        Type = clump.Color.b > clump.Color.r && clump.Color.b > clump.Color.g ? ShapeType.Cylinder:ShapeType.Cube,
                        Translation = averagePoint,
                        Rotation = new Quaternion(0, 0, 0, 0)
                    };
                }
            }
        }

        public void ProcessImage(ColorDepthImage image)
        {
            colorImage = image.ColorImage;
            depthImage = image.DepthImage;
            
            //Calculate the initial average color and standard deviation
            averageColor = ImageUtils.CalculateAverageColor(colorImage);
            float stdDev = ImageUtils.CalculateStandardColorDeviation(colorImage, averageColor);

            //Refine average color
            averageColor = ImageUtils.CalculateAverageColor(colorImage, averageColor, stdDev, STDEV);

            //Maximize contrast in image and save the new image
            Contrastify(averageColor);
            File.WriteAllBytes("contrast.png", contrastImage.EncodeToPNG());

            //Identify clumps
            CalculateTableNormal();
            Clumpify();
        }

        public Texture2D PreviewImage(ColorDepthImage image)
        {
            colorImage = image.ColorImage;
            depthImage = image.DepthImage;
            averageColor = ImageUtils.CalculateAverageColor(image.ColorImage);
            Contrastify(averageColor);
            return contrastImage;
        }

        private Vector3 AveragePoint(IEnumerable<Vector3> points)
        {
            float averageX = 0;
            float averageY = 0;
            float averageZ = 0;
            int Count = points.Count();

            foreach (Vector3 p in points)
            {
                averageX += p.x;
                averageY += p.y;
                averageZ += p.z;
            }

            averageX /= Count;
            averageY /= Count;
            averageZ /= Count;

            return new Vector3(averageX, averageY, averageZ);
        }

        private void CalculateTableNormal()
        {
            //Calculates the normal vector of the table
            float averageX = 0;
            float averageY = 0;
            float averageZ = 0;

            //Picks an arbitrary starting point
            Pixel tail = ImageUtils.GetRandomPixel(colorImage, c => c == Color.white);

            //Constructs vectors
            List<Vector3> vectors = new List<Vector3>(10);
            for (int i = 0; i < 10; i++)
            {
                Pixel head = ImageUtils.GetRandomPixel(colorImage, c => c == Color.white);
                float headZ = depthImage.GetPixel(head.X, head.Y).grayscale;
                float tailZ = depthImage.GetPixel(tail.X, tail.Y).grayscale;

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

            normalVector = new Vector3(averageX, averageY, averageZ);
            normalVector.Normalize();
        }

        private List<Vector3> FloodFill(int x, int y, Color stopColor)
        {
            //Collections
            List<Vector3> toRet = new List<Vector3>();
            Queue<Pixel> pixelQueue = new Queue<Pixel>();
            pixelQueue.Enqueue(new Pixel(x, y));

            do
            {
                //Get the pixel out of the queue
                Pixel p = pixelQueue.Dequeue();

                //Check if the current pixel is the stop color
                if (colorImage.GetPixel(p.X, p.Y) != stopColor)
                {
                    //Add the current pixel as a point to return
                    float Z = depthImage.GetPixel(p.X, p.Y).grayscale;
                    toRet.Add(new Vector3(p.X, p.Y, Z));

                    //Mark it as visited
                    colorImage.SetPixel(p.X, p.Y, stopColor);

                    //Gets the neighboring pixels
                    Pixel left = new Pixel(p.X - 1, p.Y);
                    Pixel right = new Pixel(p.X + 1, p.Y);
                    Pixel up = new Pixel(p.X, p.Y - 1);
                    Pixel down = new Pixel(p.X, p.Y + 1);

                    //Add its neighbors to the queue of pixels to be processed
                    if (p.X - 1 >= 0 && !pixelQueue.Contains(left))
                        pixelQueue.Enqueue(left);

                    if (p.X + 1 < colorImage.width && !pixelQueue.Contains(right))
                        pixelQueue.Enqueue(right);

                    if (p.Y - 1 >= 0 && !pixelQueue.Contains(up))
                        pixelQueue.Enqueue(up);

                    if (p.Y + 1 < colorImage.height && !pixelQueue.Contains(down))
                        pixelQueue.Enqueue(down);
                }

            } while (pixelQueue.Count > 0);

            return toRet;
        }

        private void Contrastify(Color backgroundColor)
        {
            //Creates the white image if it is uninitialized
            if (whiteImage == null) { 
                whiteImage = new Texture2D(colorImage.width, colorImage.height);
                for (int y = 0; y < colorImage.height; y++)
                    for (int x = 0; x < colorImage.width; x++)
                        whiteImage.SetPixel(x, y, Color.white);
            }

            //Makes a copy of the white image as the starting point for the contrast image
            contrastImage = Texture2D.Instantiate(whiteImage);

            var colorPixels = colorImage.GetPixels();
            var contrastPixels = contrastImage.GetPixels();
            for (int y = 0; y < colorImage.height; y++)
            {
                for (int x = 0; x < colorImage.width; x += HOPSCOTCH_VALUE)
                {
                    //Test if the current color is different from the background color
                    Color currColor = colorPixels[y * colorImage.width + x];
                    if (ImageUtils.AreColorsDifferent(backgroundColor, currColor))
                    {
                        //Continue checking to the left until a background pixel is encountered
                        int i = 0;
                        while (ImageUtils.AreColorsDifferent(backgroundColor, currColor))
                        {
                            contrastPixels[y * contrastImage.width + x - i] = Color.black;
                            i++;
                            if (x - i < 0)
                                break;
                            currColor = colorPixels[y * colorImage.width + x - i];
                        }

                        i = 1;
                        currColor = colorImage.GetPixel(x + i, y);

                        //Continue checking to the right until a background pixel is encountered
                        while (ImageUtils.AreColorsDifferent(backgroundColor, currColor))
                        {
                            contrastPixels[y * contrastImage.width + x + i] = Color.black;
                            i++;
                            if (x + i >= colorImage.width)
                                break;
                            currColor = colorPixels[y * colorImage.width + x + i];
                        }

                        x += i - 1;
                    }

                }
            }

            contrastImage.SetPixels(contrastPixels);
        }

        private void Clumpify()
        {
            for (int y = 0; y < colorImage.height; y++)
            {
                for (int x = 0; x < colorImage.width; x++)
                {
                    //Checks if the difference in color is within the threshold
                    Color thisColor = this.contrastImage.GetPixel(x, y);
                    if (ImageUtils.AreColorsDifferent(thisColor, Color.white))
                    {
                        //Perform flood fill
                        Clump clump = new Clump(FloodFill(x, y, Color.white));

                        //Calculate average color of clump
                        clump.Color = ImageUtils.CalculateAverageColor(clump.GetClumpPixels(colorImage));

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


        public void DrawSettings()
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();//Red

            enableRedObj = GUILayout.Toggle(enableRedObj, "Red", GUILayout.Width(60));
            GUILayout.Label("Model:", GUILayout.Width(40));
            GUILayout.TextField(redFilename, GUILayout.Width(152));
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                redFilename = EditorUtility.OpenFilePanelWithFilters("Choose Model File", "",
                    new string[] { "Object Files", "fbx,dae,3ds,dxf,obj,skp" });
            }
            GUILayout.Space(81);
            GUILayout.Label("Scale:", GUILayout.Width(40));
            GUILayout.TextField(redObjString);
            GUILayout.Space(10);
            GUILayout.Label("Count:", GUILayout.Width(42));
            GUILayout.TextField(redObjString);

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();//Green

            enableGreenObj = GUILayout.Toggle(enableGreenObj, "Green", GUILayout.Width(60));
            GUILayout.Label("Model:", GUILayout.Width(40));
            GUILayout.TextField(greenFilename, GUILayout.Width(152));
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                greenFilename = EditorUtility.OpenFilePanelWithFilters("Choose Model File", "",
                    new string[] { "Object Files", "fbx,dae,3ds,dxf,obj,skp" });
            }
            GUILayout.Space(81);
            GUILayout.Label("Scale:", GUILayout.Width(40));
            GUILayout.TextField(greenObjString);
            GUILayout.Space(10);
            GUILayout.Label("Count:", GUILayout.Width(42));
            GUILayout.TextField(greenObjString);

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();//Blue

            enableBlueObj = GUILayout.Toggle(enableBlueObj, "Blue", GUILayout.Width(60));
            GUILayout.Label("Model:", GUILayout.Width(40));
            GUILayout.TextField(blueFilename, GUILayout.Width(152));
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                blueFilename = EditorUtility.OpenFilePanelWithFilters("Choose Model File", "",
                    new string[] { "Object Files", "fbx,dae,3ds,dxf,obj,skp" });
            }
            GUILayout.Space(81);
            GUILayout.Label("Scale:", GUILayout.Width(40));
            GUILayout.TextField(blueObjString);
            GUILayout.Space(10);
            GUILayout.Label("Count:", GUILayout.Width(42));
            GUILayout.TextField(blueObjString);

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            //////////////////////////

            GUILayout.Label("Algorithm Settings (Color Tracking)", EditorStyles.boldLabel);
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("STDEV", GUILayout.Width(100));
            STDEV = GUILayout.HorizontalSlider(STDEV, 1.0F, 3.0F);
            GUILayout.Box("" + STDEV, GUILayout.Width(70));
            if (GUILayout.Button("Reset", GUILayout.Width(60)))
                STDEV = STDEV_D;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("CLUMPTHRESH", GUILayout.Width(100));
            CLUMPTHRESH = (int)GUILayout.HorizontalSlider(CLUMPTHRESH, 1500, 2000);
            GUILayout.Box("" + CLUMPTHRESH, GUILayout.Width(70));
            if (GUILayout.Button("Reset", GUILayout.Width(60)))
                CLUMPTHRESH = CLUMPTHRESH_D;
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Algorithm Log");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.TextArea(logText, GUILayout.ExpandHeight(true));//GUILayout.MaxHeight(150));
            GUILayout.EndVertical();

            GUILayout.EndVertical();
        }
    }
}