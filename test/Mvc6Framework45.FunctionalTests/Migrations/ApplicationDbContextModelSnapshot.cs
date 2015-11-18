using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Metadata.Builders;
using Mvc6Framework45.FunctionalTests.Models;

namespace Mvc6Framework45.FunctionalTests.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder builder)
        {
            builder.HasAnnotation("SqlServer:ValueGeneration", "Identity");

            builder.Entity("Mvc6Framework45.FunctionalTests.Models.ApplicationUser", b =>
                {
                    b.Property<int>("AccessFailedCount")
                        .HasAnnotation("OriginalValueIndex", 0);
                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasAnnotation("OriginalValueIndex", 1);
                    b.Property<string>("Email")
                        .HasAnnotation("OriginalValueIndex", 2);
                    b.Property<bool>("EmailConfirmed")
                        .HasAnnotation("OriginalValueIndex", 3);
                    b.Property<string>("Id")
                        .HasAnnotation("OriginalValueIndex", 4);
                    b.Property<bool>("LockoutEnabled")
                        .HasAnnotation("OriginalValueIndex", 5);
                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasAnnotation("OriginalValueIndex", 6);
                    b.Property<string>("NormalizedEmail")
                        .HasAnnotation("OriginalValueIndex", 7);
                    b.Property<string>("NormalizedUserName")
                        .HasAnnotation("OriginalValueIndex", 8);
                    b.Property<string>("PasswordHash")
                        .HasAnnotation("OriginalValueIndex", 9);
                    b.Property<string>("PhoneNumber")
                        .HasAnnotation("OriginalValueIndex", 10);
                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasAnnotation("OriginalValueIndex", 11);
                    b.Property<string>("SecurityStamp")
                        .HasAnnotation("OriginalValueIndex", 12);
                    b.Property<bool>("TwoFactorEnabled")
                        .HasAnnotation("OriginalValueIndex", 13);
                    b.Property<string>("UserName")
                        .HasAnnotation("OriginalValueIndex", 14);
                    b.HasKey("Id");
                    b.HasAnnotation("Relational:TableName", "AspNetUsers");
                });

            builder.Entity("Microsoft.AspNet.Identity.EntityFramework.IdentityRole", b =>
                {
                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasAnnotation("OriginalValueIndex", 0);
                    b.Property<string>("Id")
                        .HasAnnotation("OriginalValueIndex", 1);
                    b.Property<string>("Name")
                        .HasAnnotation("OriginalValueIndex", 2);
                    b.Property<string>("NormalizedName")
                        .HasAnnotation("OriginalValueIndex", 3);
                    b.HasKey("Id");
                    b.HasAnnotation("Relational:TableName", "AspNetRoles");
                });

            builder.Entity("Microsoft.AspNet.Identity.EntityFramework.IdentityRoleClaim`1[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]", b =>
                {
                    b.Property<string>("ClaimType")
                        .HasAnnotation("OriginalValueIndex", 0);
                    b.Property<string>("ClaimValue")
                        .HasAnnotation("OriginalValueIndex", 1);
                    b.Property<int>("Id")
                        .HasAnnotation("OriginalValueIndex", 2)
                        .HasAnnotation("SqlServer:ValueGeneration", "Default");
                    b.Property<string>("RoleId")
                        .HasAnnotation("OriginalValueIndex", 3);
                    b.HasKey("Id");
                    b.HasAnnotation("Relational:TableName", "AspNetRoleClaims");
                });

            builder.Entity("Microsoft.AspNet.Identity.EntityFramework.IdentityUserClaim`1[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]", b =>
                {
                    b.Property<string>("ClaimType")
                        .HasAnnotation("OriginalValueIndex", 0);
                    b.Property<string>("ClaimValue")
                        .HasAnnotation("OriginalValueIndex", 1);
                    b.Property<int>("Id")
                        .HasAnnotation("OriginalValueIndex", 2)
                        .HasAnnotation("SqlServer:ValueGeneration", "Default");
                    b.Property<string>("UserId")
                        .HasAnnotation("OriginalValueIndex", 3);
                    b.HasKey("Id");
                    b.HasAnnotation("Relational:TableName", "AspNetUserClaims");
                });

            builder.Entity("Microsoft.AspNet.Identity.EntityFramework.IdentityUserLogin`1[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]", b =>
                {
                    b.Property<string>("LoginProvider")
                        .HasAnnotation("OriginalValueIndex", 0);
                    b.Property<string>("ProviderDisplayName")
                        .HasAnnotation("OriginalValueIndex", 1);
                    b.Property<string>("ProviderKey")
                        .HasAnnotation("OriginalValueIndex", 2);
                    b.Property<string>("UserId")
                        .HasAnnotation("OriginalValueIndex", 3);
                    b.HasKey("LoginProvider", "ProviderKey");
                    b.HasAnnotation("Relational:TableName", "AspNetUserLogins");
                });

            builder.Entity("Microsoft.AspNet.Identity.EntityFramework.IdentityUserRole`1[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]", b =>
                {
                    b.Property<string>("RoleId")
                        .HasAnnotation("OriginalValueIndex", 0);
                    b.Property<string>("UserId")
                        .HasAnnotation("OriginalValueIndex", 1);
                    b.HasKey("UserId", "RoleId");
                    b.HasAnnotation("Relational:TableName", "AspNetUserRoles");
                });

            builder.Entity("Microsoft.AspNet.Identity.EntityFramework.IdentityRoleClaim`1[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]", b =>
                {
                    //b.ForeignKey("Microsoft.AspNet.Identity.EntityFramework.IdentityRole", "RoleId");
                });

            builder.Entity("Microsoft.AspNet.Identity.EntityFramework.IdentityUserClaim`1[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]", b =>
                {
                    //b.ForeignKey("Mvc6Framework45.FunctionalTests.Models.ApplicationUser", "UserId");
                });

            builder.Entity("Microsoft.AspNet.Identity.EntityFramework.IdentityUserLogin`1[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]", b =>
                {
                    //b.ForeignKey("Mvc6Framework45.FunctionalTests.Models.ApplicationUser", "UserId");
                });

            builder.Entity("Microsoft.AspNet.Identity.EntityFramework.IdentityUserRole`1[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]", b =>
                {
                    //b.ForeignKey("Microsoft.AspNet.Identity.EntityFramework.IdentityRole", "RoleId");
                    //b.ForeignKey("Mvc6Framework45.FunctionalTests.Models.ApplicationUser", "UserId");
                });
        }
    }
}
