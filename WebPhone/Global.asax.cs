using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using WebPhone.Services;
using Unity;
using Unity.AspNet.Mvc;
using Unity.Lifetime;

namespace WebPhone
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // Cấu hình Dependency Injection
            var container = new UnityContainer();
            container.RegisterType<SendMailService>(new HierarchicalLifetimeManager()); // Scoped
            // Scoped: HierarchicalLifetimeManager
            // Transient: TransientLifetimeManager
            // Singleton: ContainerControlledLifetimeManager

            DependencyResolver.SetResolver(new UnityDependencyResolver(container));

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}
