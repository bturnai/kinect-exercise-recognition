using Microsoft.Samples.Kinect.DepthBasics.Models;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.DepthBasics.ExerciseAnalyzers
{
    interface IAnalyzer
    {
        ResultClass Result { get; set; }    }
}
