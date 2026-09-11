using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Samples.Kinect.DepthBasics
{
    public class PoseResponse
    {
        public float[][][] PoseKeypoints { get; set; }


        public float[] PoseValues()
        {
            float[] flatArray = new float[100];
            int index = 0;

            foreach (var person in PoseKeypoints)
            {
                foreach (var keypoint in person)
                {
                    foreach (var coordinate in keypoint)
                    {
                        flatArray[index++] = coordinate;
                    }
                }
            }
            return flatArray;
        }
    }
}
