using MathNet.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pointmatcher.net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace pointmatcherTests
{
    [TestClass]
    public class ErrorMinimizerTest
    {
        private static System.Random r = new System.Random();

        [TestMethod]
        public void TestTransformComputation()
        {
            EuclideanTransform t;
            ErrorElements errorElements;
            ConstructTestCase(out t, out errorElements);

            var minimizer = new PointToPlaneErrorMinimizer();
            var solvedT = KnownCorrespondenceErrorMinimizer.IterativeSolveForTransform(errorElements, minimizer);

            float dist = (t.translation - solvedT.translation).magnitude;
            Assert.IsTrue(Precision.AlmostEqual(0.0f, dist, 3));

            float angle = VectorHelpers.AngularDistance(t.rotation, solvedT.rotation);
            Assert.IsTrue(Precision.AlmostEqual(0.0, Math.IEEERemainder(angle, Math.PI * 2), 3));
        }

        private static void ConstructTestCase(out EuclideanTransform t, out ErrorElements errorElements)
        {
            // pick some random points
            var points = new List<DataPoint>();
            for (int i = 0; i < 10000; i++)
            {
                var n = RandomVector();
                points.Add(new DataPoint
                {
                    point = 100.0f * RandomVector() - new Vector3(50.0f, 50.0f, 50.0f),
                    normal = Vector3.Normalize(n),
                });
            }

            var dataPoints = new DataPoints
            {
                points = points.ToArray(),
                contiansNormals = true,
            };

            t = new EuclideanTransform();
            t.translation = RandomVector() * 50.0f;
            //t.translation = new Vector3(0f);
            var axis = Vector3.Normalize(RandomVector());
            t.rotation = Quaternion.AngleAxis((float)(r.NextDouble() * Math.PI * 2), axis);
            t.rotation = Quaternion.Lerp(t.rotation, t.rotation, 0);//TODO: Normalizing with lerp
            //t.rotation = Quaternion.Identity;

            var transformedPoints = ICP.ApplyTransformation(dataPoints, t.Inverse());

            errorElements = new ErrorElements
            {
                reference = dataPoints,
                reading = transformedPoints,
                weights = Enumerable.Repeat(1.0f, points.Count).ToArray()
            };
        }

        private static Vector3 RandomVector()
        {
            return new Vector3((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble());
        }
    }
}
