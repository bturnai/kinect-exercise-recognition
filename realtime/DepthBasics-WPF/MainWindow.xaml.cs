//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.DepthBasics
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;

    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Windows.Media.Media3D;
    using System.Drawing;
    using System.Windows.Media;
    using System.Diagnostics;
    using System.Windows.Documents;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Timers;
    using System.Windows.Forms;
    using System.Windows.Threading;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        #region  Properties
        private KinectSensor sensor;
        /// Bitmap that will hold color information        
        private WriteableBitmap colorBitmap;
        private WriteableBitmap depthBitmap;

        //ebben fogjuk eltárolni a csontvázadatokat mint "videó"
        
        FrameManager frameManager;
        bool recordingInProgress = false;
        DispatcherTimer timer;
        int secondsRemaining;
        int secondsLeftRecording;
        //int frameIndex;
        /// Intermediate storage for the depth data received from the camera
        private DepthImagePixel[] depthPixels;

        DataAnalyzer analyzer;
        /// Intermediate storage for the depth data converted to color
        private byte[] colorPixels;

        #endregion
        public MainWindow()
        {
            InitializeComponent();
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            SearchSensor();

            if (null != this.sensor)
            {

                InitProperties();
               
            }

            
        }


        private void Sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {

            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null && depthFrame != null)
                {
                    colorFrame.CopyPixelDataTo(this.colorPixels);
                    depthFrame.CopyDepthImagePixelDataTo(this.depthPixels);


                    if (recordingInProgress)
                    {
                        
                        frameManager.EnqueueSave(colorPixels, depthPixels);
                        // Helyette egy mentett kép betöltése


                    }

                    this.colorBitmap.WritePixels(
                         new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                         this.colorPixels,
                         this.colorBitmap.PixelWidth * sizeof(int),
                         0);


                }
            }


        }


        private void ButtonRecordingClick(object sender, RoutedEventArgs e)
        {
            secondsRemaining = 5;
            secondsLeftRecording=11;
            InicializeTimer();
            listBox_result.ItemsSource = null;
            lb_exercise.Content = string.Empty;

        }

        private void InicializeTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (secondsRemaining>0)
            {
                lb_secondsToRecording.Visibility = Visibility.Visible;
                lb_secondsToRecording.Content = $"Recording starts in {secondsRemaining--} seconds";
                
            }
            else if (secondsLeftRecording-- ==0)
            {
                //lb_secondsToRecording.Visibility = Visibility.Collapsed;
                StopRecording();
            }
            else  if (!recordingInProgress)
            {

                //frameManager = new FrameManager();
                StartRecording();
                
            }

            
            
        }

        private void StartRecording()
        {

            frameManager = new FrameManager();
            frameManager.StartSavingThread();
            recordingInProgress = true;

            // Felület frissítése
            EditDisplay(Visibility.Collapsed,Visibility.Collapsed, Visibility.Visible, System.Windows.Media.Brushes.Red, 10);

        }

        private void buttonStopRecording_Click(object sender, RoutedEventArgs e)
        {
            StopRecording();

        }

        private void StopRecording()
        {
            EditDisplay(Visibility.Collapsed,Visibility.Visible, Visibility.Collapsed, System.Windows.Media.Brushes.Gray, 0);
            recordingInProgress = false;
            frameManager.StopSavingThread();

            timer.Stop();
            analyzer = new DataAnalyzer(frameManager);

            
            var results= analyzer.AnalyzeRecording();
            listBox_result.ItemsSource= results.Analitics;
            lb_exercise.Content = results.Exercise;
            
        }

        private void EditDisplay(Visibility visible, Visibility startButtonVisibility, Visibility stopButtonVisibility, SolidColorBrush color, int thickness)
        {
            lb_secondsToRecording.Visibility = visible;
             buttonStartRecording.Visibility = startButtonVisibility;
            buttonStopRecording.Visibility = stopButtonVisibility;
            recordingBorder.BorderBrush = color;
            recordingBorder.BorderThickness = new Thickness(thickness);
        }


        private void InitProperties()
        {
            // Turn on the depth stream to receive depth frames
            this.sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            this.sensor.SkeletonStream.Enable();
            

            // Allocate space to put the depth pixels we'll receive
            this.depthPixels = new DepthImagePixel[this.sensor.DepthStream.FramePixelDataLength];

            // Allocate space to put the color pixels we'll create
            this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];

            // This is the bitmap we'll display on-screen
            this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
            this.depthBitmap = new WriteableBitmap(this.sensor.DepthStream.FrameWidth, this.sensor.DepthStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

            //IMage to show
            this.Image.Source = this.colorBitmap;

            this.sensor.AllFramesReady += Sensor_AllFramesReady;
            //sensor.SkeletonFrameReady += Sensor_SkeletonFrameReady;

            frameManager= new FrameManager();
            //stopwatch = new Stopwatch();


            try
            {
                this.sensor.Start();
            }
            catch (IOException)
            {
                this.sensor = null;
            }
        }


        private void SearchSensor()
        {
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }
        }
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
            frameManager.FinishAnalyse();
            analyzer.FinishAnalyze();


        }


    }
}