using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SqlClient;

namespace Challenge3
{
    public static class CommitChecker
    {
        [FunctionName("CommitChecker")]
        public static async Task<IActionResult> LookForPng(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            string logo = "";
            string owner = "", repositoryName = "", commitId = "";
            string name = req.Query["name"];
            SqlConnection sqlConn = new SqlConnection(Environment.GetEnvironmentVariable("dbConnectionString"));
            if(req.ContentType.Equals("application/json"))
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                var commits = data.commits;
                owner = data.repository.owner.login.ToString();
                repositoryName = data.repository.name.ToString();
                foreach(var commit in commits)
                {
                    var files = commit.added;
                    commitId = commit.id.ToString();
                    foreach(var file in files)
                    {
                        String filename = file.ToString();
                        if(filename.EndsWith(".png"))
                        {
                            String insertImage = $@"Insert into Images (ImageURL) values ('{returnDownloadURL(filename,owner,repositoryName,commitId)}')";
                            using (SqlCommand cmd = new SqlCommand(insertImage))
                            {
                                sqlConn.Open();
                                cmd.Connection = sqlConn;
                                var state = sqlConn.State;
                                var result = cmd.ExecuteNonQuery();
                            }
                        }
                        sqlConn.Close();
                    }
                }
            
            }
            

            

            return logo != String.Empty
                ? (ActionResult)new OkObjectResult($"{logo}")
                : new BadRequestObjectResult("gg");
        }

        private static string returnDownloadURL(string filename, string owner, string repositoryName, string commitId)
        {
            // https://raw.githubusercontent.com/tdurkut/homework-6/540a7aa78d4c0a3da6412bb13e4ce6a8527e3956/GitHub_Logo.png
            return $"https://raw.githubusercontent.com/{owner}/{repositoryName}/{commitId}/{filename}";
        }
    }
}
