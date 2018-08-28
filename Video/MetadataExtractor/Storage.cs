using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace MetadataExtractor
{
    public class Storage
    {
        const string SKICONTAINER = "ski";
        CloudStorageAccount _account;
        Queue _queue;

        public Storage(CloudStorageAccount account)
        {
            _account = account;
            _queue = new Queue(_account);
        }

        public void UploadVideos(string path)
        {
            if (IsFilePath(path))
            {
                string outputUrl = UploadVideo(path);
                Console.WriteLine("Wrote " + path + " to " + outputUrl);
            }
            else
            {
                WalkDirectories(path);
            }            
        }

        public string UploadVideo(string localFile)
        {
            string blobName = GetBlobName(localFile);
            CloudBlobClient blobClient = _account.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(SKICONTAINER);
            CloudBlockBlob blob = blobContainer.GetBlockBlobReference(blobName);
            var task = blob.UploadFromFileAsync(localFile);
            task.Wait();

            string uri = blob.SnapshotQualifiedUri.AbsoluteUri;
            return uri; // URL to the uploaded video.
        }

        public string DownloadVideo(string videoUrl)
        {
            // Hard coded for now.
            string path = "GOPR0194.MP4";
            if (File.Exists(path)) 
            {
                Console.WriteLine("File already exists.");
            }
            else 
            {
                Console.Write("Requesting video: " + videoUrl + " ...");

                WebClient client = new WebClient();
                client.DownloadFile(videoUrl, path);

                Console.Write("DONE\n");
            }

            return path;
        }        

        private void WalkDirectories(string path) 
        {
            string[] dirs = Directory.GetDirectories(path);
            for (int i = 0; i < dirs.Length; i++) 
            {
                WalkFiles(dirs[i]);
                WalkDirectories(dirs[i]);
            }         
        }

        private void WalkFiles(string path)
        {
            string[] files = Directory.GetFiles(path);
            for (int i = 0; i < files.Length; i++)
            {
                string outputUrl = UploadVideo(files[i]);
                Console.WriteLine("Wrote " + path + " to " + outputUrl);
            }
        }

        /* Checks to see if it's a valid File or Directory.  
            returns True if File, False if Directory, exception if neither.
        */
        private bool IsFilePath(string localFile)
        {
            if (!File.Exists(localFile))
            {
                if (!Directory.Exists(localFile))
                    throw new FileNotFoundException("Invalid file or directory: " + localFile);
                else
                    return false;
            }         
            else 
                return true;
        }

        private string GetBlobName(string localFile)
        {
            if (!File.Exists(localFile))
                throw new FileNotFoundException("Video file does not exist: " + localFile);

            string dir = GetBlobDirectory(localFile);
            string blob = dir + Path.GetFileName(localFile);
            return blob;
        }

        private string GetBlobDirectory(string localFile)
        {
            // Remove HERO5 Black x directory.
            int heroMonikerStart = localFile.IndexOf("HERO");
            if (heroMonikerStart > 0)
            {
                localFile = localFile.Substring(0, heroMonikerStart);
            }

            string dir = "";
            int end = 0, start = localFile.LastIndexOf(Path.DirectorySeparatorChar);
            if (start >= 0)
            {
                for (int i = start - 1; i > 0; i--)
                {
                    if (localFile[i] == Path.DirectorySeparatorChar)
                    {
                        end = i;
                        break;
                    }
                }
                dir = localFile.Substring(end + 1, start - (end + 1));
            }
            if (dir != string.Empty)
                dir += "/";
            return dir;
        }
    }    
}