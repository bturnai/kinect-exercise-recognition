using Microsoft.Samples.Kinect.DepthBasics.Models;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.DepthBasics.ExerciseAnalyzers
{
    internal class SquatSideAnalyzer : IAnalyzer
    {
        List<ImageWithDepth> video;
        string side;
        public ResultClass Result { get; set; }

        public SquatSideAnalyzer(List<ImageWithDepth> video)
        {
            Result = new ResultClass();
            AnalyzeHelper.Video = video;
            this.video = video;
            side = AnalyzeHelper.DecideSide();
            Result.Exercise = "Guggolás";

            SpeedCheck();
        }

        //private void CountReps()
        //{
        //    int reps;
        //    if (side == "left")
        //    {
        //        reps = AnalyzeHelper.CountReps(OpenposeJointType.LHip);

        //    }
        //    else
        //    {
        //        reps = AnalyzeHelper.CountReps(OpenposeJointType.RHip);
        //    }



        //    Result.Analitics.Add($"{reps} ismétlést csináltál");

        //    SpeedCheck();

        //}
        private void SpeedCheck()
        {
            bool isSpeeding;
            if (side == "left")
                isSpeeding = AnalyzeHelper.IsSpeeding(OpenposeJointType.LHip, 1000);
            else
                isSpeeding = AnalyzeHelper.IsSpeeding(OpenposeJointType.RHip, 1000);


            if (isSpeeding)
                Result.Analitics.Add("Túl gyosan csinálod a gyakorlatot!");
            else
                Result.Analitics.Add("Megfelelő a gyakorlat sebessége");


            AnglesCheck();
        }

        private void AnglesCheck()
        {
            bool hipAngleIsSmaller;
            bool kneeMinBiggerThan;
            if (side=="left")
            {
                hipAngleIsSmaller= AnalyzeHelper.AngleMinSmallerThanCheck(OpenposeJointType.LHip, OpenposeJointType.LKnee, OpenposeJointType.Neck, 45);
                kneeMinBiggerThan = AnalyzeHelper.AngleBiggerThanCheck(OpenposeJointType.LKnee, OpenposeJointType.LKnee, OpenposeJointType.LHip,40);
            }
            else
            {
                hipAngleIsSmaller= AnalyzeHelper.AngleMinSmallerThanCheck(OpenposeJointType.RHip, OpenposeJointType.RKnee, OpenposeJointType.Neck, 45);
                kneeMinBiggerThan = AnalyzeHelper.AngleBiggerThanCheck(OpenposeJointType.RKnee, OpenposeJointType.RKnee, OpenposeJointType.RHip, 40);
            }

            if (hipAngleIsSmaller)
            {
                Result.Analitics.Add("Guggolás közben nagyon előre hajoltál, tartsd a hátadat egyenesen");
            }

            if (kneeMinBiggerThan)
            {
                Result.Analitics.Add("Nem guggoltál elég mélyre, így nem lesz elég hatékony a gyakorlat!");

            }
            else
            {
                Result.Analitics.Add("Jó a guugolás mélysége!");
            }

        }
    }
}
