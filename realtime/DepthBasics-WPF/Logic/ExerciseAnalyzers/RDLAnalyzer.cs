using Microsoft.Samples.Kinect.DepthBasics.Models;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.DepthBasics.ExerciseAnalyzers
{
    internal class RDLAnalyzer: IAnalyzer
    {
        List<ImageWithDepth> video;
        string side;

        public ResultClass Result { get; set; }

        public RDLAnalyzer(List<ImageWithDepth> video)
        {
            Result = new ResultClass();
            this.video = video;
            //Results = new List<string>();
            Result.Exercise = "Merevlábas felhúzás";
            AnalyzeHelper.Video = video;
            DecideSide();
            SpeedCheck();

        }

        private void DecideSide()
        {
            side= AnalyzeHelper.DecideSide();
        }

        //private void CountReps()
        //{
        //    int reps;
        //    if (side == "left")
        //    {
        //        reps = AnalyzeHelper.CountReps(OpenposeJointType.LShoulder);


        //    }
        //    else
        //    {
        //        reps = AnalyzeHelper.CountReps(OpenposeJointType.RShoulder);

        //        var valami = AnalyzeHelper.CountReps(OpenposeJointType.Neck);
        //    }



        //    Result.Analitics.Add($"{reps} ismétlést csináltál");

        //    SpeedCheck();

        //}
        private void SpeedCheck()
        {
            bool isSpeeding;
            if (side=="left")
            {
                isSpeeding= AnalyzeHelper.IsSpeeding(OpenposeJointType.LShoulder);

            }
            else
            {
                isSpeeding= AnalyzeHelper.IsSpeeding(OpenposeJointType.RShoulder);
            }

            if (isSpeeding)
            {
                Result.Analitics.Add("Túl gyorsan csinálod a mozgást, lassítsd le!");
            }
            else
            {
                Result.Analitics.Add("Megfelelő a gyakorlat sebessége!");
            }
            AngleCheck();
        }

        private void AngleCheck()
        {
            bool kneeIsBadAngle;
            bool hipIsBadAngle;
            if (side =="left")
            {
                kneeIsBadAngle = AnalyzeHelper.AngleAvgSmallerThanCheck(OpenposeJointType.LKnee, OpenposeJointType.LHip, OpenposeJointType.LAnkle, 160);
                hipIsBadAngle = AnalyzeHelper.AngleMinSmallerThanCheck(OpenposeJointType.LHip, OpenposeJointType.Neck, OpenposeJointType.LAnkle, 75);

            }
            else
            {
                kneeIsBadAngle = AnalyzeHelper.AngleAvgSmallerThanCheck(OpenposeJointType.RKnee, OpenposeJointType.RHip, OpenposeJointType.RAnkle, 160);
                hipIsBadAngle = AnalyzeHelper.AngleMinSmallerThanCheck(OpenposeJointType.RHip, OpenposeJointType.Neck, OpenposeJointType.RAnkle, 75);

            }


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
