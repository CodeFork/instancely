using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
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
    public class EC2Controller : Controller
    {
        private static BasicAWSCredentials credentials = new BasicAWSCredentials(System.Environment.GetEnvironmentVariable("AWS_ACCESS_KEY"), System.Environment.GetEnvironmentVariable("AWS_SECRET_KEY"));
        private static AmazonEC2Client client = new AmazonEC2Client(credentials, RegionEndpoint.APSoutheast2);

        public async Task<IActionResult> Index(string team, string search)
        {
            var instances = await GetInstancesAsync();
            var teams = GetTeams(instances);
            var environments = new List<Models.Environment>();

            if (!String.IsNullOrEmpty(team) || !String.IsNullOrEmpty(search))
                environments = GetEnvironments(instances, team, search);

            var ec2 = new EC2
            {
                Teams = teams,
                Environments = environments
            };

            ViewData["Team"] = team;
            ViewData["Search"] = search;

            return View("~/Views/Instancely/EC2.cshtml", ec2);
        }

        public async Task<List<Instance>> GetInstancesAsync()
        {
            DescribeInstancesResponse res;
            using (var client = new AmazonEC2Client(credentials, RegionEndpoint.APSoutheast2))
                res = await client.DescribeInstancesAsync(new DescribeInstancesRequest());

            var instances = new List<Instance>();
            foreach (var instance in res.Reservations.SelectMany(r => r.Instances))
                instances.Add(instance);

            return instances;
        }

        public List<string> GetTeams(List<Instance> instances)
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

        public List<Models.Environment> GetEnvironments(List<Instance> instances, string team, string search)
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

                var instanceName = instance.Tags.Where(t => t.Key.Equals("Name", StringComparison.CurrentCultureIgnoreCase)).Select(t => t.Value).FirstOrDefault();
                if (instanceName == null)
                    instanceName = "Unknown Instance";

                if (!String.IsNullOrEmpty(team) && String.Compare(teamName, team, true) == 0 || !String.IsNullOrEmpty(search) && applicationName.ToLower().Contains(search.ToLower()) || !String.IsNullOrEmpty(search) && instanceName.ToLower().Contains(search.ToLower()))
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
                            EC2Instances = new List<Instance>()
                        };

                        environment.Applications.Add(application);
                    }

                    application.EC2Instances.Add(instance);
                }
            }

            return environments;
        }

        [Authorize]
        [HttpPost("/ec2/instance")]
        public async Task<JsonResult> Actions()
        {
            string action = Request.Query["action"];
            var id = Request.Query["id"];
            if (String.IsNullOrEmpty(action) || String.IsNullOrEmpty(id))
                return Json("The value for the action or id parameter cannot be null.");

            var username = User.Claims.Where(c => c.Type.Equals("name")).Select(c => c.Value).SingleOrDefault();
            var slack = new SlackController();
            if (action == "start" || action == "stop" || action == "restart" || action == "terminate")
                await slack.SendMessageAsync("A request to " + action + " the EC2 instance '" + id + "' has been submitted by " + username + ".");

            var instanceIds = new List<string>();
            instanceIds.Add(id);

            var result = string.Empty;

            switch (action)
            {
                case "start":
                {
                    try
                    {
                        var request = new StartInstancesRequest() { InstanceIds = instanceIds };
                        var response = await client.StartInstancesAsync(request);

                        result = response.HttpStatusCode.ToString();
                    }
                    catch (Exception e)
                    {
                        result = e.ToString();
                    }
                    
                    break;
                }
                case "stop":
                {
                    try
                    {
                        var request = new StopInstancesRequest() { InstanceIds = instanceIds };
                        var response = await client.StopInstancesAsync(request);

                        result = response.HttpStatusCode.ToString();
                    }
                    catch (Exception e)
                    {
                        result = e.ToString();
                    }

                    break;
                }
                case "restart":
                {
                    try
                    {
                        var request = new RebootInstancesRequest() { InstanceIds = instanceIds };
                        var response = await client.RebootInstancesAsync(request);

                        result = response.HttpStatusCode.ToString();
                    }
                    catch (Exception e)
                    {
                        result = e.ToString();
                    }

                    break;
                }
                case "terminate":
                {
                    try
                    {
                        var request = new TerminateInstancesRequest() { InstanceIds = instanceIds };
                        var response = await client.TerminateInstancesAsync(request);

                        result = response.HttpStatusCode.ToString();
                    }
                    catch (Exception e)
                    {
                        result = e.ToString();
                    }

                    break;
                }
                case "logs":
                {
                    try
                    {
                        var request = new GetConsoleOutputRequest() { InstanceId = id };
                        var response = await client.GetConsoleOutputAsync(request);
                        var base64 = Convert.FromBase64String(response.Output);

                        result = Encoding.UTF8.GetString(base64);
                    }
                    catch (Exception e)
                    {
                        result = e.ToString();
                    }

                    break;
                }
                case "screenshot":
                {
                    try
                    {
                        var request = new GetConsoleScreenshotRequest() { InstanceId = id };
                        var response = await client.GetConsoleScreenshotAsync(request);

                        result = "data:image/jpeg;base64," + response.ImageData;
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
