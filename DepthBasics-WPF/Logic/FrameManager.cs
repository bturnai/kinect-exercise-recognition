using Accord.IO;
using Accord.Statistics.Distributions.Univariate;
using Microsoft.Kinect;
using Microsoft.Samples.Kinect.DepthBasics.Backend;
using Microsoft.Samples.Kinect.DepthBasics.Models;
using Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Microsoft.Samples.Kinect.DepthBasics
{
    #region intro
    //ez az osztáy kezeli a kinecttől kapott adatok mentését és kiolvasását
    //frameIndex-t növeljük minden alkalommal amikor beejön egy elmentendő kép
    //és a végén ennek a hosszából tudjuk megadni hogy mekkora tömböt kell visszaalakítsunk
    #endregion
    public class FrameManager
    {

        //private BlockingCollection<(int frameIndex, byte[] colorPixels, short[] depthPixels)> saveQueue = new BlockingCollection<(int, byte[], short[])>();
        private BlockingCollection<(int frameIndex, byte[] colorPixels, DepthImagePixel[] depthPixels)> saveQueue;

        private bool isSaving = true;
        OpenPoseClient openPoseClient;
        object lockObject;


        const int IMAGEWIDTH = 640;
        public int FrameIndex { get; set; }
        public List<ImageWithDepth> Video { get => video; private set => video = value; }

        FileManager fileManager;


        List<ImageWithDepth> video;
        Task task;
        public FrameManager()
        {
            saveQueue = new BlockingCollection<(int, byte[], DepthImagePixel[])>();
            openPoseClient = new OpenPoseClient("127.0.0.1", 1111);
            FrameIndex = 0;


            fileManager = new FileManager();
            lockObject = new object();
        }

        public void EnqueueSave(byte[] colorPixels, DepthImagePixel[] depthPixels)
        {

            if (FrameIndex % fileManager.ImageCountToSave == 0)
            {

                saveQueue.Add((FrameIndex, (byte[])colorPixels.Clone(), (DepthImagePixel[])depthPixels.Clone()));
            }

            FrameIndex++;
        }


        public void StartSavingThread()
        {
            Video = new List<ImageWithDepth>();

            task = Task.Factory.StartNew(() =>
            {
                while (isSaving || saveQueue.Count > 0)
                {

                    try
                    {
                       var item = saveQueue.Take();
                        var depth = item.depthPixels.Select(dp => dp.Depth).ToArray();
                        GetJointsFromOpenpose(item.colorPixels, depth);
                        Console.WriteLine($"{item.frameIndex}.ik elem feldolgozásra került");

                    }
                    catch (InvalidOperationException)
                    {
                        Console.WriteLine("Sikertelen mentés");
                    }

                }
                Console.WriteLine($"Feldolgozás vége. Összes feldolgozott frame: {Video.Count}");

            });


            //openPoseClient.Disconnect();
        }

        public void GetJointsFromOpenpose(byte[] colorPixels, short[] depthPixels)
        {
            ImageWithDepth frame = new ImageWithDepth();

            frame.SkeletonDatas = openPoseClient.SendImageToOpenPose(colorPixels);
            frame.DepthDatas = depthPixels;

           

            if (frame.SkeletonDatas != null)
            {
                this.Video.Add(frame);
                Console.WriteLine("Sikeres Frame hozzáadása");
            }
            else
            {
                Console.WriteLine("Frame hozzáadása sikertelen");

            }
        }
        
        public async void StopSavingThread()
        {
            await Task.WhenAny(task);
            //return true;
        }

        public void FinishAnalyse()
        {

            isSaving = false;
            saveQueue.CompleteAdding();
            openPoseClient.Disconnect();
        }
        
        public string ConvertToStructuredJson(string videoName)
        {
            int dataCount = 3;
            
            var videoData = new
            {
                video_name = videoName,
                frames = Video.Select((frame, index) => new
                {
                    frame_number = index + 1,
                    keypoints = Enumerable.Range(0, frame.SkeletonDatas.Length / dataCount).Select(jointIndex =>
                    {
                        // Extract x and y for reuse
                        float x = frame.SkeletonDatas[jointIndex * dataCount];
                        float y = frame.SkeletonDatas[jointIndex * dataCount + 1];
                        short depth = frame.DepthDatas[(int)Math.Round(y) * IMAGEWIDTH + (int)Math.Round(x)];
                        float confidence = frame.SkeletonDatas[jointIndex * dataCount + 2];

                        return new
                        {
                            x, // Directly use x
                            y, // Directly use y
                            depth,
                            confidence
                        };
                    }).ToList()
                }).ToList()
            };

            return JsonConvert.SerializeObject(videoData, Formatting.Indented);
        }

        public string ConvertToVectorJson()
        {
            while (saveQueue.Count > 0)
            { 
                
            }
            var data = new List<List<List<List<float>>>>();
            var movement = new List<List<List<float>>>();

            for (int frameIdx = 0; frameIdx < video.Count - 1; frameIdx++)
            {
                List<List<float>> frameDatas = CalculateFrameVectors( frameIdx);
                movement.Add(frameDatas);

                if (movement.Count == 25)
                {
                    data.Add(movement);
                    movement = new List<List<List<float>>>();
                }
            }

            return JsonConvert.SerializeObject(data, Formatting.Indented);


        }
        List<List<float>> CalculateFrameVectors( int frameIdx)
        {
            ImageWithDepth frame1 = video[frameIdx];
            ImageWithDepth frame2 = video[frameIdx + 1];

            // A két kép közötti kulcspontokból vektorokat készít
            List<List<float>> frameDatas = new List<List<float>>();

            JointData keypoint1; ;
            JointData keypoint2;

            for (int i = 0; i < ImageWithDepth.JointCount; i++)
            {
                //JObject keypoint1 = (JObject)keypoints1[i];
                //JObject keypoint2 = (JObject)keypoints2[i];

                keypoint1= frame1.GetJoint(i);
                keypoint2 = frame2.GetJoint(i);

                List<float> vector = new List<float>
                {
                    (float)keypoint2.X - (float)keypoint1.X,
                    (float)keypoint2.Y - (float)keypoint1.Y
                };

                frameDatas.Add(vector);
            }

            return frameDatas;
        }
    }
}
