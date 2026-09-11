using Accord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.DepthBasics.Models
{
    public class JointData
    {
        static float veloMultiplier = 1/ (4f / 30);
        public JointData(float x, float y, float depth)
        {
            X = x;
            Y = y;
            Depth = depth;
        }
        public JointData(float x, float y, float depth, float confidece) : this(x, y, depth)
        {
            Confidence= confidece;
        }

        public float X { get; set; }
        public float Y { get; set; }
        public float Depth { get; set; }
        public float Confidence { get; set; }




         
        public double CalculateAngle(JointData startJoint, JointData endJoint)
        {
            // Vektorok számítása
            float[] vectorA = { startJoint.X- this.X , startJoint.Y -this.Y  };
            float[] vectorB = { endJoint.X - this.X , endJoint.Y-this.Y };

            double dotProduct = vectorA[0] * vectorB[0] + vectorA[1] * vectorB[1];
            //double dotProduct = vectorA[0] * vectorB[0] + vectorA[1] * vectorB[1] + vectorA[2] * vectorB[2];
            // Vektor hossza
            double magnitudeA = Math.Sqrt(Math.Pow(vectorA[0], 2) + Math.Pow(vectorA[1], 2) );
            double magnitudeB = Math.Sqrt(Math.Pow(vectorB[0], 2) + Math.Pow(vectorB[1], 2) );
            //double magnitudeA = Math.Sqrt(Math.Pow(vectorA[0], 2) + Math.Pow(vectorA[1], 2) + Math.Pow(vectorA[2], 2));
            //double magnitudeB = Math.Sqrt(Math.Pow(vectorB[0], 2) + Math.Pow(vectorB[1], 2) + Math.Pow(vectorB[2], 2));
            
            // Szög számítása radiánban
            double radian= Math.Acos(dotProduct / (magnitudeA * magnitudeB));
            return radian * (180.0 / Math.PI);

        }

        public double CalculateSpeed(JointData pointFrom, double deltaTime = (4d/30))
        {
            double distance = Math.Sqrt(
            Math.Pow(this.X - pointFrom.X, 2) + // x tengely
            Math.Pow(this.Y - pointFrom.Y, 2)  // y tengely
            //+ Math.Pow(this.Depth - point1.Depth, 2)   // depth (z tengely)
            );
            return distance / deltaTime;
        }

        public double CalculateDistance(JointData jointFrom)
        {
            return Math.Sqrt(
                Math.Pow((this.X-jointFrom.X), 2) 
                +
                Math.Pow((this.Y-jointFrom.Y), 2)
                );
        }
    }
}
