using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Debugging.Controllers;
using DotJEM.AspNetCore.FluentRouting.Extentions;
using DotJEM.AspNetCore.FluentRouting.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Debugging
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
            services.AddMvc();
            services.AddFluentRouting();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseFluentRouter(router =>
            {
                router.Route("hello").To(() => JObject.FromObject(new {hello = "World"}));
                router.Route("api/values/{id?}").To<ValuesController>();
                router.Route("api/context/{id?}").To(context => "HELLO CONTEXT");
                router.Route("api/services/{param}").To((FromRoute<string> param, FromQuery<string> take, FromBody<JObject> body, FromServices<IActionSelector> selector)
                    =>
                {
                    JObject entity = body;
                    entity["param"] = param.Value;
                    entity["take"] = take.Value;
                    return entity;
                });


                //router.Load<MyModule>();
            });
        }
    }
}
