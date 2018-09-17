using System.Drawing;
using Accord.Video.FFMPEG;

namespace CreatePreviewService
{
    public class PreviewImage
    {
        public static string Create(string mp4Path, string qualifier, double seconds)
        {
            string imgPath = mp4Path.Replace(".MP4", qualifier) + ".png";

            VideoFileReader reader = new VideoFileReader();
            reader.Open(mp4Path);
            int frameIndex = (int)(reader.FrameRate.Value * seconds);

            Bitmap videoFrame = reader.ReadVideoFrame(frameIndex);
            videoFrame.Save(imgPath, System.Drawing.Imaging.ImageFormat.Jpeg);
            videoFrame.Dispose();

            reader.Close();

            return imgPath;
        }
    }
}
