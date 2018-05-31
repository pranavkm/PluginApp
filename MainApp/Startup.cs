using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace MainApp
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
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            var mvcBuilder = services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            ConfigurePlugin(mvcBuilder.PartManager);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            var staticFileOptions = new StaticFileOptions();
            var fileProvider = new IFileProvider[]
            {
                env.ContentRootFileProvider,
                new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "PluginApp", "wwwroot")),
            };
            app.UseStaticFiles(new StaticFileOptions { FileProvider = new CompositeFileProvider(fileProvider) });
            app.UseCookiePolicy();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private void ConfigurePlugin(ApplicationPartManager applicationPartManager)
        {
            var pluginAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(Directory.GetCurrentDirectory(), "PluginApp", "PluginApp.dll"));
            var partFactory = ApplicationPartFactory.GetApplicationPartFactory(pluginAssembly);

            foreach (var part in partFactory.GetApplicationParts(pluginAssembly))
            {
                applicationPartManager.ApplicationParts.Add(part);
            }

            var relatedAssemblies = RelatedAssemblyAttribute.GetRelatedAssemblies(pluginAssembly, throwOnError: true);
            foreach (var assembly in relatedAssemblies)
            {
                partFactory = ApplicationPartFactory.GetApplicationPartFactory(assembly);
                foreach (var part in partFactory.GetApplicationParts(assembly))
                {
                    applicationPartManager.ApplicationParts.Add(part);
                }
            }
        }
    }
}
