using Amazon;
using Amazon.RDS;
using Amazon.RDS.Model;
using Amazon.Runtime;
using Instancely.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instancely.Controllers
{
    public class RDSController : Controller
    {
        private static BasicAWSCredentials credentials = new BasicAWSCredentials(System.Environment.GetEnvironmentVariable("AWS_ACCESS_KEY"), System.Environment.GetEnvironmentVariable("AWS_SECRET_KEY"));
        private static AmazonRDSClient client = new AmazonRDSClient(credentials, RegionEndpoint.APSoutheast2);

        [HttpGet("/rds")]
        public async Task<IActionResult> Index(string team, string search)
        {
            var instances = await GetInstancesAsync();
            var teams = GetTeams(instances);
            var environments = new List<Models.Environment>();

            if (!String.IsNullOrEmpty(team) || !String.IsNullOrEmpty(search))
                environments = GetEnvironments(instances, team, search);

            var rds = new RDS
            {
                Teams = teams,
                Environments = environments
            };

            ViewData["Team"] = team;
            ViewData["Search"] = search;

            return View("~/Views/Instancely/RDS.cshtml", rds);
        }

        public async Task<List<RDSInstance>> GetInstancesAsync()
        {
            DescribeDBInstancesResponse res;
            using (var client = new AmazonRDSClient(credentials, RegionEndpoint.APSoutheast2))
                res = await client.DescribeDBInstancesAsync(new DescribeDBInstancesRequest());

            var instances = new List<RDSInstance>();
            foreach (var instance in res.DBInstances)
            {
                var tags = await client.ListTagsForResourceAsync(new ListTagsForResourceRequest { ResourceName = instance.DBInstanceArn });

                instances.Add(new RDSInstance(instance, tags.TagList));
            }

            return instances;
        }

        public List<string> GetTeams(List<RDSInstance> instances)
        {
            var teams = new List<string>();

            foreach (var instance in instances)
            {
                var teamName = instance.Tags.Where(t => t.Key.Equals("Team", StringComparison.CurrentCultureIgnoreCase)).Select(t => t.Value).FirstOrDefault();
                if (teamName == null)
                    teamName = "Unknown Team";

                teams.Add(teamName);
            }

            return teams.Distinct().OrderBy(t => t).ToList();
        }

        public List<Models.Environment> GetEnvironments(List<RDSInstance> instances, string team, string search)
        {
            var environments = new List<Models.Environment>();

            foreach (var instance in instances)
            {
                var teamName = instance.Tags.Where(t => t.Key.Equals("Team", StringComparison.CurrentCultureIgnoreCase)).Select(t => t.Value).FirstOrDefault();
                if (teamName == null)
                    teamName = "Unknwon Team";

                var environmentName = instance.Tags.Where(t => t.Key.Equals("Environment", StringComparison.CurrentCultureIgnoreCase)).Select(t => t.Value).FirstOrDefault();
                if (environmentName == null)
                    environmentName = "Unknown Environment";

                var applicationName = instance.Tags.Where(t => t.Key.Equals("Application", StringComparison.CurrentCultureIgnoreCase)).Select(t => t.Value).FirstOrDefault();
                if (applicationName == null)
                    applicationName = "Unknown Application";

                if (!String.IsNullOrEmpty(team) && String.Compare(teamName, team, true) == 0 || !String.IsNullOrEmpty(search) && applicationName.ToLower().Contains(search.ToLower()) || !String.IsNullOrEmpty(search) && instance.DBInstance.DBInstanceIdentifier.ToLower().Contains(search.ToLower()))
                {
                    var environment = environments.FirstOrDefault(e => e.Name.Equals(environmentName));
                    if (environment == null)
                    {
                        environment = new Models.Environment
                        {
                            Name = environmentName,
                            Applications = new List<Application>()
                        };

                        environments.Add(environment);
                    }

                    var application = environment.Applications.FirstOrDefault(e => e.Name.Equals(applicationName));
                    if (application == null)
                    {
                        application = new Application
                        {
                            Name = applicationName,
                            RDSInstances = new List<RDSInstance>()
                        };

                        environment.Applications.Add(application);
                    }

                    application.RDSInstances.Add(instance);
                }
            }

            return environments;
        }

        [Authorize]
        [HttpPost("/rds/instance")]
        public async Task<JsonResult> Actions()
        {
            string action = Request.Query["action"];
            var id = Request.Query["id"];
            if (String.IsNullOrEmpty(action) || String.IsNullOrEmpty(id))
                return Json("The value for the action or id parameter cannot be null.");

            var username = User.Claims.Where(c => c.Type.Equals("name")).Select(c => c.Value).SingleOrDefault();
            var slack = new SlackController();
            if (action == "restart")
                await slack.SendMessageAsync("A request to " + action + " the RDS instance '" + id + "' has been submitted by " + username + ".");

            var result = string.Empty;

            switch (action)
            {
                case "restart":
                {
                    try
                    {
                        var request = new RebootDBInstanceRequest() { DBInstanceIdentifier = id };
                        var response = await client.RebootDBInstanceAsync(request);

                        result = response.HttpStatusCode.ToString();
                    }
                    catch (Exception e)
                    {
                        result = e.ToString();
                    }

                    break;
                }
                default:
                {
                    result = action + " is not a valid action.";
                    break;
                }
            }

            return Json(result);
        }
    }
}
