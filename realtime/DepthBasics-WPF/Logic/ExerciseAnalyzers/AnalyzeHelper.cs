using Accord.Math;
using Microsoft.Samples.Kinect.DepthBasics.Logic;
using Microsoft.Samples.Kinect.DepthBasics.Models;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Microsoft.Samples.Kinect.DepthBasics.ExerciseAnalyzers
{
    internal static class AnalyzeHelper
    {
        public static List<ImageWithDepth> Video{ get; set; }
        public static bool IsSpeeding(OpenposeJointType joint, int speedLimit =2000)
        {

            double[] speeds = new double[Video.Count - 1];
            for (int i = 0; i < Video.Count - 1; i++)
            {
                JointData left1 = Video[i].GetJoint((int)joint);
                JointData left2 = Video[i + 1].GetJoint((int)joint);

                speeds[i] = left2.CalculateSpeed(left1);
            }

            return speeds.Max() > speedLimit;
        }

        //public static int CountReps(OpenposeJointType joint)
        //{
        //    float now = Video[0].GetJoint((int)joint).Y;
        //    float next= Video[1].GetJoint((int)joint).Y;
        //    int down=0;
        //    int up=0;
        //    float direction = next - now;
        //    bool was = false;
        //    for (int i = 1; i < Video.Count-2;)
        //    {
        //        direction = next - now;
        //        while (direction >= 0 && i < Video.Count - 2)
        //        {
        //            now = Video[i].GetJoint((int)joint).Y;
        //            next = Video[i + 1].GetJoint((int)joint).Y;
        //            direction = next - now;
        //            if (!was )
        //            {
        //                down++;
        //                was = true;
        //            }
        //            i++;
        //        }
        //        was = false;
        //        while (direction < 0 && i < Video.Count - 2)
        //        {
        //            now = Video[i].GetJoint((int)joint).Y;
        //            next = Video[i + 1].GetJoint((int)joint).Y;
        //            direction = next - now;
        //            if (!was )
        //            {
        //                up++;
        //                was = true;
        //            }
        //            i++;
        //        }
        //        was = false;

        //    }


        //    if (up > down)
        //        return down;
        //    else
        //        return up;
        //}


        public static bool IsSwinging(OpenposeJointType joint= OpenposeJointType.Neck, int swingLimit=15)
        {
            int jointIdx = (int)joint;
            List<float> distances = new List<float>();
            for (int i = 0; i < Video.Count-2; i++)
            {
                var pos1 = Video[i].GetJoint(jointIdx).Depth;
                var pos2 = Video[i + 2].GetJoint(jointIdx).Depth;
                if (pos1 != 0 && pos2 != 0)
                {
                    distances.Add(Math.Abs(pos2 - pos1));

                }
            }
            return distances.Average()> swingLimit;

        }
        public static bool AngleMinSmallerThanCheck(OpenposeJointType center, OpenposeJointType side1, OpenposeJointType side2, int angleLimit)
        {
            JointData middle;
            JointData joint1;
            JointData joint2;

            List<double> angles = new List<double>();

            foreach (var frame in Video)
            {
                middle = frame.GetJoint((int)center);
                joint1 = frame.GetJoint((int)side1);
                joint2 = frame.GetJoint((int)side2);





                if (middle.Confidence>0 && joint1.Confidence>0 && joint2.Confidence>0)
                    angles.Add(middle.CalculateAngle(joint1, joint2));

            }

            return angles.Min()<angleLimit;
        }
        public static bool AngleAvgSmallerThanCheck(OpenposeJointType center, OpenposeJointType side1, OpenposeJointType side2, int angleLimit)
        {
            JointData middle;
            JointData joint1;
            JointData joint2;

            List<double> angles= new List<double>();

            foreach (var frame in Video)
            {
                middle = frame.GetJoint((int)center);
                joint1 = frame.GetJoint((int)side1);
                joint2 = frame.GetJoint((int)side2);



                if (middle.Confidence > 0 && joint1.Confidence > 0 && joint2.Confidence > 0)
                    angles.Add(middle.CalculateAngle(joint1, joint2));

            }


            return angles.Average()<angleLimit;
        }
        public static bool AngleBiggerThanCheck(OpenposeJointType center, OpenposeJointType side1, OpenposeJointType side2, int angleLimit)
        {
            JointData middle;
            JointData joint1;
            JointData joint2;

            List<double> angles = new List<double>();

            foreach (var frame in Video)
            {
                middle = frame.GetJoint((int)center);
                joint1 = frame.GetJoint((int)side1);
                joint2 = frame.GetJoint((int)side2);


                if (middle.Confidence > 0 && joint1.Confidence > 0 && joint2.Confidence > 0)
                    angles.Add(middle.CalculateAngle(joint1, joint2));
                    Console.WriteLine($"angle {angles[angles.Count-1]}");
            }

            return angles.Min()>angleLimit;
        }


        /// <summary>
        /// Decides whether the user stands with the left or the right side towards the camera,
        /// from the average horizontal offset between the neck and the mid-hip.
        /// Frames where either joint was not detected are skipped.
        /// </summary>
        public static string DecideSide()
        {
            double offset = 0;

            foreach (var frame in Video)
            {
                JointData neck = frame.GetJoint((int)OpenposeJointType.Neck);
                JointData midhip = frame.GetJoint((int)OpenposeJointType.MidHip);

                if ((neck.X == 0 && neck.Y == 0) || (midhip.X == 0 && midhip.Y == 0))
                {
                    continue;
                }

                offset += neck.X - midhip.X;
            }

            return offset > 0 ? "left" : "right";
        }
    }
}
