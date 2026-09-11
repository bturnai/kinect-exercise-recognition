    using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Accord.MachineLearning.VectorMachines;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Statistics.Kernels;
using Microsoft.Samples.Kinect.DepthBasics.Backend;
using Microsoft.Samples.Kinect.DepthBasics.ExerciseAnalyzers;
using Microsoft.Samples.Kinect.DepthBasics.Models;
using Newtonsoft.Json;

namespace Microsoft.Samples.Kinect.DepthBasics
{
    internal class DataAnalyzer
    {

        FrameManager frameManager;
        const int PORT = 2222;
         int frameIndex;

        //OpenPoseManager client;

        FileManager fileManager;
        OpenPoseClient openPoseClient;
        short[] depth;
        public DataAnalyzer(FrameManager manager)
        {
            frameManager = manager;
            this.frameIndex = manager.FrameIndex;
            //client = new OpenPoseManager(manager);
            this.frameManager = manager;
            openPoseClient = new OpenPoseClient("127.0.0.1", PORT );

            idx = 0;

        }


        int idx;
        public ResultClass AnalyzeRecording()
        {
            
            
            string json = frameManager.ConvertToVectorJson();
           


            int exercise = openPoseClient.GetExercise(json);
          

            ExerciseType exerciseType = (ExerciseType)exercise;
            IAnalyzer analyser ;
            switch (exerciseType)
            {
                case ExerciseType.BentOverRows:
                    analyser = new BOWAnalyzer(frameManager.Video);
                    break;

                case ExerciseType.Biceps:
                    analyser = new BicepsAnalyzer(frameManager.Video);
                    break;

                case ExerciseType.LatheralRaises:
                    analyser = new LathRaisesAnalyzer(frameManager.Video);
                    break;

                case ExerciseType.RDL:
                    analyser = new RDLAnalyzer(frameManager.Video);
                    break;

                case ExerciseType.ShoulderPress:
                    analyser = new SholderPressAnalyzer(frameManager.Video);
                    break;

                case ExerciseType.Squat:
                    analyser = new SquatSideAnalyzer(frameManager.Video);
                    break;

                default:
                    analyser = new BOWAnalyzer(frameManager.Video);
                    break;

            }

            PropertyInfo resultProp = analyser.GetType().GetProperty("Result");
            //PropertyInfo resultProp = analyser.GetType().GetProperty("Result");

            return analyser.Result as ResultClass;
        }

        public void FinishAnalyze()
        {
            openPoseClient.Disconnect();
        }

    }
}
