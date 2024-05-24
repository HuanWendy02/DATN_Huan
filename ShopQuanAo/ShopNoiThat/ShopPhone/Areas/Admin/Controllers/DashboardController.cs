    using ShopNoiThat.Common;
using ShopNoiThat.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ShopNoiThat.Areas.Admin.Controllers
{

    public class DashboardController : BaseController
    {
        private ShopNoiThatDbContext db = new ShopNoiThatDbContext();
        // GET: Admin/Dashboard
       
        public ActionResult Index()
        {

            ViewBag.product = db.Products.Count();
            ViewBag.Neworder = db.Orders.Where(m => m.status == 2).Count();
            ViewBag.contact = db.Contacts.Where(m => m.status == 2).Count();
            ViewBag.user = db.users.Where(m=> m.status ==1 && m.access==1).Count();
            ViewBag.category = db.Categorys.Count();
            ViewBag.post = db.posts.Count();
            ViewBag.topic = db.topics.Count();
            ViewBag.slider = db.Sliders.Count();
            //
            return View();
        }
        public ActionResult CallSessionForLayout()
        {
            ViewBag.adminName = Session["Admin_user"];
            ViewBag.adminID = int.Parse(Session["Admin_id"].ToString());
            ViewBag.adminFull = Session["Admin_fullname"];
            return View("_userNav");
        }

        [CustomAuthorizeAttribute(RoleID = "ACCOUNTANT")]
        public ActionResult statistics()
        {
            var allOrders = db.Orders.ToList();
            var allOrderDetails = db.Orderdetails.ToList();
            DateTime endDate = DateTime.Today;
            DateTime startDate = endDate.AddDays(-6);

            // Initialize lists to store dates and amounts
            List<String> dates = new List<String>();
            List<double?> amounts = new List<double?>();
            ViewBag.orderDay = 0;
            ViewBag.orderWeek = 0;
            ViewBag.productDay = 0;
            ViewBag.productWeek = 0;
            int dem = 0;
            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                dem++;

                // Get successful orders for the current day
                var successfulOrders = db.Orders
                    .Where(o => o.status == 4 && DbFunctions.TruncateTime(o.updated_at) == date.Date)
                    .Select(o => o.ID)
                    .ToList();

                var successfulOrders2 = db.Orders
                    .Where(o => o.status == 4 && DbFunctions.TruncateTime(o.updated_at) == date.Date)
                    .Select(o => o.ID)
                    .ToList();

                var successfulOrders3 = db.Orders
                    .Where(o => o.status == 4 && DbFunctions.TruncateTime(o.updated_at) == date.Date)
                    .Select(o => o.ID)
                    .ToList();

                // Calculate the total amount for the current day
                double? totalAmount = db.Orderdetails
                    .Where(od => successfulOrders.Contains(od.orderid))
                    .Sum(od => (double?)(od.price * od.quantity)); 

              

                int? totalNumbert = db.Orderdetails
                   .Where(oda => successfulOrders2.Contains(oda.orderid))
                   .Sum(oda => (int?)(oda.quantity));


                if(totalNumbert == null)
                {
                    totalNumbert = 0;
                }
                ViewBag.orderWeek += successfulOrders3.Count;
                ViewBag.productWeek += totalNumbert;
                if(dem == 7)
                {
                    ViewBag.orderDay += successfulOrders3.Count;
                    ViewBag.productDay += totalNumbert;
                    Debug.WriteLine($"aTotal number of products on {date.ToString("dd/MM/yyyy")}: {totalNumbert}");

                }

                // Add date and amount to the lists
                string formattedDate = date.ToString("dd/MM/yyyy");
                if(totalAmount == null)
                {
                    totalAmount = 0;
                }
                dates.Add(formattedDate);
                amounts.Add(totalAmount);
            }

            // Convert lists to arrays
            String[] dateArray = dates.ToArray();
            double?[] amountArray = amounts.ToArray();

            ViewBag.dateArray = dateArray;
            ViewBag.amountArray = amountArray;


            //order weed
           
            return View("_Statistical");
        }
        public string CallSessionFullname()
        {
            //ViewBag.admiUser = Session["Admin_user"];
            string userFullname = Session["Admin_fullname"].ToString();
            return userFullname;
        }

    }
}