﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityScanner3D.ComputerVision
{
    static class ImageUtils
    {
        private const float DIFFERENCE_THRESHOLD = 0.45f;

        public static bool AreColorsDifferent(Color u, Color v)
        {
            return ColorDifference(u, v) > DIFFERENCE_THRESHOLD;
        }

        public static Color CalculateAverageColor(Texture2D colorImage)
        {
            return CalculateAverageColor(colorImage.GetPixels());
        }

        public static Color CalculateAverageColor(Texture2D colorImage, Color oldAverage, float stdDev, float range)
        {
            return CalculateAverageColor(colorImage.GetPixels(), oldAverage, stdDev, range);
        }

        public static Color CalculateAverageColor(IEnumerable<Color> colors)
        {
            float r = 0, g = 0, b = 0;
            float num = 0.0f;

            foreach(var c in colors)
            {
                r += c.r;
                g += c.g;
                b += c.b;
                num += 1.0f;
            }

            return new Color(r / num, g / num, b / num);
        }

        public static Color CalculateAverageColor(IEnumerable<Color> colors, Color oldAverage, float stdDev, float range)
        {
            float r = 0, g = 0, b = 0;
            int num = 0;

            foreach(var c in colors)
            {
                if (ColorDifference(oldAverage, c) < stdDev * range)
                {
                    r += c.r;
                    g += c.g;
                    b += c.b;
                    num++;
                }
            }

            return new Color(r / num, g / num, b / num);
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

        public static Pixel GetRandomPixel(Texture2D colorImage, Func<Color, bool> condition)
        {
            Color c;
            int x;
            int y;

            do
            {
                x = UnityEngine.Random.Range(0, colorImage.width);
                y = UnityEngine.Random.Range(0, colorImage.height);
                c = colorImage.GetPixel(x, y);
            } while (!condition(c));

            return new Pixel(x, y);
        }
    }
}
