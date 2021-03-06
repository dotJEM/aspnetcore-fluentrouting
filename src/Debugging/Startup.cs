﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Debugging.Controllers;
using DotJEM.AspNetCore.FluentRouting.Builders;
using DotJEM.AspNetCore.FluentRouting.Extentions;
using DotJEM.AspNetCore.FluentRouting.Routing;
using DotJEM.AspNetCore.FluentRouting.Routing.Lambdas;
using DotJEM.AspNetCore.FluentRouting.Views;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Debugging
{
    public class DummyViewDataProvider : IViewDataProvider
    {
        public class Model
        {
            public string Message { get; set; }
        }

        public ViewDataDictionary ViewData(string viewName, HttpContext context)
        {
            return new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                Model = new Model
                {
                    Message = "FOO IS HERE!"
                }
            };
        }

        public ITempDataDictionary TempData(string viewName, HttpContext context) => null;
    }

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
            //TODO: Commenting out AddMvc should work, but it changes a bit... Need to figure that one out...
            services.AddFluentRouting();
            services.AddMvc();
            services.AddSingleton<IViewDataProvider>(new DummyViewDataProvider());
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
                router.Route("").ToView("~/views/index.cshtml");
                
                router.Route("hello").To(() => JObject.FromObject(new {hello = "World"}));

                router.Route("api/values/{id?}").To<ValuesController>();

                router.Route("api/context/{id?}").To(context => "HELLO CONTEXT");
                
                router.Route("api/conven").To((string key, string value) => { return key + value; });

                router.Route("api/services/{param}").To((FromRoute<string> param, FromQuery<string> take, FromBody<JObject> body, FromServices<IActionSelector> selector)
                    =>
                {
                    JObject entity = body;
                    entity["param"] = param.Value;
                    entity["take"] = take.Value;
                    return entity;
                });

                router.Default().ToView("~/views/index.cshtml");


                //router.Load<MyModule>();

            });
        }
    }
}
