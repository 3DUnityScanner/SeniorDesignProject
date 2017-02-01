using System;
using System.Drawing;
using Accord.Imaging;
using Accord.Vision;
using Accord.Math.Geometry;
using Accord.Math;

namespace accord
{
    //accord img handling
    //Bitmap bmp = FromFile("block.jpg");
    

    class positTest
    {
        //basic posit estimation
        public void estimate(Vector3[] model, float fl)
        {
            //POSIT object
            Posit posit = new Posit(model, fl);

            //projection points of the cube
            Accord.Point[] projectedPoints = new Accord.Point[4]
            {
            new Accord.Point(-4,29),
            new Accord.Point(-180,86),
            new Accord.Point(-5,-102),
            new Accord.Point(76,137),
            };

            //estimating pose
            Matrix3x3 rotation;
            Vector3 translation;
            posit.EstimatePose(projectedPoints, out rotation, out translation);
            System.Console.WriteLine("posit rotation:" + rotation.V00+","+rotation.V01 + "," + rotation.V02 + ",\n" 
                + rotation.V10 + "," + rotation.V11 + "," + rotation.V12 + ",\n" 
                + rotation.V20 + "," + rotation.V21 + "," + rotation.V22);
            System.Console.WriteLine("posit translation:" + translation);
            System.Console.ReadLine();
        }
    }
}
