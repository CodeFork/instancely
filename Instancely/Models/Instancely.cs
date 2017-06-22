using System.Collections.Generic;

namespace Instancely.Models
{
    public class EC2
    {
        public List<string> Teams { get; set; }
        public List<Environment> Environments { get; set; }
    }

    public class RDS
    {
        public List<string> Teams { get; set; }
        public List<Environment> Environments { get; set; }
    }

    public class Environment
    {
        public string Name { get; set; }
        public List<Application> Applications { get; set; }
    }

    public class Application
    {
        public string Name { get; set; }
        public List<Amazon.EC2.Model.Instance> EC2Instances { get; set; }
        public List<RDSInstance> RDSInstances { get; set; }
    }

    public class RDSInstance
    {
        public Amazon.RDS.Model.DBInstance DBInstance { get; set; }
        public List<Amazon.RDS.Model.Tag> Tags { get; set; }
        public RDSInstance(Amazon.RDS.Model.DBInstance dBInstance, List<Amazon.RDS.Model.Tag> tags)
        {
            DBInstance = dBInstance;
            Tags = tags;
        }
    }
}
