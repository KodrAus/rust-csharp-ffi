using Db.Api.Storage;
using Db.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Buffers;

namespace Db.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public string DataPath => Configuration["datapath"] ?? Configuration.GetSection("Data")["Path"] ?? "dbdata";

        public void ConfigureServices(IServiceCollection services)
        {
            var applicationPartManager = new ApplicationPartManager();
            applicationPartManager.ApplicationParts.Add(new AssemblyPart(typeof(Startup).Assembly));
            services.Add(new ServiceDescriptor(typeof(ApplicationPartManager), applicationPartManager));

            services.AddSingleton(new DataStore(MemoryPool<byte>.Shared, Store.Open(DataPath)));

            services.AddMvcCore().AddJsonFormatters();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

            app.UseMvc();
        }
    }
}