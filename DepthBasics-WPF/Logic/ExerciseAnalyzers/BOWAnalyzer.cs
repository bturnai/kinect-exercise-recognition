using Accord.Math;
using Microsoft.Samples.Kinect.DepthBasics.Models;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.DepthBasics.ExerciseAnalyzers
{
    internal class BOWAnalyzer : IAnalyzer
    {
        List<ImageWithDepth> video;
        
        int resultIndex;
        int[] rightSide = new int[] { 2, 3, 4, 9, 10, 11};
        int[] leftSide = new int[] { 5, 6, 7, 12,  13, 14 };
        
        double speedLimit = 1.5;


        string side;

        public ResultClass Result { get; set; }

        public BOWAnalyzer(List<ImageWithDepth> video)
        {
            Result = new ResultClass();
            AnalyzeHelper.Video = video;
            Result.Exercise = "Döntött törzsű evezés";
            this.video = video;
            DecideSide();
            SpeedCheck();
        }

        //private void CountReps()
        //{
        //    int reps;
        //    if (side=="left")
        //    {
        //        reps = AnalyzeHelper.CountReps(OpenposeJointType.LWrist);

        //    }
        //    else
        //    {
        //        reps= AnalyzeHelper.CountReps(OpenposeJointType.RWrist);
        //    }



        //    Result.Analitics.Add($"{reps} ismétlést csináltál");

        //    CalculateVelocity();

        //}
        private void DecideSide()
        {
            side= AnalyzeHelper .DecideSide();
        }

        private void SpeedCheck()
        {
            OpenposeJointType relevantJointIndex;

            if (side=="left")
            {
                relevantJointIndex = OpenposeJointType.LWrist;
            }
            else
            {
                relevantJointIndex = OpenposeJointType.RWrist;
            }
            bool isSpeeding = AnalyzeHelper.IsSpeeding(relevantJointIndex);
            if (isSpeeding)
            {
                Result.Analitics.Add("Túlságosan gyorsan csinálod a gyakorlatot, engedd le vagy húzd fel lassabban.");
            }
            else
            {
                Result.Analitics.Add("Megfelelő a gyakorlat sebessége");

            }



            CheckAngles();
        }

        private void CheckAngles()
        {
            OpenposeJointType[] hipAngle;
            OpenposeJointType[] kneeAngle;
            if (side=="left")
            {
                hipAngle = new OpenposeJointType[3] { OpenposeJointType.Neck, OpenposeJointType.LHip, OpenposeJointType.LKnee };
                kneeAngle= new OpenposeJointType[3] { OpenposeJointType.LHip, OpenposeJointType.LKnee, OpenposeJointType.LAnkle };
            }
            else
            {
                hipAngle = new OpenposeJointType[3] { OpenposeJointType.Neck, OpenposeJointType.RHip, OpenposeJointType.RKnee };
                kneeAngle= new OpenposeJointType[3] { OpenposeJointType.RHip, OpenposeJointType.RKnee, OpenposeJointType.RAnkle };

            }
            bool kneeIsBadAngle = AnalyzeHelper.AngleAvgSmallerThanCheck(kneeAngle[0], kneeAngle[1], kneeAngle[2], 160);
            bool hipIsBadAngle = AnalyzeHelper.AngleAvgSmallerThanCheck(hipAngle[0], hipAngle[1], hipAngle[2], 90);

            if (kneeIsBadAngle)
            {
                
                Result.Analitics.Add("Volt, hogy nagyon behajlítottad a lábad, kicsit nyújtsd ki és tartsd meg így!");
            }
            else
            {
                Result.Analitics.Add("Jól tartod a lábadat");
            }

            if (hipIsBadAngle)
            {
                Result.Analitics.Add("Volt, hogy nagyon behajloltál, ez nem jó a hátadnak. Kicsit emeld feljebb a hátadat!");

            }
            else
            {
                Result.Analitics.Add("Szépen tartod a hátadat!");
            }

        }
    }
}
