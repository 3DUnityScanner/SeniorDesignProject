using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pointmatcher.net;
using UnityEngine;

namespace pointmatcherTests
{
    [TestClass]
    public class SamplingSurfaceNormalTest
    {
        [TestMethod]
        public void SingleBinTest()
        {
            var points = new List<Vector3>();
            var r = new System.Random();
            for (int i = 0; i < 100; i++)
            {
                points.Add(new Vector3((float)(r.NextDouble() * 10), (float)(r.NextDouble() * 10), 0));
            }

            var filter = new SamplingSurfaceNormalDataPointsFilter(SamplingMethod.Bin, knn: points.Count);
            var processed = filter.Filter(new DataPoints
                {
                    points = points.Select(x => new DataPoint { point = x }).ToArray()
                });

            var normal = processed.points[0].normal;
            Assert.AreEqual(0, normal.x);
            Assert.AreEqual(0, normal.y);
            Assert.AreEqual(1, Math.Abs(normal.z));
        }
    }
}
