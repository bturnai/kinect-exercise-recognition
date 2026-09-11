using Microsoft.Kinect;
using Microsoft.Samples.Kinect.DepthBasics.Models;
using System;

namespace Models
{
    public class ImageWithDepth
    {
        public static int JointCount { get { return 25; } }
        public ImageWithDepth()
        {
        }

        public float[] SkeletonDatas { get; set; }
        //public DepthImagePixel[] DepthDatas { get; set; }
        public short[] DepthDatas { get; set; }


        public JointData GetJoint(int jointIndex)
        {
            float X = SkeletonDatas[jointIndex * 3];
            float Y = SkeletonDatas[jointIndex * 3 + 1];
            var depth= DepthDatas[(int)Math.Round(Y) * 640 + (int)Math.Round(X)];
            float conf= SkeletonDatas[jointIndex * 3 + 2];

            return new JointData(X, Y, Convert.ToSingle(depth),conf );
        }
    }
}
