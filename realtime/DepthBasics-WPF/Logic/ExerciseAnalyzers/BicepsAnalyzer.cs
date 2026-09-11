using Microsoft.Kinect;
using Microsoft.Samples.Kinect.DepthBasics.Models;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.DepthBasics.ExerciseAnalyzers
{
    internal class BicepsAnalyzer : IAnalyzer
    {
        private readonly List<ImageWithDepth> video;
        public ResultClass Result { get ; set; }



        public BicepsAnalyzer(List<ImageWithDepth> video)
        {
            Result = new ResultClass();
            Result.Exercise = "Bicepsz";
            this.video = video;

            AnalyzeHelper.Video = video;

            SpeedCheck();
        }

        //private void CountReps()
        //{

        //    int reps = AnalyzeHelper.CountReps(OpenposeJointType.LWrist);

        //    Result.Analitics.Add($"{reps} ismétlést csináltál");

        //    SpeedCheck();


        //}

        private void SpeedCheck()
        {

            bool leftSpeeding = AnalyzeHelper.IsSpeeding(OpenposeJointType.LWrist);
            bool rightSpeeding = AnalyzeHelper.IsSpeeding(OpenposeJointType.RWrist);

            if (leftSpeeding || rightSpeeding)
            {
                Result.Analitics.Add("Túlságosan gyorsan csinálod a gyakorlatot, engedd le vagy húzd fel lassabban.");

            }
            else
            {
                Result.Analitics.Add("Megfelelő a gyakorlat sebessége");
            }
            WideHoldingCheck();

        }

        private void WideHoldingCheck()
        {
            JointData leftWrist;
            JointData rightWrist;
            JointData leftShoulder;
            JointData rightShoulder;

            double[] distances=new double[video.Count];
            for (int i = 0; i < video.Count; i++)
            {
                leftWrist = video[i].GetJoint((int)OpenposeJointType.LWrist);
                rightWrist= video[i].GetJoint((int)OpenposeJointType.RWrist);
                leftShoulder= video[i].GetJoint((int)OpenposeJointType.LShoulder);
                rightShoulder= video[i].GetJoint((int)OpenposeJointType.RShoulder);

                var shoulderDistance= leftShoulder.CalculateDistance(rightShoulder);
                var wristDistance= leftWrist.CalculateDistance(rightWrist);

                distances[i] = shoulderDistance - wristDistance;

            }
            if (distances.Max()>10)
            {
                Result.Analitics.Add("Túl szélesen csinálod a gyakorlatot, a csuklód egy vonalba mozogjon a váladdal");
            }

            else
            {
                Result.Analitics.Add("Jól fogod a súlyt");
            }

            SwingCheck();


        }

        private void SwingCheck()
        {

            bool isSwinging = AnalyzeHelper.IsSwinging();
            if (isSwinging)
            {
                Result.Analitics.Add("Rángattad a súlyt, maradjon fix és egyenes a törzsed");
            }
            else
            {
                Result.Analitics.Add("Jól tartottad a tözsedet");
            }


            StandingCheck();
        }

        private void StandingCheck()
        {

            JointData leftFoot;
            JointData rightFoot;

            double[] distances = new double[video.Count - 1];
            for (int i = 0; i < distances.Length; i++)
            {
                leftFoot = video[i].GetJoint((int)OpenposeJointType.LAnkle);
                rightFoot = video[i].GetJoint((int)OpenposeJointType.RAnkle);

                if (leftFoot.Depth != 0 && rightFoot.Depth != 0)
                {
                    
                    distances[i] = Math.Abs(leftFoot.Depth - rightFoot.Depth);
                }
                else
                {
                    distances[i] = 151;
                }
            }

            if (distances.Min() < 150)
            {
                Result.Analitics.Add("Állj haránt terpeszbe, úgy stabilabb lesz a gyakorlat");
            }

            else 
            {
                Result.Analitics.Add("Jó a beállás!");
            }

        }
    }
}
