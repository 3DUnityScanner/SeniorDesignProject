using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Globalization;
using knearest;
using pointmatcher.net;
using UnityEngine;
using UnityScanner3D.CameraIO;

namespace UnityScanner3D.ComputerVision
{
    public class icp:IAlgorithm
    {
        List<Shape> poseList;   
        //output
        public IEnumerable<Shape> GetShapes()
        {
            return poseList;
        }
        //input
        public void ProcessImage(ColorDepthImage image)
        {
            DataPoints cloud = new DataPoints(); DataPoint newPoint = new DataPoint();
            int ctr = 0;
            foreach (var v in image.GetCloudPointData())
            {
                newPoint.point.x=v.x;
                newPoint.point.y = v.y;
                newPoint.point.z = v.z;
                cloud.points[ctr] =  newPoint;
                ctr++;
            }
            poseList.Add(runICP(cloud));
        }

        //Get a point cloud (list of 3d points) from a .ply file for testing purposes
        public DataPoints getCloudFromPLY(string filename)
        {
            FileStream file = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Read);
            StreamReader reader = new StreamReader(file);
            var fmt = new NumberFormatInfo()
            {
                NegativeSign = "-"
            };
            DataPoints cloud = new DataPoints();
            Vector3 vec = Vector3.zero;
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
                    vec.x = float.Parse(items[0], fmt);
                    vec.y = float.Parse(items[1], fmt);
                    vec.z = float.Parse(items[2], fmt);
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
        public Shape runICP(DataPoints reading)
        {
            //DataPoints reading = getCloudFromPLY("../../mesh.ply");//point cloud
            DataPoints reference = getCloudFromPLY("../../mesh2.ply"); ;//reference point cloud //DEBUG

            //could do RANSAC to init pose and ICP to refine??? Random for now
            System.Random r = new System.Random();
            EuclideanTransform init = new EuclideanTransform()//initial guess (?)
            {
                translation = { x = (float)r.NextDouble(), y = (float)r.NextDouble(), z = (float)r.NextDouble() },
                rotation = {w= (float)r.NextDouble(), x= (float)r.NextDouble(), y= (float)r.NextDouble() , z= (float)r.NextDouble() }
            };
            
            ICP icp = new ICP();
            icp.ReadingDataPointsFilters = new RandomSamplingDataPointsFilter(prob: 0.1f);
            icp.ReferenceDataPointsFilters = new SamplingSurfaceNormalDataPointsFilter(SamplingMethod.RandomSampling, ratio: 0.2f);
            icp.OutlierFilter = new TrimmedDistOutlierFilter(ratio: 0.5f);

            var transform = icp.Compute(reading, reference, init);
            Shape pose = new Shape()
            {
                //translation & rotation from transform
                Translation = transform.translation,
                Rotation = transform.rotation
            };
             return pose;
        }
    }
}
