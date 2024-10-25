using RestSharp;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Text;

public class DevOpsAutomation
{
    public static void RunAutomation(int testCaseId)
    {
        string azureDevOpsUrl = "https://dev.azure.com/ABBDemo/ABBDevOpsDemoNew/_apis/wit/workitems/";
        string personalAccessToken = "fzobflefbkmwfzzd4xrwsykgnd2qhhohqbrqwv6mpvfg4zjkn5na";

        // Step 1: Get the Test Case Work Item from Azure DevOps
        var testCase = GetTestCaseFromAzureDevOps(azureDevOpsUrl, personalAccessToken, testCaseId);

        if (testCase != null)
        {
            // Step 2: Extract the website URL from the test case description (first line)
            string description = testCase["fields"]["System.Description"].ToString();
            //            string[] descriptionLines = description.Split(new[] { "<div>", "</div>" }, StringSplitOptions.None);
            //            string websiteUrl = descriptionLines[0]; // Assuming first line contains the website URL
            // Find the URL inside the href attribute
            string urlPattern = "href=\"";
            int startIndex = description.IndexOf(urlPattern) + urlPattern.Length;
            int endIndex = description.IndexOf("\"", startIndex);
            string websiteUrl = description.Substring(startIndex, endIndex - startIndex);

            // Print the extracted URL
            Console.WriteLine("Extracted Website URL: " + websiteUrl);

            Console.WriteLine("Website URL: " + websiteUrl);
            // Print the raw HTML content
            Console.WriteLine("Work Item Description (Raw HTML):");
            Console.WriteLine(description);

            // Step 3: Use Selenium to verify if "Dashboard" is present on the webpage
            bool testPassed = CheckDashboardTextOnWebsite(websiteUrl);

            if (!testPassed)
            {
                Console.WriteLine("Test Failed. Creating a Bug...for Website URL" + websiteUrl);

                // Step 4: Create a Bug in Azure DevOps
                string testCaseTitle = testCase["fields"]["System.Title"].ToString();
                string bugTitle = $"Test Case ID: {testCaseId}, Title: {testCaseTitle}, Test Failed during Automation";

                CreateBugInAzureDevOps(azureDevOpsUrl, personalAccessToken, bugTitle, testCaseTitle, testCaseId);
            }
            else
            {
                Console.WriteLine("Test Passed.");
            }
        }
    }

    // Other methods (GetTestCaseFromAzureDevOps, CheckDashboardTextOnWebsite, CreateBugInAzureDevOps) remain unchanged
    static JObject GetTestCaseFromAzureDevOps(string azureDevOpsUrl, string personalAccessToken, int workItemId)
    {

        string requestUrl = $"{azureDevOpsUrl}{workItemId}?api-version=6.0";
        var client = new RestClient(requestUrl);
        var request = new RestRequest(Method.GET);

        string authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{personalAccessToken}"));
        request.AddHeader("Authorization", $"Basic {authToken}");

        IRestResponse response = client.Execute(request);
        if (response.IsSuccessful)
        {
            return JObject.Parse(response.Content); // Return the test case as a JObject
        }

        Console.WriteLine("Failed to retrieve test case: " + response.ErrorMessage);
        return null;
    }

    static bool CheckDashboardTextOnWebsite(string websiteUrl)
    {
        IWebDriver driver = new ChromeDriver();
        try
        {
            driver.Navigate().GoToUrl(websiteUrl);

            // Check if "Dashboard" text is present on the page
            bool isDashboardPresent = driver.PageSource.Contains("Dashboard");

            return isDashboardPresent;
        }
        finally
        {
            driver.Quit();
        }
    }

    static void CreateBugInAzureDevOps(string azureDevOpsUrl, string personalAccessToken, string bugTitle, string testCaseTitle, int testCaseId)
    {
        string requestUrl = $"{azureDevOpsUrl}$Bug?api-version=6.0";
        var client = new RestClient(requestUrl);
        var request = new RestRequest(Method.POST);
        request.AddHeader("Content-Type", "application/json-patch+json");

        string authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{personalAccessToken}"));
        request.AddHeader("Authorization", $"Basic {authToken}");

        Console.WriteLine("azureDevOpsUrl: " + azureDevOpsUrl);
        Console.WriteLine("requestUrl: " + requestUrl);
        Console.WriteLine("personalAccessToken: " + personalAccessToken);
        Console.WriteLine("bugTitle: " + bugTitle);
        Console.WriteLine("testCaseTitle: " + testCaseTitle);
        Console.WriteLine("testCaseId: " + testCaseId);

        // Construct Bug data
        var bugData = new[]
        {
            new { op = "add", path = "/fields/System.Title", value = bugTitle },
            new { op = "add", path = "/fields/System.Description", value = $"Test Case ID: {testCaseId}, Title: {testCaseTitle}. Failed during automation." },
            new { op = "add", path = "/fields/System.AssignedTo", value = "shahab@tecoholic.com" }, // Change if you want to assign the bug
            new { op = "add", path = "/fields/Microsoft.VSTS.TCM.ReproSteps", value = "See attached logs for detailed error." }
        };

        request.AddParameter("application/json-patch+json", Newtonsoft.Json.JsonConvert.SerializeObject(bugData), ParameterType.RequestBody);

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
