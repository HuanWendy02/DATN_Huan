
using MoMo;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ShopNoiThat.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;

namespace ShopNoiThat.Controllers
{
    public class CheckoutController : BaseController
    {
        private const string SessionCart = "SessionCart";
        ShopNoiThatDbContext db = new ShopNoiThatDbContext();
        public ActionResult Index()
        {
            var cart = Session[SessionCart];
            var list = new List<Cart_item>();
            if (cart != null)
            {
                list = (List<Cart_item>)cart;
            }
            return View(list);

        }

        public static string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }

            return hash.ToString();
        }


        [HttpPost]
        public ActionResult Index(Morder order)
        {
            Random rand = new Random((int)DateTime.Now.Ticks);
            int numIterations = 0;
            numIterations = rand.Next(1, 100000);
            DateTime time = DateTime.Now;

            string orderCode = numIterations + "" + time;
            double sumOrder = Convert.ToDouble(Request["sumOrder"]);
            string payment_method = Request["option_payment"];
            // Neu Ship COde
            if (payment_method.Equals("COD"))
            {
                // cap nhat thong tin sau khi dat hang thanh cong

                saveOrder(order, "COD", 2, orderCode);
                var cart = Session[SessionCart];
                var list = new List<Cart_item>();
                ViewBag.cart = (List<Cart_item>)cart;
                Session["SessionCart"] = null;
                var listProductOrder = db.Orderdetails.Where(m => m.orderid == order.ID);
                return View("payment");
            }
            if (payment_method.Equals("Vnpay"))
            {
                saveOrder(order, "Vnpay", 1, orderCode);
                var cart = Session[SessionCart];
                var list = new List<Cart_item>();
                ViewBag.cart = (List<Cart_item>)cart;
                Session["SessionCart"] = null;
                // Lấy giá trị tổng tiền thanh toán
                var total = sumOrder * 100;
                
                string url = "http://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
                string returnUrl = "http://localhost:22222/vnpay-success";
                string tmnCode = "2TNEXMQ8";
                string hashSecret = "NTELFBCCDPWBIXLZQRTUCNLOYNVFNANA";

                PayLib pay = new PayLib();

                pay.AddRequestData("vnp_Version", "2.1.0"); //Phiên bản api mà merchant kết nối. Phiên bản hiện tại là 2.1.0
                pay.AddRequestData("vnp_Command", "pay"); //Mã API sử dụng, mã cho giao dịch thanh toán là 'pay'
                pay.AddRequestData("vnp_TmnCode", tmnCode); //Mã website của merchant trên hệ thống của VNPAY (khi đăng ký tài khoản sẽ có trong mail VNPAY gửi về)
                pay.AddRequestData("vnp_Amount", total.ToString()); //số tiền cần thanh toán, công thức: số tiền * 100 - ví dụ 10.000 (mười nghìn đồng) --> 1000000
                pay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss")); //ngày thanh toán theo định dạng yyyyMMddHHmmss
                pay.AddRequestData("vnp_CurrCode", "VND"); //Đơn vị tiền tệ sử dụng thanh toán. Hiện tại chỉ hỗ trợ VND
                pay.AddRequestData("vnp_IpAddr", UtilVnpay.GetIpAddress()); //Địa chỉ IP của khách hàng thực hiện giao dịch
                pay.AddRequestData("vnp_Locale", "VN"); //Ngôn ngữ giao diện hiển thị - Tiếng Việt (vn), Tiếng Anh (en)
                pay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang"); //Thông tin mô tả nội dung thanh toán
                pay.AddRequestData("vnp_OrderType", "other"); //topup: Nạp tiền điện thoại - billpayment: Thanh toán hóa đơn - fashion: Thời trang - other: Thanh toán trực tuyến
                pay.AddRequestData("vnp_ReturnUrl", returnUrl); //URL thông báo kết quả giao dịch khi Khách hàng kết thúc thanh toán
                pay.AddRequestData("vnp_TxnRef", DateTime.Now.Ticks.ToString()); //mã hóa đơn

                string paymentUrl = pay.CreateRequestUrl(url, hashSecret);
                System.Diagnostics.Debug.WriteLine("vnp_Url: " + paymentUrl);

                return Redirect(paymentUrl);
            }
            if (payment_method.Equals("MoMo"))
            {
                // cap nhat thong tin sau khi dat hang thanh cong

                saveOrder(order, "MoMo", 2, orderCode);
                var cart = Session[SessionCart];
                var list = new List<Cart_item>();
                ViewBag.cart = (List<Cart_item>)cart;
                Session["SessionCart"] = null;
                var listProductOrder = db.Orderdetails.Where(m => m.orderid == order.ID);
                string endpoint = "https://test-payment.momo.vn/gw_payment/transactionProcessor";
                string partnerCode = "MOMOOJOI20210710";
                string accessKey = "iPXneGmrJH0G8FOP";
                string serectkey = "sFcbSGRSJjwGxwhhcEktCHWYUuTuPNDB";
                string orderInfo = "Thanh toán hóa đơn";
                string returnUrl = "https://localhost:44394/Home/ConfirmPaymentClient";
                string notifyurl = "https://4c8d-2001-ee0-5045-50-58c1-b2ec-3123-740d.ap.ngrok.io/Home/SavePayment"; //lưu ý: notifyurl không được sử dụng localhost, có thể sử dụng ngrok để public localhost trong quá trình test
                var tien = sumOrder;
                string amount = tien.ToString();
                string orderid = orderCode; //mã đơn hàng
                string requestId = DateTime.Now.Ticks.ToString();
                string extraData = "";

                //Before sign HMAC SHA256 signature
                string rawHash = "partnerCode=" +
                    partnerCode + "&accessKey=" +
                    accessKey + "&requestId=" +
                    requestId + "&amount=" +
                    amount + "&orderId=" +
                    orderid + "&orderInfo=" +
                    orderInfo + "&returnUrl=" +
                    returnUrl + "&notifyUrl=" +
                    notifyurl + "&extraData=" +
                    extraData;

                MoMoSecurity crypto = new MoMoSecurity();
                //sign signature SHA256
                string signature = crypto.signSHA256(rawHash, serectkey);

                //build body json request
                JObject message = new JObject
                {
                { "partnerCode", partnerCode },
                { "accessKey", accessKey },
                { "requestId", requestId },
                { "amount", amount },
                { "orderId", orderid },
                { "orderInfo", orderInfo },
                { "returnUrl", returnUrl },
                { "notifyUrl", notifyurl },
                { "extraData", extraData },
                { "requestType", "captureMoMoWallet" },
                { "signature", signature }

            };

                string responseFromMomo = PaymentRequest.sendPaymentRequest(endpoint, message.ToString());

                JObject jmessage = JObject.Parse(responseFromMomo);
                return Redirect(jmessage.GetValue("payUrl").ToString());
            }
            //Neu Thanh toan Ngan Luong
            return View("payment");
        }

        public ViewResult vnpaySuccess()
        {
            return View("success_vnpay");
        }

        public static string HMACSHA512(string key, string data)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);

            using (HMACSHA512 hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashBytes = hmac.ComputeHash(dataBytes);
                StringBuilder hashBuilder = new StringBuilder();

                // Convert byte array to hexadecimal string
                foreach (byte hashByte in hashBytes)
                {
                    hashBuilder.Append(hashByte.ToString("x2"));
                }

                return hashBuilder.ToString();
            }
        }
        //Khi huy thanh toán Ngan Luong
        public ActionResult cancel_order()
        {

            return View("cancel_order");
        }
        //Khi thanh toán Ngan Luong XOng
        public ActionResult confirm_orderPaymentOnline()
        {

            //String Token = Request["token"];
            //RequestCheckOrder info = new RequestCheckOrder();
            //info.Merchant_id = nganluongInfo.Merchant_id;
            //info.Merchant_password = nganluongInfo.Merchant_password;
            //info.Token = Token;
            //APICheckoutV3 objNLChecout = new APICheckoutV3();
            //ResponseCheckOrder result = objNLChecout.GetTransactionDetail(info);
            //if (result.errorCode=="00")
            //{
            //    var cart = Session[SessionCart];
            //    var list = new List<Cart_item>();
            //    ViewBag.cart = (List<Cart_item>)cart;
            //    Session["SessionCart"] = null;
            //    var OrderInfo = db.Orders.OrderByDescending(m=>m.ID).FirstOrDefault();
            //    ViewBag.name = OrderInfo.deliveryname;
            //    ViewBag.email = OrderInfo.deliveryemail;
            //    ViewBag.address = OrderInfo.deliveryaddress;
            //    ViewBag.code = OrderInfo.code;
            //    ViewBag.phone = OrderInfo.deliveryphone;
            //    OrderInfo.StatusPayment = 1;
            //    db.Entry(OrderInfo).State = EntityState.Modified;
            //    db.SaveChanges();
            //    ViewBag.paymentStatus = OrderInfo.StatusPayment;
            //    ViewBag.Methodpayment = OrderInfo.deliveryPaymentMethod;
            //    return View("payment");
            //}
            //else
            //{
            //     ViewBag.status = false;
            //}

            return View("confirm_orderPaymentOnline");
        }

       

        //function ssave order when order success!
        public void saveOrder(Morder order, string paymentMethod, int StatusPayment, string ordercode)
        {
            var cart = Session[SessionCart];
            var list = new List<Cart_item>();
            if (cart != null)
            {
                list = (List<Cart_item>)cart;
            }

            if (ModelState.IsValid)
            {

                order.code = ordercode;
                order.userid = Convert.ToInt32(Session["id"]);
                order.deliveryPaymentMethod = paymentMethod;
                order.StatusPayment = StatusPayment;
                order.created_ate = DateTime.Now;
                order.updated_by = 1;
                order.updated_at = DateTime.Now;
                order.updated_by = 1;
                order.status = 2;
                order.exportdate = DateTime.Now;
                db.Orders.Add(order);
                db.SaveChanges();
                ViewBag.name = order.deliveryname;
                ViewBag.email = order.deliveryemail;
                ViewBag.address = order.deliveryaddress;
                ViewBag.code = order.code;
                ViewBag.phone = order.deliveryphone;
                Mordersdetail orderdetail = new Mordersdetail();

                foreach (var item in list)
                {
                    float price = 0;
                    int sale = (int)item.product.pricesale;
                    if (sale > 0)
                    {
                        price = (float)item.product.price - (int)item.product.price / 100 * (int)sale * item.quantity;
                    }
                    else
                    {
                        price = (float)item.product.price * (int)item.quantity;
                    }
                    orderdetail.orderid = order.ID;
                    orderdetail.productid = item.product.ID;
                    orderdetail.priceSale = (int)item.product.pricesale;
                    orderdetail.price = item.product.price;
                    orderdetail.quantity = item.quantity;
                    orderdetail.amount = price;

                    db.Orderdetails.Add(orderdetail);
                    db.SaveChanges();
                    var updatedProduct = db.Products.Find(item.product.ID);
                    updatedProduct.number = (int)updatedProduct.number - (int)item.quantity;
                    db.Products.Attach(updatedProduct);
                    db.Entry(updatedProduct).State = EntityState.Modified;
                    db.SaveChanges();
                }

            }
        }
        //
    }
}