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
    internal class SholderPressAnalyzer: IAnalyzer
    {
        List<ImageWithDepth> video;

        public ResultClass Result { get; set; }

        public SholderPressAnalyzer(List<ImageWithDepth> video)
        {
            Result = new ResultClass();
            AnalyzeHelper.Video = video;
            this.video = video;
            Result.Exercise = "Vállból nyomás";

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
            bool leftIsSpeeding = AnalyzeHelper.IsSpeeding(OpenposeJointType.LWrist);
            bool rightIsSpeeding = AnalyzeHelper.IsSpeeding(OpenposeJointType.RWrist);

            if (leftIsSpeeding||rightIsSpeeding)
            {
                Result.Analitics.Add("Túl gyorsan mozgatod a súlyt! Válassz nagyobbat, vagy lassítsd le a mozgást!");
            }
            else
            {
                Result.Analitics.Add("Megfelelő a gyakorlat sebessége!");
            }

            ElbowPositionCheck();
        }

        private void ElbowPositionCheck()
        {

            List<int> positions = new List<int>();


            var idx = video
                .Select((t, index) => new { Index = index, Y = t.GetJoint((int)OpenposeJointType.LElbow).Y })
                .OrderByDescending(t => t.Y)
                .First()
                .Index;


            float left = video[idx].GetJoint((int)OpenposeJointType.LElbow).Depth;
            float right = video[idx].GetJoint((int)OpenposeJointType.RElbow).Depth;
            float neck= video[idx].GetJoint((int)OpenposeJointType.Neck).Depth;



            if ((neck - left) < 60 || (neck - right) < 60)
                Result.Analitics.Add("Hozd előrébb a könyöködet, úgy jobban terheli az elülső vállizmot!");
            else
                Result.Analitics.Add("Jól tartod a kezedet");



            ArmAnglesCheck();

        }

        private void ArmAnglesCheck()
        {
            bool leftArmIsBad=AnalyzeHelper.AngleMinSmallerThanCheck(OpenposeJointType.Neck, OpenposeJointType.LShoulder, OpenposeJointType.MidHip, 75);
            bool rightArmIsBad=AnalyzeHelper.AngleMinSmallerThanCheck(OpenposeJointType.Neck, OpenposeJointType.RShoulder, OpenposeJointType.MidHip, 75);


            if (leftArmIsBad || rightArmIsBad)
                Result.Analitics.Add("Túlságosan levitted a könyöködet, ez nem jó a válladnak. Felkarod a mozdulat legalján legfeljebb 90 fokban legyen a testeddel!");
            else
                Result.Analitics.Add("Szépen csináltad a gyakorlatot, nem vitted túl mélyre a gyakorlatot.");
        }
    }
}
