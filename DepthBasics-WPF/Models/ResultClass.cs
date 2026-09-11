using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.DepthBasics.Models
{
    internal class ResultClass
    {

        public List<string> Analitics { get; set; }
        //public int MyProperty { get; set; }
        public string Exercise { get; set; }
        public ResultClass()
        {

            Analitics = new List<string>();

        }

    }
}
