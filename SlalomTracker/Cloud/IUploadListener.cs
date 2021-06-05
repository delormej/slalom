using System;

namespace SlalomTracker.Cloud
{
    public interface IUploadListener
    {
        event EventHandler Completed;
        void Start();
        void Stop();
    }
}