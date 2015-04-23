using System;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Metadata.Builders;
using Microsoft.Data.Entity.Relational.Migrations.Infrastructure;
using SampleWebAppIntegration.Models;

namespace SampleWebAppIntegration.Migrations
{
    [ContextType(typeof(SampleWebAppIntegration.Models.ApplicationDbContext))]
    public class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        public override IModel Model
        {
            get
            {
                var builder = new BasicModelBuilder();

                builder.Entity("Microsoft.AspNet.Identity.IdentityRole", b =>
                {
                    b.Property<string>("Id");
                    b.Property<string>("Name");
                    b.Key("Id");
                    b.Metadata.Relational().Table = "AspNetRoles";
                });

                builder.Entity("Microsoft.AspNet.Identity.IdentityRoleClaim`1[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]", b =>
                {
                    b.Property<string>("ClaimType");
                    b.Property<string>("ClaimValue");
                    b.Property<int>("Id")
                        .GenerateValueOnAdd();
                    b.Property<string>("RoleId");
                    b.Key("Id");
                    b.Metadata.Relational().Table = "AspNetRoleClaims";
                });

                builder.Entity("Microsoft.AspNet.Identity.IdentityUserClaim`1[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]", b =>
                {
                    b.Property<string>("ClaimType");
                    b.Property<string>("ClaimValue");
                    b.Property<int>("Id")
                        .GenerateValueOnAdd();
                    b.Property<string>("UserId");
                    b.Key("Id");
                b.Metadata.Relational().Table = "AspNetUserClaims";
                    
                });

                builder.Entity("Microsoft.AspNet.Identity.IdentityUserLogin`1[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]", b =>
                {
                    b.Property<string>("LoginProvider");
                    b.Property<string>("ProviderDisplayName");
                    b.Property<string>("ProviderKey");
                    b.Property<string>("UserId");
                    b.Key("LoginProvider", "ProviderKey");
                b.Metadata.Relational().Table = "AspNetUserLogins";
                });

                builder.Entity("Microsoft.AspNet.Identity.IdentityUserRole`1[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]", b =>
                {
                    b.Property<string>("RoleId");
                    b.Property<string>("UserId");
                    b.Key("UserId", "RoleId");
                b.Metadata.Relational().Table = "AspNetUserRoles";
                });

                builder.Entity("SampleWebAppIntegration.Models.ApplicationUser", b =>
                {
                    b.Property<int>("AccessFailedCount");
                    b.Property<string>("ConcurrencyStamp");
                    b.Property<string>("Email");
                    b.Property<bool>("EmailConfirmed");
                    b.Property<string>("Id");
                    b.Property<bool>("LockoutEnabled");
                    b.Property<DateTimeOffset?>("LockoutEnd");
                    b.Property<string>("NormalizedEmail-");
                    b.Property<string>("NormalizedUserName");
                    b.Property<string>("PasswordHash");
                    b.Property<string>("PhoneNumber");
                    b.Property<bool>("PhoneNumberConfirmed");
                    b.Property<string>("SecurityStamp");
                    b.Property<bool>("TwoFactorEnabled");
                    b.Property<string>("UserName");
                    b.Key("Id");
                    b.Metadata.Relational().Table = "AspNetUsers";
                });

                builder.Entity("Microsoft.AspNet.Identity.IdentityRoleClaim`1[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]", b =>
                {
                    b.ForeignKey("Microsoft.AspNet.Identity.IdentityRole", "RoleId");
                });

                builder.Entity("Microsoft.AspNet.Identity.IdentityUserClaim`1[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]", b =>
                {
                    b.ForeignKey("SampleWebAppIntegration.Models.ApplicationUser", "UserId");
                });

                builder.Entity("Microsoft.AspNet.Identity.IdentityUserLogin`1[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]", b =>
                {
                    b.ForeignKey("SampleWebAppIntegration.Models.ApplicationUser", "UserId");
                });

                return builder.Model;
            }
        }
    }
}