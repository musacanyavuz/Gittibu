using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using GittiBu.Common;
using GittiBu.Services;
using GittiBu.Web.Controllers;
using GittiBu.Web.CustomProviders;
using GittiBu.Web.Helpers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;

namespace GittiBu.Web
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
            /*
            services.Configure<CookiePolicyOptions>(options =>
            { 
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
            */

            //services.Configure<FormOptions>(x =>
            //{
            //    x.ValueLengthLimit = int.MaxValue;
            //    x.MultipartBodyLengthLimit = int.MaxValue;
            //});

            services.AddTransient<TextService>();
            services.AddTransient<Localization>();

            services.AddMemoryCache();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(80);//set time   
            });
            services.AddSession();
           
            services.AddSingleton<ITempDataProvider, CookieTempDataProvider>();
            services.AddSingleton<IMyCache, MyMemoryCache>();
            services.AddHttpContextAccessor();

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.LoginPath = new PathString("/GirisYap");
                });
            services.Configure<RequestLocalizationOptions>(options =>
            { //projeyi mac'te çalıştırdığımda tarih inputlarından gelen değerleri modeldeki DateTime alanlara parse edemediği için ekledim. -berk
                options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("tr-TR");
                options.SupportedCultures = new List<CultureInfo> { new CultureInfo("tr-TR") };
            });

            /*   cshtml ler DB den okumak için 
            services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 60000000;
                options.BufferBodyLengthLimit = 60000000;
            });
            */
           
            services.AddMvc().AddSessionStateTempDataProvider();

           // services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();


            //var sp = services.BuildServiceProvider();
            //services.Configure<RazorViewEngineOptions>(opts =>
            //{

            //    opts.FileProviders.Insert(0, new DatabaseFileProvider(sp.GetService<IHttpContextAccessor>()));
            //} );



            //            services.AddMvc()
            //                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
            //                .AddSessionStateTempDataProvider(); 
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            env.EnvironmentName = Configuration["EnvironmentName"];
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/404");
                app.UseHsts();
            }
            app.UseStatusCodePagesWithReExecute("/404");

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseAuthentication();
            app.UseSession();
            app.UseRequestLocalization();

            app.Use(async (context, next) =>
            {
                string path = context.Request.Path;

                if (path.EndsWith(".css") || path.EndsWith(".js"))
                {
                    //Set css and js files to be cached for 7 days
                    TimeSpan maxAge = new TimeSpan(7, 0, 0, 0);     //7 days
                    context.Response.Headers.Append("Cache-Control", "max-age=" + maxAge.TotalSeconds.ToString("0"));
                }
                else if (path.EndsWith(".gif") || path.EndsWith(".jpg") || path.EndsWith(".png"))
                {
                    //Set files to be cached for 7 days
                    TimeSpan maxAge = new TimeSpan(7, 0, 0, 0);     //7 days
                    context.Response.Headers.Append("Cache-Control", "max-age=" + maxAge.TotalSeconds.ToString("0"));
                }
                else
                {
                    //Request for views fall here.
                    context.Response.Headers.Append("Cache-Control", "no-cache");
                    context.Response.Headers.Append("Cache-Control", "private, no-store");
                }
                await next();
            });

            // app.UseStaticFiles();

            app.UseStaticFiles(
                new StaticFileOptions
                {
                    OnPrepareResponse = ctx =>
                    {
                        const int durationInSeconds = 60 * 60 * 24;
                        ctx.Context.Response.Headers[HeaderNames.CacheControl] =
                            "public,max-age=" + durationInSeconds;
                    }

                });

            //app.UseHttpsRedirection();
            //app.UseStaticFiles();

            var cookiePolicyOptions = new CookiePolicyOptions
            {
                MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.Lax,
                Secure = CookieSecurePolicy.Always
            };
            app.UseCookiePolicy(cookiePolicyOptions);

            app.UseMvc(routes =>
            {
                routes.MapRoute(name: "admin",
                    template: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}