using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;

namespace Instancely.Controllers
{
    public class SlackController : Controller
    {
        private readonly Uri webhook = new Uri(Environment.GetEnvironmentVariable("SLACK_WEBHOOK"));
        private readonly HttpClient client = new HttpClient();

        [Authorize]
        [HttpGet("/slack")]
        public async Task<HttpResponseMessage> SendMessageAsync(string text)
        {
            var payload = new
            {
                channel = "instancely",
                username = "instancely",
                text = text
            };

            var serializedPayload = JsonConvert.SerializeObject(payload);
            var response = await client.PostAsync(webhook,
                new StringContent(serializedPayload, Encoding.UTF8, "application/json"));

            return response;
        }
    }
}
