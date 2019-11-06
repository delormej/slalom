using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Microsoft.Azure.Services.AppAuthentication;

namespace SlalomTracker.SkiJobs
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            
            // Use Dependency Injection to add Azure Credentials to all controllers.
            services.AddSingleton<ServiceClientCredentials>(sp => 
                GetAzureCredentials() );
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private ServiceClientCredentials GetAzureCredentials()
        {
            string accessToken = GetAzureAccessToken();    
            ServiceClientCredentials credentials = new TokenCredentials(accessToken);
            return credentials;
        }

        private string GetAzureAccessToken()
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            var task = azureServiceTokenProvider.GetAccessTokenAsync("https://management.azure.com/");
            task.Wait();
            return task.Result;    
        }      
    }
}
