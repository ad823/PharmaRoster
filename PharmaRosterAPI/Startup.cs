using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace PharmaRosterAPI
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
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder
                        .SetIsOriginAllowed(origin => true)
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });

            });
            services.AddControllers();
            services.Configure<ForwardedHeadersOptions>(opts =>
            {
                opts.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                // 如有固定可信任 Proxy，可加入：
                // opts.KnownProxies.Add(IPAddress.Parse("172.18.0.2"));
            });

        
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "PharmaRosterAPI", Version = "v1" });

                var xmlPath = Path.Combine(AppContext.BaseDirectory, $"PharmaRosterAPI.xml");

                c.IncludeXmlComments(xmlPath, true);
                c.OrderActionsBy(s => s.RelativePath);

            });

            // JWT 驗證（只有 [Authorize] 才會啟用）
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        // 驗證簽發者
                        ValidateIssuer = true,
                        ValidIssuer = JwtHelper.Issuer,

                        // 驗證接收者
                        ValidateAudience = true,
                        ValidAudience = JwtHelper.Audience,

                        // 驗證過期時間
                        ValidateLifetime = true,

                        // 驗證簽章
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(
                               JwtHelper.SecretKey
                            )
                        ),

                        // 醫療系統建議縮短時間誤差
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseForwardedHeaders();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors(builder =>
            {
                builder
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .SetIsOriginAllowed(_ => true)
                    .AllowCredentials();
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseWebSockets();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseSwagger();
            app.UseSwaggerUI();
        }

    }
}
