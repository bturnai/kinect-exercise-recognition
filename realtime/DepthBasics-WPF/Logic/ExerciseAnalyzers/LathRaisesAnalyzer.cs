using Microsoft.Samples.Kinect.DepthBasics.Models;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.DepthBasics.ExerciseAnalyzers
{

    internal class LathRaisesAnalyzer : IAnalyzer
    {
        List<ImageWithDepth> video;

        public ResultClass Result { get; set; }

        public LathRaisesAnalyzer(List<ImageWithDepth> video)
        {
            Result = new ResultClass();
            Result.Exercise = "Oldal elemés";
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
            bool leftWristSwing=AnalyzeHelper.IsSpeeding(OpenposeJointType.LWrist);
            bool rightWristSwing=AnalyzeHelper.IsSpeeding(OpenposeJointType.RWrist);

            if (leftWristSwing || rightWristSwing)
            {
                Result.Analitics.Add("Ne rángatsd a súlyt, lassítsd le a mozgást!");
            }
            else
            {
                Result.Analitics.Add("Jó a gyakorlat sebessége");

            }

            SwingCheck();
        }

        private void SwingCheck()
        {
            bool isSwinging= AnalyzeHelper.IsSwinging();

            if (isSwinging)
            {
                Result.Analitics.Add("Ne dőlj előre-hátra! Tartsd egyenesen a törzsed!");
            }
            else
            {
                Result.Analitics.Add("Szépen tartod a törzsedet!");
            }



            ArmAnglesCheck();
        }

        private void ArmAnglesCheck()
        {
            bool leftArmIsGoodAngle = AnalyzeHelper.AngleAvgSmallerThanCheck(OpenposeJointType.LElbow, OpenposeJointType.LShoulder, OpenposeJointType.LWrist, 150);
            bool rightArmIsGoodAngle = AnalyzeHelper.AngleAvgSmallerThanCheck(OpenposeJointType.RElbow, OpenposeJointType.RShoulder, OpenposeJointType.RWrist, 150);


            if (leftArmIsGoodAngle && rightArmIsGoodAngle)
            {
                Result.Analitics.Add("Jól tartod a karjaidat, stabilan tartod");
            }
            else
            {
                Result.Analitics.Add("A könyököd vigye a mozgást, ne mozogjon az alkarod, hanem tartsd stabilan a karjaidat");
            }
        }
    }
}
