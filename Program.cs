using System;
using System.Collections.Generic;
using System.IO;
using Elasticsearch.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace aws_elasticsearch_dotnetcore
{
    class Program
    {
        static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton(new LoggerFactory()
                .AddConsole()
                .AddDebug());
            serviceCollection.AddLogging();

            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            if (string.IsNullOrWhiteSpace(environment))
            {
                environment = "Development";
            }

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables("ASPNETCORE_")
                .Build();

            serviceCollection.AddSingleton<IConfigurationRoot>(configuration);

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var _configuration = serviceProvider.GetRequiredService<IConfigurationRoot>();

            var options = _configuration.GetAWSOptions();                 

            var people = new List<Object>();
            
            for (int i = 0; i < 100; i++)
            {
                string _id = Guid.NewGuid().ToString();
                people.Add(new { index = new { _index = "people", _type = "person", _id = _id } });
                people.Add(new
                {   
                    first_name = "name "+i,
                });                     
            }

            var elasticURI = _configuration.GetSection("WorkerSettings").GetSection("ElasticURI").Value;
            var settings = new ConnectionConfiguration(new Uri(elasticURI)).RequestTimeout(TimeSpan.FromMinutes(2));
            var lowlevelClient = new ElasticLowLevelClient(settings);
            var indexResponse = lowlevelClient.Bulk<StringResponse>(PostData.MultiJson(people.ToArray()));
            string responseStream = indexResponse.Body;           

        }
    }
}
