using System;
using System.IO;
using System.Text;
using Logger = jasondel.Tools.Logger;

namespace SlalomTracker.Cloud
{ 
    /// <summary>
    /// Creates a WebVTT API compliant file: https://developer.mozilla.org/en-US/docs/Web/API/WebVTT_API
    /// </summary>
    public class WebVtt
    {
        const string Prefix = "[@";
        const string Suffix = "seconds]";
        const double Duration = 2.0;

        const string Header = "WEBVTT\n\n";

        private SkiVideoEntity _skiVideo;

        public WebVtt(SkiVideoEntity skiVideo) 
        {
            _skiVideo = skiVideo;
        }

        public string Create() 
        {               
            StringBuilder builder = new StringBuilder(Header);
            StringReader reader = new StringReader(_skiVideo.Notes);
            while(true)
            {
                string line = reader.ReadLine();
                if (line == null)
                    break;
                
                string formattedLine = FormatLine(line);
                if (formattedLine != null)
                    builder.Append(formattedLine);
            }
            
            return builder.ToString();
        }

        private string FormatLine(string line)
        {
            double startSeconds;
            if (!ParseSeconds(line, out startSeconds))
                return null;            
            string time = GetTime(startSeconds, startSeconds + Duration);
            string text = GetText(line);

            if (time == null || text == null)
                return null;

            return time + "\n" + text + "\n\n";
        }

        private bool ParseSeconds(string line, out double seconds)
        {
            int start = Prefix.Length;
            int end = line.IndexOf(Suffix);
            
            if (end > start && line.StartsWith(Prefix))
            {
                string value = line.Substring(start, end - start);
                if (!double.TryParse(value, out seconds))
                    return false;
                else
                    return true;
            }
            else
            {
                seconds = default(double);
                return false;
            }
        }

        private string GetTime(double start, double end)
        {
            return $"00:{start.ToString("00.000")} --> 00:{end.ToString("00.000")}";
        }

        private string GetText(string line)
        {
            int start = line.IndexOf(Suffix) + Suffix.Length;
            if (start >= line.Length)
                return null;
            return line.Substring(start + 1, line.Length - start - 1);
        }
    }
}