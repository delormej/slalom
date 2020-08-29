using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SlalomTracker.Cloud;

namespace SlalomTracker.Cloud.Tests
{
    [TestClass]
    public class VttTest
    {
        const string RECORDED_DATE = "2019-10-26";
        const string VIDEO_FILE = "GP012353_ts.MP4";

        [TestMethod]
        public void TestCreateVtt()
        {
            AzureStorage storage = new AzureStorage();
            SkiVideoEntity entity = storage.GetSkiVideoEntity(RECORDED_DATE, VIDEO_FILE);
            WebVtt vtt = new WebVtt(entity);
            string vttContent = vtt.Create();
            if (vttContent == null)
                System.Console.WriteLine("Nothing..");
            else
                System.Console.WriteLine("Content: " + vttContent);
        }
    }
}