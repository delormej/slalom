using System;
using System.Drawing;
using Accord.Video.FFMPEG;

namespace ImageExtractor
{
    class Program
    {
        static void Main(string[] args)
        {
            string file = args[0];
            double seconds = double.Parse(args[1]);

            // create instance of video reader
            VideoFileReader reader = new VideoFileReader( );
            // open video file
            //string file = @"\\files\video\GoPro Import\2018-07-09\HERO5 Black 3\GOPR0409.MP4";
            // @"..\..\backup\GOPR0188.MP4"
            reader.Open(file);

            //double seconds = 15.0;
            int frameIndex = (int)(reader.FrameRate.Value * seconds);
            Bitmap videoFrame = reader.ReadVideoFrame(frameIndex);

            //Console.WriteLine("Frames: {0}", reader.FrameCount);

            // read 100 video frames out of it
            //for ( int i = 0; i < reader.FrameCount; i+=100 )
            //{
                //(int)reader.FrameRate.Value * 41)
                //Bitmap videoFrame = reader.ReadVideoFrame();
                // process the frame somehow
                // ...
                videoFrame.Save(string.Format("GOPR0409-{0}.jpeg", frameIndex),
                    System.Drawing.Imaging.ImageFormat.Jpeg);
                // dispose the frame when it is no longer required
                videoFrame.Dispose( );
            //}
            reader.Close( );                
        }
    }
}
