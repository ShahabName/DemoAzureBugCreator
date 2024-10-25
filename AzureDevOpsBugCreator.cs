
using Newtonsoft.Json;
using RestSharp;
using System;
using System.IO;

namespace SeleniumTests
{
    public class AzureDevOpsBugCreator
    {
        private string azureDevOpsUrl;
        private string project;
        private string personalAccessToken;

        public AzureDevOpsBugCreator()
        {
            var config = File.ReadAllText("appsettings.json");
            dynamic settings = JsonConvert.DeserializeObject(config);
            azureDevOpsUrl = settings.AzureDevOpsUrl;
            project = settings.Project;
            personalAccessToken = settings.PersonalAccessToken;
        }

        public void CreateBug(string bugTitle)
        {
            var client = new RestClient($"{azureDevOpsUrl}/{project}/_apis/wit/workitems/$Bug?api-version=6.0");
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/json-patch+json");
            string authToken = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{personalAccessToken}"));
            request.AddHeader("Authorization", $"Basic {authToken}");

            var bugData = new[]
            {
                new { op = "add", path = "/fields/System.Title", value = bugTitle },
                new { op = "add", path = "/fields/System.Description", value = "Bug created automatically due to failed Selenium test." },
                new { op = "add", path = "/fields/System.AssignedTo", value = "shahab@tecoholic.com" },
                new { op = "add", path = "/fields/Microsoft.VSTS.TCM.ReproSteps", value = "See attached logs for detailed error." }
            };

            request.AddParameter("application/json-patch+json", JsonConvert.SerializeObject(bugData), ParameterType.RequestBody);

            IRestResponse response = client.Execute(request);
            if (response.IsSuccessful)
            {
                Console.WriteLine("Bug created successfully in Azure DevOps.");
            }
            else
            {
                Console.WriteLine("Failed to create bug: " + response.ErrorMessage);
            }
        }
    }
}
