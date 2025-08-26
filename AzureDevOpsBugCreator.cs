using Newtonsoft.Json;
using RestSharp;
using System;
using System.IO;

namespace SeleniumTests
{
    /// <summary>
    /// Provides functionality to create bugs in Azure DevOps from automated Selenium test failures.
    /// </summary>
    public class AzureDevOpsBugCreator
    {
        private string azureDevOpsUrl;
        private string project;
        private string personalAccessToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureDevOpsBugCreator"/> class.
        /// Loads configuration from appsettings.json file.
        /// </summary>
        /// <exception cref="FileNotFoundException">Thrown when appsettings.json is not found.</exception>
        /// <exception cref="JsonException">Thrown when appsettings.json is not properly formatted.</exception>
        /// <exception cref="Exception">Thrown when required settings are missing.</exception>
        public AzureDevOpsBugCreator()
        {
            try
            {
                // Read configuration from appsettings.json
                var config = File.ReadAllText("appsettings.json");
                dynamic settings = JsonConvert.DeserializeObject(config);
                azureDevOpsUrl = settings.AzureDevOpsUrl;
                project = settings.Project;
                personalAccessToken = settings.PersonalAccessToken;

                // Validate required settings
                if (string.IsNullOrEmpty(azureDevOpsUrl) ||
                    string.IsNullOrEmpty(project) ||
                    string.IsNullOrEmpty(personalAccessToken))
                {
                    throw new InvalidOperationException("One or more required settings (AzureDevOpsUrl, Project, PersonalAccessToken) are missing in appsettings.json.");
                }
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine("Configuration file not found: " + ex.Message);
                throw;
            }
            catch (JsonException ex)
            {
                Console.WriteLine("Error parsing configuration file: " + ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error initializing AzureDevOpsBugCreator: " + ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Creates a bug work item in Azure DevOps with the specified title.
        /// </summary>
        /// <param name="bugTitle">The title of the bug to create.</param>
        /// <remarks>
        /// The bug will be assigned to shahab@tecoholic.com and have default description and repro steps.
        /// </remarks>
        public void CreateBug(string bugTitle)
        {
            try
            {
                // Initialize RestSharp client for Azure DevOps Bug API
                var client = new RestClient($"{azureDevOpsUrl}/{project}/_apis/wit/workitems/$Bug?api-version=6.0");
                var request = new RestRequest(Method.POST);
                request.AddHeader("Content-Type", "application/json-patch+json");

                // Prepare basic authentication header
                string authToken = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{personalAccessToken}"));
                request.AddHeader("Authorization", $"Basic {authToken}");

                // Prepare the JSON patch payload for bug creation
                var bugData = new[]
                {
                    new { op = "add", path = "/fields/System.Title", value = bugTitle },
                    new { op = "add", path = "/fields/System.Description", value = "Bug created automatically due to failed Selenium test." },
                    new { op = "add", path = "/fields/System.AssignedTo", value = "shahab@tecoholic.com" },
                    new { op = "add", path = "/fields/Microsoft.VSTS.TCM.ReproSteps", value = "See attached logs for detailed error." }
                };

                // Add payload to the request
                request.AddParameter("application/json-patch+json", JsonConvert.SerializeObject(bugData), ParameterType.RequestBody);

                // Execute the request and handle response
                IRestResponse response = client.Execute(request);
                if (response.IsSuccessful)
                {
                    Console.WriteLine("Bug created successfully in Azure DevOps.");
                }
                else
                {
                    Console.WriteLine("Failed to create bug: " + response.ErrorMessage);
                    if (response.ErrorException != null)
                    {
                        Console.WriteLine("Exception details: " + response.ErrorException);
                    }
                }
            }
            catch (Exception ex)
            {
                // Catch any exception during bug creation and log it
                Console.WriteLine("An unexpected error occurred while creating the bug: " + ex.Message);
                Console.WriteLine("Stack Trace: " + ex.StackTrace);
            }
        }
    }
}
