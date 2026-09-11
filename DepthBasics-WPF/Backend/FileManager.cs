using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using System.Net.NetworkInformation;

namespace Microsoft.Samples.Kinect.DepthBasics.Backend
{
    internal class FileManager
    {
        const string SAVINGPATH = "frames";


        const int WIDTH = 640; 
        const int HEIGHT = 480;

        const int CURRENT_MAX_IMG_ID = 872;


        public int ImageCountToSave { get; private set; }

        public FileManager()
        {
            if (!Directory.Exists(SAVINGPATH))
            {
                Directory.CreateDirectory(SAVINGPATH);
            }


            ImageCountToSave = 4;
        }
        public void SaveColorFrameData(int frameIndex, byte[] colorPixels, string path)
        {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            //}
            Bitmap colorBitmap = new Bitmap(WIDTH, HEIGHT, System.Drawing.Imaging.PixelFormat.Format32bppRgb);

            var bmpData = colorBitmap.LockBits(new System.Drawing.Rectangle
                (0, 0, WIDTH, HEIGHT),
                System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);

            System.Runtime.InteropServices.Marshal.Copy(colorPixels, 0, bmpData.Scan0, colorPixels.Length);
            colorBitmap.UnlockBits(bmpData);
            string filePath = Path.Combine(path, $"rgb_{ frameIndex:D4}.png");

            colorBitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
        }

        public void SaveDepthFrameData(int frameIndex, DepthImagePixel[] depthPixels, string path)
        {
            using (BinaryWriter writer = new BinaryWriter(File.Open($"{path}\\depth_{frameIndex:D4}.bin", FileMode.Create)))
            {
                foreach (var depth in depthPixels)
                {
                    writer.Write(depth.Depth);
                }
            }
        }
        public List<byte[]> AllColorPixelData(string filePath)
        {
            List<byte[]> motionSequence = new List<byte[]>();
            byte[] data;
            string path = string.Empty;
            for (int i = 0; i < CURRENT_MAX_IMG_ID; i += 4)
            {
                try
                {
                    path = $"{filePath}\\rgb_{i:D4}.png";
                    data = LoadColorPixelsFromImage(path);
                    motionSequence.Add(data);
                    Console.WriteLine("Sikeres beolvasás");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Hiba betöltésnél: " + path);
                }
            }

            return motionSequence;  
        }
        public byte[] LoadColorPixelsFromImage(string filePath)
        {

            try
            {
                bool letezik = File.Exists(filePath);
                Bitmap bitmap = new Bitmap(filePath);
                byte[] colorPixels = new byte[WIDTH * HEIGHT * 4];

                BitmapData bitmapData = bitmap.LockBits(
                    new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppRgb);

                System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, colorPixels, 0, colorPixels.Length);
                bitmap.UnlockBits(bitmapData);

                return colorPixels;
            }
            catch (Exception e )
            {

                Console.WriteLine("Memória Probléma: "+ e.Message);
            }
            return null;
            
        }

        public List<short[]> AllDepthData(string filePath)
        {
            List<short[]> depthSequence = new List<short[]>();
            short[] data;
            string path = string.Empty;
            for (int i = 0; i < 400; i += 4)
            {
                try
                {
                    path = $"{filePath}\\depth_{i:D4}.bin";
                    data = LoadDepthPixelsFromBinary(path);
                    depthSequence.Add(data); 
                    //"C:\Users\Legion\Documents\Projektmunka\DepthBasics-WPF\bin\Debug\frames\bent-over_rows\depth_0028.bin"
                    Console.WriteLine("Sikeres beolvasás");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Hiba betöltésnél: " + path);
                }
            }

            return depthSequence;
        }

        public short[] LoadDepthPixelsFromBinary(string filePath)
        {
            short[] depthPixels = new short[WIDTH * HEIGHT];
            using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
            {
                for (int i = 0; i < depthPixels.Length; i++)
                {
                    depthPixels[i] = Convert.ToInt16(reader.ReadInt16());
                }
            }
            return depthPixels;
        }
    }
}
