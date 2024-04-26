using Gemini.Controllers.Bussiness;
using Gemini.Models;
using Gemini.Models._02_Cms.U;
using Gemini.Models._05_Website;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace Gemini.Controllers._05_Website
{
    [CustomAuthorizeAttribute]
    public class WOrderController : GeminiController
    {
        public ActionResult Index()
        {
            return RedirectToAction("List");
        }

        /// <summary>
        /// List view grid
        /// </summary>
        /// <returns></returns>
        public ActionResult List()
        {
            GetSettingUser();
            return View();
        }

        public ActionResult Read([DataSourceRequest] DataSourceRequest request)
        {
            List<WOrderModel> wOrders = (from wo in DataGemini.WOrders
                                         join su in DataGemini.SUsers on wo.GuidUser equals su.Guid
                                         //where wo.Status >= WOrder_Status.Paid
                                         select new WOrderModel
                                         {
                                             Guid = wo.Guid,
                                             OrderCode = wo.Guid.ToString(),
                                             Username = su.Username,
                                             Status = wo.Status,
                                             Mobile = wo.Mobile,
                                             FullAddress = wo.FullAddress,
                                             CreatedAt = wo.CreatedAt
                                         }).ToList();

            foreach (var item in wOrders)
            {
                item.StatusName = WOrder_Status.dicDesc[item.Status.GetValueOrDefault(WOrder_Status.Inprogress)];
            }

            DataSourceResult result = wOrders.OrderByDescending(x => x.CreatedAt).ToDataSourceResult(request);
            return Json(result);
        }

        public ActionResult ReadTabc1([DataSourceRequest] DataSourceRequest request, string guid)
        {
            List<WOrderDetailModel> wOrderDetails = (from wod in DataGemini.WOrderDetails
                                                     join pp in DataGemini.PosProduces on wod.GuidProduce equals pp.Guid
                                                     where wod.GuidOrder != null && wod.GuidOrder.ToString().ToLower() == guid
                                                     select new WOrderDetailModel
                                                     {
                                                         ProduceCode = pp.Code,
                                                         ProduceName = pp.Name,
                                                         Quantity = wod.Quantity,
                                                         Price = wod.Price,
                                                         Size = wod.Size,
                                                         Color = wod.Color,
                                                         ListGallery = (from fr in DataGemini.FProduceGalleries
                                                                        join im in DataGemini.UGalleries on fr.GuidGallery equals im.Guid
                                                                        where fr.GuidProduce == pp.Guid
                                                                        select new UGalleryModel
                                                                        {
                                                                            Image = im.Image,
                                                                            CreatedAt = im.CreatedAt
                                                                        }).OrderBy(x => x.CreatedAt).Take(1).ToList(),
                                                     }).ToList();

            foreach (var item in wOrderDetails)
            {
                var tmpLinkImg = item.ListGallery;
                if (tmpLinkImg.Count == 0)
                {
                    item.ProduceLinkImg0 = "/Content/Custom/empty-album.png";
                }
                else
                {
                    item.ProduceLinkImg0 = tmpLinkImg[0].Image;
                }
            }

            DataSourceResult result = wOrderDetails.ToDataSourceResult(request);
            return Json(result);
        }

        public ActionResult ConfirmOrder(Guid guid)
        {
            var wOrder = new WOrder();
            try
            {
                wOrder = DataGemini.WOrders.FirstOrDefault(c => c.Guid == guid);
                var lstErrMsg = Validate_Approval(wOrder);

                if (lstErrMsg.Count > 0)
                {
                    DataReturn.StatusCode = Convert.ToInt16(HttpStatusCode.Conflict);
                    DataReturn.MessagError = String.Join("<br/>", lstErrMsg);
                }
                else
                {
                    wOrder.Status = WOrder_Status.Confirm;
                    wOrder.UpdatedAt = DateTime.Now;
                    wOrder.UpdatedBy = GetUserInSession();

                    if (SaveData("WOrder") && wOrder != null)
                    {
                        DataReturn.StatusCode = Convert.ToInt16(HttpStatusCode.OK);
                    }
                    else
                    {
                        DataReturn.StatusCode = Convert.ToInt16(HttpStatusCode.Conflict);
                        DataReturn.MessagError = Constants.CannotUpdate + " Date : " + DateTime.Now;
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }

            return Json(DataReturn, JsonRequestBehavior.AllowGet);
        }

        public ActionResult RejectOrder(Guid guid)
        {
            var wOrder = new WOrder();
            try
            {
                wOrder = DataGemini.WOrders.FirstOrDefault(c => c.Guid == guid);
                var lstErrMsg = Validate_Approval(wOrder);

                if (lstErrMsg.Count > 0)
                {
                    DataReturn.StatusCode = Convert.ToInt16(HttpStatusCode.Conflict);
                    DataReturn.MessagError = String.Join("<br/>", lstErrMsg);
                }
                else
                {
                    wOrder.Status = WOrder_Status.Reject;
                    wOrder.UpdatedAt = DateTime.Now;
                    wOrder.UpdatedBy = GetUserInSession();

                    if (SaveData("WOrder") && wOrder != null)
                    {
                        DataReturn.StatusCode = Convert.ToInt16(HttpStatusCode.OK);
                    }
                    else
                    {
                        DataReturn.StatusCode = Convert.ToInt16(HttpStatusCode.Conflict);
                        DataReturn.MessagError = Constants.CannotUpdate + " Date : " + DateTime.Now;
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }

            return Json(DataReturn, JsonRequestBehavior.AllowGet);
        }

        private List<string> Validate_Approval(WOrder wOrder)
        {
            List<string> lstErrMsg = new List<string>();

            //if (wOrder.Status < WOrder_Status.Paid)
            //{
            //    lstErrMsg.Add("Đơn hàng chưa thanh toán, không thể xác nhận/từ chối!");
            //}

            if (wOrder.Status >= WOrder_Status.Confirm)
            {
                lstErrMsg.Add("Đơn hàng đã qua bước xác nhận, không thể sửa!");
            }

            return lstErrMsg;
        }
    }
}