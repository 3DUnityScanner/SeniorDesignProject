using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MathNet.Numerics.LinearAlgebra.Generic;
using MathNet.Numerics.LinearAlgebra.Single;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra.Storage;
using knearest;
using pointmatcher.net;
using MathNet.Numerics;
using System.IO;
using System.Globalization;

namespace accord
{ 
    [TestClass]
    public class testICP
    {
        /// <summary>
        /// Test KNN - 
        /// assigns weights to each point based on its distance to the query model
        /// </summary>
        [TestMethod]
        public void testKnn()
        {
            Matrix<float> queryMatrix = DenseMatrix.CreateRandom(3, 100, new ContinuousUniform(-10, 10));
            DenseColumnMajorMatrixStorage<float> query = (DenseColumnMajorMatrixStorage<float>)queryMatrix.Storage;
            Matrix<float> points = DenseMatrix.CreateRandom(3, 100, new ContinuousUniform(-10, 10));
            MathNet.Numerics.LinearAlgebra.Generic.Vector<float> maxRadii = DenseVector.Create(100, i => float.PositiveInfinity);

            //good search
            var search = new KdTreeNearestNeighborSearch((DenseColumnMajorMatrixStorage<float>)points.Storage); 
            var results = DenseColumnMajorMatrixStorage<int>.OfInit(1, 100, (i, j) => 0);
            var resultDistances = DenseColumnMajorMatrixStorage<float>.OfInit(1, 100, (i, j) => 0);
            search.knn(query, results, resultDistances, maxRadii, k: 1, epsilon: float.Epsilon, optionFlags: SearchOptionFlags.AllowSelfMatch);

            //bad but correct search
            var bruteForceSearch = new BruteForceNearestNeighbor(points);
            var results2 = DenseColumnMajorMatrixStorage<int>.OfInit(1, 100, (i, j) => 0);
            var resultDistances2 = DenseColumnMajorMatrixStorage<float>.OfInit(1, 100, (i, j) => 0);
            search.knn(query, results2, resultDistances2, maxRadii, k: 1, epsilon: float.Epsilon, optionFlags: SearchOptionFlags.AllowSelfMatch);

            //make sure results are the same
            for (int i = 0; i < results.ColumnCount; i++)
            {
                for (int j = 0; j < results.RowCount; j++)
                {
                    Assert.AreEqual(results2.At(j, i), results.At(j, i));
                }
            }
        }

        /// <summary>
        /// Test Error Minimization - 
        /// minimizing alignment error
        /// </summary>
        private static Random r = new Random();

        [TestMethod]
        public void TestTransformComputation()
        {
            EuclideanTransform t;
            ErrorElements errorElements;
            ConstructTestCase(out t, out errorElements);

            var minimizer = new PointToPlaneErrorMinimizer();
            var solvedT = KnownCorrespondenceErrorMinimizer.IterativeSolveForTransform(errorElements, minimizer);

            float dist = (t.translation - solvedT.translation).Length();
            Assert.IsTrue(Precision.AlmostEqualInDecimalPlaces(0.0f, dist, 3));

            float angle = VectorHelpers.AngularDistance(t.rotation, solvedT.rotation);
            Assert.IsTrue(Precision.AlmostEqualInDecimalPlaces(0.0, Math.IEEERemainder(angle, Math.PI * 2), 3));
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
                    point = 100.0f * RandomVector() - new Vector3(50.0f),
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
            t.rotation = Quaternion.CreateFromAxisAngle(axis, (float)(r.NextDouble() * Math.PI * 2));
            t.rotation = Quaternion.Normalize(t.rotation);
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

        /// <summary>
        /// Test Quick Selection - 
        /// quickly gets a value that satisfies a given param
        /// </summary>
        [TestMethod]
        public void QuickSelectTest()
        {
            var r = new Random();

            float[] d = Enumerable.Range(0, 100).Select(i => (float)r.NextDouble()).ToArray();

            var dSorted = new List<float>(d);
            dSorted.Sort();

            int[] indices = Enumerable.Range(0, d.Length).ToArray();
            int n = 30;
            QuickSelect.Select(indices, 0, d.Length - 1, n, i => d[i]);

            var selectedD = indices.Select(i => d[i]).ToArray();

            for (int i = 0; i < n; i++)
            {
                Assert.IsTrue(selectedD[i] < selectedD[n]);
            }

            float limit = d[indices[n]];

            Assert.AreEqual(30, d.Count(x => x < limit));
        }

        /// <summary>
        /// Test Normal Sampling - 
        /// finds normals
        /// </summary>
        [TestMethod]
        public void SingleBinTest()
        {
            var points = new List<Vector3>();
            var r = new Random();
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
            Assert.AreEqual(0, normal.X);
            Assert.AreEqual(0, normal.Y);
            Assert.AreEqual(1, Math.Abs(normal.Z));
        }


    }
    /// <summary>
    /// Actual algorithm goes here
    /// </summary>
    public class icp
    {
        //Get a point cloud (list of 3d points) from a .ply file
        public DataPoints getCloudFromPLY(string filename)
        {
            FileStream file = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Read);
            StreamReader reader = new StreamReader(file);
            var fmt = new NumberFormatInfo()
            {
                NegativeSign = "-"
            };
            DataPoints cloud = new DataPoints();
            Vector3 vec;
            string line;
            int ctr=0, index=0, limit=-1, flag=0;//hey i see u lookin at my excessive vars

            while ((line = reader.ReadLine()) != null)
            {
                string[] items = line.Split(' ');
                if (items[0].Equals("element") && items[1].Equals("vertex"))
                {
                    limit = int.Parse(items[2]);//num of verts
                    cloud.points = new DataPoint[limit];
                }

                if (flag == 1)
                {
                    vec.X = float.Parse(items[0], fmt);
                    vec.Y = float.Parse(items[1], fmt);
                    vec.Z = float.Parse(items[2], fmt);
                    cloud.points[index].point = vec;
                    index++;
                }
                else if (line.Equals("end_header"))
                { flag = 1; }
                
                ctr++;
                if (ctr == limit)
                        break;
                
            }
            file.Close();
            reader.Close();
            return cloud;
        }

        //TODO: add return for EuclideanTransform -> Shape when in actual project
        public void runICP()
        {
            DataPoints reading = getCloudFromPLY("../../mesh.ply");//point cloud
            DataPoints reference = getCloudFromPLY("../../mesh2.ply"); ;//reference point cloud //DEBUG

            //could do RANSAC to init pose and ICP to refine??? Random for now
            Random r = new Random();
            EuclideanTransform init = new EuclideanTransform()//initial guess (?)
            {
                translation = { X = (float)r.NextDouble(), Y = (float)r.NextDouble(), Z = (float)r.NextDouble() },
                rotation = {W= (float)r.NextDouble(), X= (float)r.NextDouble(), Y= (float)r.NextDouble() , Z= (float)r.NextDouble() }
            };
            
            ICP icp = new ICP();
            icp.ReadingDataPointsFilters = new RandomSamplingDataPointsFilter(prob: 0.1f);
            icp.ReferenceDataPointsFilters = new SamplingSurfaceNormalDataPointsFilter(SamplingMethod.RandomSampling, ratio: 0.2f);
            icp.OutlierFilter = new TrimmedDistOutlierFilter(ratio: 0.5f);

            var transform = icp.Compute(reading, reference, init);
            /*Shape pose = new Shape()
             * {
             *   //translation & rotation from transform   
             * };
             * return pose;
             */
        }
    }
}
