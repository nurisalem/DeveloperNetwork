using System;
using DeveloperNetwork.Aliases;
using DeveloperNetwork.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DeveloperNetwork
{
    class Program
    {
        private static   IServiceProvider _serviceProvider;

        private static void Main(string[] args)
        {
            // set up DI
            var serviceCollection = new ServiceCollection();
            _serviceProvider = ConfigureServices(args, serviceCollection);
                
            // run program
 try            
            {
                var projectAnalyzer = _serviceProvider.GetRequiredService<ProjectAnalyzer>();
                projectAnalyzer.RunAnalysis().GetAwaiter().GetResult();

                // log
                var logger = _serviceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Analysis complete. Press any key to exit.");

                Console.ReadKey();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static IServiceProvider ConfigureServices(string[] args, IServiceCollection services)
        {
            // set up logging
            services.AddLogging(configure => configure
                .AddConsole())
                .Configure<LoggerFilterOptions>(options =>
                {
                    options.MinLevel = LogLevel.Trace;
                });

            // set up configuration
            services.AddSingleton(provider => new ProgramConfiguration(args));

            // set up internal classes
            services.AddSingleton<AliasReader, AliasReader>();
            services.AddSingleton<ProjectAnalyzer, ProjectAnalyzer>();

            // set up external services
            services.AddSingleton<MongoProxy, MongoProxy>();
            services.AddSingleton<GitProxy, GitProxy>();
            services.AddSingleton<GoogleProxy, GoogleProxy>();

            // warm-up DI
            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider;
        }
    }
}
