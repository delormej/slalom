using System.IO;
using System.Net;
using System.Diagnostics;

namespace MetadataExtractor
{
    class Parser
    {
        const string GPMFEXE = "gpmfdemo";
        
        static string ParseMetadata(string path)
        {
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = GPMFEXE,
                    Arguments = path,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return result;
        }
    }
}
