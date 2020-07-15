/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TeamCloud.Providers.Testing.Services
{
    class OrchestratorStartup
    {
        public OrchestratorStartup(IConfiguration configuration)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public IConfiguration Configuration { get; }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app
                .UsePathBase(new PathString("/api"))
                .UseRouting()
                .UseEndpoints(configuration => configuration.MapControllers());
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvc();

            services
                .AddRouting()
                .AddControllers()
                .AddNewtonsoftJson()
                .ConfigureApiBehaviorOptions(options => options.SuppressMapClientErrors = true);
        }
    }
}
