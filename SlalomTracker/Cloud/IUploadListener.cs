using System;

namespace SkiConsole
{
    public interface IUploadListener
    {
        event EventHandler Completed;
        void Start();
        void Stop();
    }
}