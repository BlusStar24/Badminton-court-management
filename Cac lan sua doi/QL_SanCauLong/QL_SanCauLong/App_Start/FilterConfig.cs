using System.Web;
using System.Web.Mvc;

namespace QL_SanCauLong
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            // KHÔNG thêm dòng sau:
            // filters.Add(new AuthorizeAttribute());
        }
    }
}