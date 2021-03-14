using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace healthchecks
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
            services.AddDbContext<ApplicationDbContext>(options => 
                options.UseSqlServer("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;"));
            
            services.AddControllers();

            // Registers required services for health checks
            services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy(), new[] { "services" })
                .AddDbContextCheck<ApplicationDbContext>("db context", null, new [] { "services" });

            services
              .AddHealthChecksUI()
              .AddInMemoryStorage();
        }
        
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                // an endpoint for the health check UI
                endpoints.MapHealthChecksUI(config => 
                { 
                    config.UIPath = "/healthcheck"; 
                });

                // Kubernetes fro example has probes for livelyness and readiness so
                // configure an endpoint for all the services that have checks as ready
                endpoints.MapHealthChecks("/ready", new HealthCheckOptions()
                {
                    Predicate = x => x.Tags.Contains("services"),
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                //configure a simple "self" endpoint for the livelyness probe
                endpoints.MapHealthChecks("/lively", new HealthCheckOptions()
                {
                    Predicate = x => x.Name.Equals("self"),
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
            });
        }
    }
}
