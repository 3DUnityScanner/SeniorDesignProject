using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using MathNet.Numerics.LinearAlgebra.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace pointmatcher.net
{
    public class PointToPlaneErrorMinimizer : IErrorMinimizer
    {
        public EuclideanTransform SolveForTransform(ErrorElements mPts)
        {
            if (!mPts.reference.contiansNormals)
            {
                throw new ArgumentException("Reference points must have computed normals. Use appropriate input filter.");
            }

            var readingPts = mPts.reading.points;
            var refPts = mPts.reference.points;

            // Compute cross product of cross = cross(reading X normalRef)
            // wF = [ weights*cross   ]
            //      [ weights*normals ]
            //
            // F  = [ cross   ]
            //      [ normals ]
            var wF = new DenseMatrix(6, readingPts.Length);
            var F = new DenseMatrix(6, readingPts.Length);
            for (int i = 0; i < readingPts.Length; i++)
            {
                Vector3 cross = Vector3.Cross( readingPts[i].point, refPts[i].normal);
                var wCross = (cross * mPts.weights[i]);
                Vector3 wNormal = refPts[i].normal * mPts.weights[i];
                wF.At(0, i, wCross.x);
                wF.At(1, i, wCross.y);
                wF.At(2, i, wCross.z);
                wF.At(3, i, wNormal.x);
                wF.At(4, i, wNormal.y);
                wF.At(5, i, wNormal.z);
                F.At(0, i, cross.x);
                F.At(1, i, cross.y);
                F.At(2, i, cross.z);
                F.At(3, i, refPts[i].normal.x);
                F.At(4, i, refPts[i].normal.y);
                F.At(5, i, refPts[i].normal.z);
            }

            // Unadjust covariance A = wF * F'
            var A = wF.TransposeAndMultiply(F);

            // dot product of dot = dot(deltas, normals)
            var dotProd = new DenseMatrix(1, mPts.reading.points.Length);
            for (int i = 0; i < readingPts.Length; i++)
            {
                var delta = readingPts[i].point - refPts[i].point;
                dotProd.At(0, i, Vector3.Dot(delta, refPts[i].normal));
            }

            // b = -(wF' * dot)
            var b = -(wF.TransposeAndMultiply(dotProd));

            // Cholesky decomposition
            var x = A.Cholesky().Solve(b);

            EuclideanTransform transform;
            Vector3 axis = new Vector3(x.At(0, 0), x.At(1, 0), x.At(2, 0));
            float len = axis.magnitude;
             var ref1 = axis / len;
            Quaternion quat = Quaternion.AngleAxis(len, ref1);
            transform.rotation = quat;//TODO: normalize
            transform.translation = new Vector3(x.At(3, 0), x.At(4, 0), x.At(5, 0));

            return transform;
        }
    }

    public class ErrorElements
    {
        public DataPoints reading;
        public DataPoints reference;
        public float[] weights;
    }

    public static class ErrorMinimizerHelper
    {
        public static EuclideanTransform Compute(
            DataPoints filteredReading,
            DataPoints filteredReference,
            Matrix<float> outlierWeights,
            Matches matches,
            IErrorMinimizer minimizer)
        {
            Debug.Assert(matches.Ids.RowCount > 0);
            ErrorElements mPts = ErrorMinimizerHelper.GetMatchedPoints(filteredReading, filteredReference, matches, outlierWeights);
            return minimizer.SolveForTransform(mPts);
        }

        private static ErrorElements GetMatchedPoints(
		    DataPoints requestedPts,
		    DataPoints sourcePts,
		    Matches matches, 
		    Matrix<float> outlierWeights)
        {
	        Debug.Assert(matches.Ids.RowCount > 0);
	        Debug.Assert(matches.Ids.ColumnCount > 0);
	        Debug.Assert(matches.Ids.ColumnCount == requestedPts.points.Length); //nbpts
	        Debug.Assert(outlierWeights.RowCount == matches.Ids.RowCount);  // knn
	
	        int knn = outlierWeights.RowCount;
	        
            int maxPointsCount = matches.Ids.RowCount * matches.Ids.ColumnCount;

            var keptPoints = new List<DataPoint>(maxPointsCount);
            var matchedPoints = new List<DataPoint>(maxPointsCount);
	        List<float> keptWeights = new List<float>(maxPointsCount);

	        //float weightedPointUsedRatio = 0;
	        for(int k = 0; k < knn; k++) // knn
	        {
		        for (int i = 0; i < requestedPts.points.Length; ++i) //nb pts
		        {
                    float weight = outlierWeights.At(k,i);
			        if (weight != 0.0f)
			        {
				        keptPoints.Add(requestedPts.points[i]);

                        int matchIdx = matches.Ids.At(k, i);
                        matchedPoints.Add(sourcePts.points[matchIdx]);
				        keptWeights.Add(weight);
				        //weightedPointUsedRatio += weight;
			        }
		        }
	        }

            var result = new ErrorElements
            {
                reading = new DataPoints
                {
                    points = keptPoints.ToArray(),
                    contiansNormals = requestedPts.contiansNormals,
                },
                reference = new DataPoints
                {
                    points = matchedPoints.ToArray(),
                    contiansNormals = sourcePts.contiansNormals,
                },
                weights = keptWeights.ToArray(),
            };
            return result;
        }
    }
}
