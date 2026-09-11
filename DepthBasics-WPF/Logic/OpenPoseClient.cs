using System.Net.Sockets;
using System;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Linq;
using Microsoft.Samples.Kinect.DepthBasics;
using System.Threading;
using Accord.Statistics.Distributions.Univariate;
using System.Windows.Markup;
using Newtonsoft.Json;
using System.Text;
using System.Reflection;
using Microsoft.Samples.Kinect.DepthBasics;
using Microsoft.Samples.Kinect.DepthBasics.Logic;

public class OpenPoseClient
{
    private string host;
    private int port;
    TcpClient client;
    NetworkStream stream;
    public OpenPoseClient(string host, int port)
    {
        this.host = host;
        this.port = port;
        ConnectToServer(host, port);
        stream = client.GetStream();

    }
    private void ConnectToServer(string host, int port)
    {
        while (client == null)
        {
            try
            {
                client = new TcpClient(host, port);

            }
            catch (Exception)
            {

                Thread.Sleep(100);
                Console.WriteLine("C#: Nincs kapcsolat");
            }
        }
    }
    public void Disconnect()
    {
        stream.Close();
        client.Close();
        Console.WriteLine("Kapcsolat lezárva.");


    }



    public float[] SendImageToOpenPose(byte[] colorPixels)
    {
        SendDatas(colorPixels);
        // Olvasd vissza az eredmény hosszát

        int responseLength = GetLenght();
        Console.WriteLine("2. responselenght: " +responseLength);
        //Console.WriteLine($"C#: Fogadott hossz prefiksz: {responseLength}");
        if (responseLength > 0)
        {
            PoseResponse response;
            try
            {
                // Adat fogadása
                byte[] responseBuffer = ReadDatas(responseLength);// JSON válasz feldolgozása
                string jsonResponse = Encoding.UTF8.GetString(responseBuffer);

                response = JsonConvert.DeserializeObject<PoseResponse>(jsonResponse);
                Console.WriteLine("Mentendő pontok: " + response.PoseKeypoints);
                //if (!string.IsNullOrEmpty(response.CvOutputData))
                //{
                //    byte[] imageBytes = Convert.FromBase64String(response.CvOutputData);
                //    File.WriteAllBytes("cvOutputImage.jpg", imageBytes);
                //    Console.WriteLine("cvOutputImage.jpg mentve");
                //}
            }
            catch (Exception ex)
            {

                throw;
            }
            if (response.PoseKeypoints == null)
            {
                return null;
            }

            return response.KeypointsToVector();
        }

        Console.WriteLine("C#: Üres válasz érkezett.");
        return null;
    }

    private byte[] ReadDatas(int responseLength)
    {
        byte[] messageBuffer = new byte[responseLength];
        int totalRead = 0;

        while (totalRead < responseLength)
        {
            int read = stream.Read(messageBuffer, totalRead, responseLength - totalRead);
            if (read == 0)
                throw new Exception("Connection closed.");
            totalRead += read;
        }
        return messageBuffer;
        

    }

    private int GetLenght()
    {
        byte[] responseLengthBuffer = new byte[4];
        int totalBytesRead = 0;

        while (totalBytesRead < 4)
        {
            int bytesRead = stream.Read(responseLengthBuffer, totalBytesRead, 4 - totalBytesRead);
            if (bytesRead == 0)
            {
                 throw new Exception("Kapcsolat megszakadt hossz prefiksz fogadása közben.");
            }
            totalBytesRead += bytesRead;
        }
        Array.Reverse(responseLengthBuffer);
        return BitConverter.ToInt32(responseLengthBuffer, 0);

    }

    private void SendDatas(byte[] colorPixels)
    { 
        // Küldd el a képadatokat
        byte[] lengthPrefix = BitConverter.GetBytes(colorPixels.Length);
        Array.Reverse(lengthPrefix);
        stream.Write(lengthPrefix, 0, lengthPrefix.Length);
        stream.Write(colorPixels, 0, colorPixels.Length);

    }

    public int GetExercise(string jsonData)
    {
        SendDataToClassifier(jsonData);
        return GetLenght();
    }


    public void SendDataToClassifier(string datas)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(datas);

        // Írjuk le a JSON string hosszát, hogy a szerver oldalon tudjuk, mekkora adatot kell várni
        byte[] lengthPrefix = BitConverter.GetBytes(buffer.Length);
        Array.Reverse(lengthPrefix); // Biztosítsuk a big-endian sorrendet
        stream.Write(lengthPrefix, 0, lengthPrefix.Length);

        // Küldjük el a JSON adatokat
        stream.Write(buffer, 0, buffer.Length);


    }
}
