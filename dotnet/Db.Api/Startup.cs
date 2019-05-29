using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Buffers;
using Db.Api.Storage;
using Db.Storage;
using Serilog;
using Serilog.Context;

namespace Db.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        private string DataPath => Configuration["datapath"] ?? Configuration.GetSection("Data")["Path"] ?? "dbdata";

        public void ConfigureServices(IServiceCollection services)
        {
            var applicationPartManager = new ApplicationPartManager();
            applicationPartManager.ApplicationParts.Add(new AssemblyPart(typeof(Startup).Assembly));
            services.Add(new ServiceDescriptor(typeof(ApplicationPartManager), applicationPartManager));

            services.AddMvcCore();
            services.AddCors(options => options
                .AddDefaultPolicy(cors => cors
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowAnyOrigin()));

            services.AddSingleton(new DataStore(MemoryPool<byte>.Shared, Store.Open(DataPath)));
        }

        public void Configure(IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                using (LogContext.PushProperty("TraceId", context.TraceIdentifier, true))
                {
                    try
                    {
                        Log.Debug("Executing request");
                        await next.Invoke();
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Failed to execute request");
                        throw;
                    }
                }
            });

            app.UseCors();
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}