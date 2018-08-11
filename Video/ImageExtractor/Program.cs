using System;
using System.Drawing;
using Accord.Video.FFMPEG;

namespace ImageExtractor
{
    class Program
    {
        static void Main(string[] args)
        {
            // create instance of video reader
            VideoFileReader reader = new VideoFileReader( );
            // open video file
            reader.Open(@"..\..\backup\GOPR0188.MP4");

            Console.WriteLine("Frames: {0}", reader.FrameCount);

            // read 100 video frames out of it
            for ( int i = 0; i < reader.FrameCount; i+=100 )
            {
                
                Bitmap videoFrame = reader.ReadVideoFrame(i);
                // process the frame somehow
                // ...
                videoFrame.Save(string.Format("GOPR0188-{0}.jpeg", i),
                    System.Drawing.Imaging.ImageFormat.Jpeg);
                // dispose the frame when it is no longer required
                videoFrame.Dispose( );
            }
            reader.Close( );                
        }
    }
}
