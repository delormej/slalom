using System;
using System.IO;
using FFmpeg.NET;
using FFmpeg.NET.Events;
using System.Threading.Tasks;
using System.Diagnostics;
using Logger = jasondel.Tools.Logger;

namespace SlalomTracker
{
    public class VideoTasks
    {
        public const string DefaultVideoRecordingTimeZone = "America/New_York";
        private readonly TimeZoneInfo _videoTimeZone;

        FFmpeg.NET.Engine _ffmpeg;

        int _fileOutputIndex = 0;

        string _localVideoPath;

        public VideoTasks(string localVideoPath, 
            string videoTimeZone = DefaultVideoRecordingTimeZone)
        {
            _ffmpeg = new Engine("ffmpeg");
            _ffmpeg.Progress += OnProgress;
            _ffmpeg.Error += OnError;
            _localVideoPath = localVideoPath;
            _videoTimeZone = TimeZoneInfo.FindSystemTimeZoneById(videoTimeZone);
        }

        public async Task<string> CombineVideoAsync(string video2Path)
        {
            // -itsoffset 0.2 <-- use this param in front of the video you what to offset the start time of video (e.g. 0.2 seconds)
            Logger.Log($"Combining {_localVideoPath} with {video2Path}");
            string outputPath = GetCombinedVideoPath(video2Path);
            string arguments = $"-i {_localVideoPath} -i {video2Path} " +
                "-filter_complex \"[1]format=yuva444p,colorchannelmixer=aa=0.5[in2]; " +
                $"[in2][0]scale2ref[in2][in1];[in1][in2]overlay\" {outputPath}";

            await _ffmpeg.ExecuteAsync(arguments);

            return outputPath;
        }

        public async Task<string> TrimAndSilenceVideoAsync(double start, double duration)
        {               
            if (duration > 0.0d)
            {
                Logger.Log(
                    $"Trimming {_localVideoPath} from {start} seconds for {duration} seconds.");     

                string trimmedPath = await TrimAsync(_localVideoPath, start, duration);
                
                Logger.Log($"Trimmed: {trimmedPath}");
                Logger.Log($"Removing audio from {_localVideoPath}.");               

                string silencedPath = await RemoveAudioAsync(trimmedPath);

                // Increments the sequence # to output files.
                _fileOutputIndex++;            

                return silencedPath;
            }
            else
            {
                throw new ApplicationException(
                    $"Start ({start}) and duration ({duration}) invalid for video: {_localVideoPath}.");
            }
        }

        private async Task<string> TrimAsync(string inputPath, double start, double length)
        {
            var inputFile = new MediaFile(inputPath);
            var outputFile = new MediaFile(AppendToFileName(inputPath, "_t", true));
            var options = new ConversionOptions();            
            options.CutMedia(TimeSpan.FromSeconds(start), TimeSpan.FromSeconds(length));   
            
            try 
            {
                await _ffmpeg.ConvertAsync(inputFile, outputFile, options); 
            }
            catch (Exception e)
            {
                throw new ApplicationException(
                    "Unable to trim ski video, is FFMPEG installed?", e
                );
            }
            return outputFile.FileInfo.FullName;
        }

        private async Task<string> RemoveAudioAsync(string inputFile)
        {
            string outputFile = AppendToFileName(inputFile, "s");
            string parameters = $"-i {inputFile} -c copy -an {outputFile}";
            await _ffmpeg.ExecuteAsync(parameters);
            Logger.Log($"Removed Audio: {outputFile}");
            
            return outputFile;
        }

        public DateTime GetCreationTime()
        {
            string output = GetFFPmegOutput(_localVideoPath);
            return ParseCreationDate(output);
        }

        public async Task<DateTime> GetCreationTimeAsync()
        {
            DateTime creationTime = await Task.Run( () => {
                return GetCreationTime();
            } );
            return creationTime;
        }        

        /// <summary>
        /// Generates a thumbnail image at the seconds specified.  Returns the path.
        /// </summary>
        public async Task<string> GetThumbnailAsync(double atSeconds)
        {
            if (!_localVideoPath.ToUpper().EndsWith(".MP4"))
                throw new ApplicationException($"Cannot generate thumbnail, invalid video path: {_localVideoPath}");
            
            string thumbnailPath = AppendToFileName(
                Path.ChangeExtension(_localVideoPath, ".PNG"), "", true);

            var inputFile = new MediaFile(_localVideoPath);
            var outputFile = new MediaFile(thumbnailPath);
            var options = new ConversionOptions { Seek = TimeSpan.FromSeconds(atSeconds) };

            Logger.Log($"Generating thumbnail: {thumbnailPath} for video: {_localVideoPath} at {atSeconds} seconds.");
            await _ffmpeg.GetThumbnailAsync(inputFile, outputFile, options);

            return thumbnailPath;
        }

        private void OnError(object sender, ConversionErrorEventArgs e)
        {
            Logger.Log(string.Format("FFMPEG error: [{0} => {1}]: Error: {2}\n\t{3}",
                e.Input.FileInfo.Name, e.Output.FileInfo.Name, e.Exception.ExitCode, e.Exception.Message),
                e.Exception);
        }    

        private void OnProgress(object sender, ConversionProgressEventArgs e)
        {
            // Grab every 10th progress update.
            if (e.Frame % 10 == 0)
                Logger.Log($"{_localVideoPath} -- Processed frame {e.Frame} @{e.ProcessedDuration}");
        }

        private string AppendToFileName(string inputFile, string suffix, bool appendFileIndex = false)
        {
            string fileIndex = "";
            if (appendFileIndex && _fileOutputIndex > 0)
                fileIndex = "_" + _fileOutputIndex.ToString();
            
            string extension = Path.GetExtension(inputFile);
            int start = inputFile.LastIndexOf(extension);
            if (start <= 0)
                throw new ApplicationException($"Could not generate an output filename from {inputFile}.");

            string outputFile = $"{inputFile.Substring(0, start)}{fileIndex}{suffix}{extension}";
            return outputFile;
        }    

        private string GetFFPmegOutput(string inputFile)
        {
            string output;
            try
            {
                string parameters = $"-i {inputFile}";
                using (Process ffmpeg = new Process())
                {
                    ffmpeg.StartInfo.FileName = "ffmpeg";
                    ffmpeg.StartInfo.Arguments = parameters;
                    ffmpeg.StartInfo.UseShellExecute = false;
                    ffmpeg.StartInfo.RedirectStandardOutput = true;
                    ffmpeg.StartInfo.RedirectStandardError = true;
                    ffmpeg.Start();
                    output = ffmpeg.StandardError.ReadToEnd();
                    ffmpeg.WaitForExit();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException("Unable to execute ffmpeg:  " + e.Message);
            }        
            return output;    
        }

        private DateTime ParseCreationDate(string input)
        {
            // Read and trim start of each line until it begins with: creation_time
            // Then parse after the ':'
            //      creation_time   : 2019-07-11T07:13:55.000000Z
            DateTime creationTime = default; // DateTime.MinValue;
            using (StringReader reader = new StringReader(input))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Trim().StartsWith("creation_time")) 
                    {
                        creationTime = ParseCreationTimeLine(line);
                        break;
                    }
                }
            }

            if (creationTime == default)
            {
                throw new ApplicationException($"Unable to find creation_time in output:\n {input}");
            }
            return creationTime;
        }

        private DateTime ParseCreationTimeLine(string line)
        {
            int start = line.IndexOf(':');
            if (start <= 0 || line.Length < start + 1)
                throw new ApplicationException("Improperly formed creation_time.");

            string rawDate = line.Substring(start+1).Trim();
            DateTime creationTime = DateTime.Parse(rawDate);

            //
            // Video creationTime is local to the camera where it was recorded.  This process is likely
            // to run on a machine/container who's local time is UTC.  Need to force a conversion to UTC
            // from the timezone where the video was recorded.
            //           
            DateTime toConvertTime = DateTime.SpecifyKind(creationTime, DateTimeKind.Unspecified);
            DateTime utcTime = TimeZoneInfo.ConvertTimeToUtc(toConvertTime, _videoTimeZone);

            return utcTime;
        }

        private string GetCombinedVideoPath(string video2Path)
        {
            int hash = (_localVideoPath + video2Path).GetHashCode();
            return $"{hash:X8}.MP4"; 
        }
    }
}
