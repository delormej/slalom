using System;
using System.Threading.Tasks;

namespace SlalomTracker.Video 
{
    public interface IProcessor
    {
        Task ProcessAsync();
    }
}