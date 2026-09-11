using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Samples.Kinect.DepthBasics.Logic
{
    public class PoseResponse
    {
        public float[][][] PoseKeypoints { get; set; }
        //public string CvOutputData { get; set; }

        static int Size = 25 * 3;

        public float[] KeypointsToVector()
        {
            float[] flatArray = new float[Size];
            int index = 0;

            // Töltsük fel az egydimenziós tömböt
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
