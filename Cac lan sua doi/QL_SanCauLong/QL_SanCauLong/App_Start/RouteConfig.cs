using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace QL_SanCauLong
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Route cho mặc định: chuyển hướng về ViewDatSan
            routes.MapRoute(
                name: "Default",
                url: "",
                defaults: new { controller = "Booking", action = "ViewDatSan" }
            );

            // Các route còn lại
            routes.MapRoute(
                name: "AdminQuanLy",
                url: "AdminQuanLy/{action}/{id}",
                defaults: new { controller = "AdminQuanLy", action = "Booking", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Other",
                url: "{controller}/{action}/{id}",
                defaults: new { id = UrlParameter.Optional }
            );
        }
    }
}
