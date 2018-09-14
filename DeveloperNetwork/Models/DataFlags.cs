using System;

namespace DeveloperNetwork.Models
{
    [Flags]
    public enum DataFlags : short
    {
        Commits = 1,
        Releases = 2,
        Statistics = 4,
        Users = 8
    }
}
