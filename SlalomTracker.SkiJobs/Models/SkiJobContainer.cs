using Microsoft.Azure.Management.ContainerInstance.Models;

namespace SlalomTracker.SkiJobs.Models
{
    public class SkiJobContainer
    {
        public string Image { get; set; }
        public string Name { get; set; }
        public string Video { get; set; }

        public static SkiJobContainer FromContainerGroup(ContainerGroup item)
        {
            if (item.Containers == null || item.Containers.Count < 1 ||
                    item.Containers[0].Command.Count < 2)
                return new SkiJobContainer() { Name = item.Name };

            return new SkiJobContainer() {
                Image = item.Containers[0].Image,
                Name = item.Name,
                Video = item.Containers[0].Command[2]
            };
        }
    }
}