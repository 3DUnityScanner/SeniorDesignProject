using System;
using System.Collections.Generic;
using UnityScanner3D.ComputerVision;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using MathNet.Numerics.LinearAlgebra.Generic;
using MathNet.Numerics.LinearAlgebra.Single;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra.Storage;
using knearest;
using pointmatcher.net;
using MathNet.Numerics;
using System.IO;
using System.Globalization;
namespace cv
{
    class cv
    {
        /// <summary>
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
                Vector3 vec = new Vector3(0,0,0);
                string line;
                int ctr = 0, index = 0, limit = -1, flag = 0;//hey i see u lookin at my excessive vars

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

            //TODO: add support for incoming clouds
            Shape runICP()
            {
                DataPoints reading = getCloudFromPLY("../../mesh.ply");//point cloud
                DataPoints reference = getCloudFromPLY("../../mesh2.ply"); ;//reference point cloud //DEBUG

                //TODO: Need better way to init guess??? Random for now
                Random r = new Random();
                EuclideanTransform init = new EuclideanTransform()//initial guess (?)
                {
                    translation = { X = (float)r.NextDouble(), Y = (float)r.NextDouble(), Z = (float)r.NextDouble() },
                    rotation = { W = (float)r.NextDouble(), X = (float)r.NextDouble(), Y = (float)r.NextDouble(), Z = (float)r.NextDouble() }
                };

                ICP icp = new ICP();
                icp.ReadingDataPointsFilters = new RandomSamplingDataPointsFilter(prob: 0.1f);
                icp.ReferenceDataPointsFilters = new SamplingSurfaceNormalDataPointsFilter(SamplingMethod.RandomSampling, ratio: 0.2f);
                icp.OutlierFilter = new TrimmedDistOutlierFilter(ratio: 0.5f);

                var transform = icp.Compute(reading, reference, init);
                Shape pose = new Shape()
                  {
                    //translation & rotation from transform   
                  };
                  return pose;
                 
            }
        }
    }

}

