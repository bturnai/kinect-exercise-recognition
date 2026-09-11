using Accord.MachineLearning;
using Microsoft.Samples.Kinect.DepthBasics.Models;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Accord.MachineLearning;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.DepthBasics.Logic
{
    internal class KMeansClassifier
    {

        public string DetermineDirection(List<ImageWithDepth> video)
        {
            // Minden frame pontjait tároljuk
            var allPoints = new List<double[]>();

            foreach (var frame in video)
            {
                // Csak releváns kulcspontokat gyűjtünk (pl. vállak és csípő)
                var leftShoulder = frame.GetJoint((int)OpenposeJointType.LShoulder);
                var rightShoulder = frame.GetJoint((int)OpenposeJointType.RShoulder);
                var midHip = frame.GetJoint((int)OpenposeJointType.MidHip);

                allPoints.Add(new double[] { leftShoulder.X, leftShoulder.Y });
                allPoints.Add(new double[] { rightShoulder.X, rightShoulder.Y });
                allPoints.Add(new double[] { midHip.X, midHip.Y });
            }

            // K-means clustering (2 csoport: balra/jobbra)
            var kmeans = new KMeans(k: 2);
            var clusters = kmeans.Learn(allPoints.ToArray());
            int[] labels = clusters.Decide(allPoints.ToArray());

            // Jobbra vagy balra fordulás meghatározása
            int leftClusterCount = labels.Count(label => label == 0);
            int rightClusterCount = labels.Count(label => label == 1);

            // Ha a "balra" klaszterben több pont van, akkor balra néz
            return leftClusterCount > rightClusterCount ? "left" : "right";
        }

    }
}
