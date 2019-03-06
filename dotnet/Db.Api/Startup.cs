using Db.Api.Storage;
using Db.Storage;

namespace Db.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var applicationPartManager = new ApplicationPartManager();
            applicationPartManager.ApplicationParts.Add(new AssemblyPart(typeof(Startup).Assembly));
            services.Add(new ServiceDescriptor(typeof(ApplicationPartManager), applicationPartManager));

            services.AddSingleton(new DataStore(Store.Open("./dbdata")));
            services.AddScoped(s => new Lazy<DataReader>(() => s.GetService<DataStore>().BeginRead()));
            services.AddScoped(s => new Lazy<DataWriter>(() => s.GetService<DataStore>().BeginWrite()));

            services.AddMvcCore().AddJsonFormatters();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

            app.UseMvc();
        }
    }
}