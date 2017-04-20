using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityScanner3D.ComputerVision
{
    static class ImageUtils
    {
        private const float DIFFERENCE_THRESHOLD = 0.3f;

        public static bool AreColorsDifferent(Color u, Color v)
        {
            return ColorDifference(u, v) > DIFFERENCE_THRESHOLD;
        }

        public static Color CalculateAverageColor(Texture2D colorImage)
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

        public static Color CalculateAverageColor(Texture2D colorImage, Color oldAverage, float stdDev, float range)
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

        public static float ColorDifference(Color u, Color v)
        {
            //Calculates the squared difference of each component
            double r = Math.Pow(u.r - v.r, 2.0);
            double g = Math.Pow(u.g - v.g, 2.0);
            double b = Math.Pow(u.b - v.b, 2.0);

            //Returns the square root
            float distance = (float)Math.Sqrt(r + g + b);
            return distance;
        }

        public static float CalculateStandardColorDeviation(Texture2D colorImage, Color average)
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
            return (float)stdDev;
        }
    }
}
