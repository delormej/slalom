using System;
using FFmpeg.NET;
using FFmpeg.NET.Events;
using System.Threading;
using System.Threading.Tasks;

namespace SlalomTracker
{
    public class VideoTasks
    {
        FFmpeg.NET.Engine _ffmpeg;

        public VideoTasks()
        {
            _ffmpeg = new Engine("ffmpeg");
            _ffmpeg.Error += OnError;
        }

        public async Task<string> TrimAsync(string localVideoPath, double start, double length)
        {
            var inputFile = new MediaFile (localVideoPath);
            var outputFile = new MediaFile (AppendToFileName(localVideoPath, "_trimmed"));
            var options = new ConversionOptions();            
            options.CutMedia(TimeSpan.FromSeconds(start), TimeSpan.FromSeconds(length));   
            
            await _ffmpeg.ConvertAsync(inputFile, outputFile, options); 
            Console.WriteLine($"Converted: {outputFile.FileInfo.FullName}");
            
            return outputFile.FileInfo.FullName;
        }

        public async Task<string> RemoveAudioAsync(string inputFile)
        {
            string outputFile = AppendToFileName(inputFile, "_silent");
            string parameters = $"-i {inputFile} -c copy -an {outputFile}";
            await _ffmpeg.ExecuteAsync(parameters);
            Console.WriteLine($"Removed Audio: {outputFile}");
            
            return outputFile;
        }

        private void OnError(object sender, ConversionErrorEventArgs e)
        {
            Console.WriteLine("Trim video error: [{0} => {1}]: Error: {2}\n{3}", e.Input.FileInfo.Name, e.Output.FileInfo.Name, e.Exception.ExitCode, e.Exception.InnerException);
        }    

        private string AppendToFileName(string inputFile, string suffix)
        {
            int start = inputFile.LastIndexOf(".MP4");
            if (start <= 0)
                throw new ApplicationException($"Could not generate an output filename from {inputFile}.");

            string outputFile = $"{inputFile.Substring(0, start)}{suffix}.MP4";
            return outputFile;
        }    
    }
}
