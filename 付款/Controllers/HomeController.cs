using System;
using System.Collections.Generic;
using System.Data.Objects.SqlClient;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using project.Models;
using project.Models.ViewModel;

namespace project.Controllers
{
    public class HomeController : Controller
    {
        project_v5 db = new project_v5();
        public ActionResult Index()
        {
            var products = db.tProduct.ToList();
            if (Session["Member"] == null)
            {
                return View("Index", "_Layout", products);
            }
            return View("Index", "_LayoutMember", products);
        }
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Login(string fUserId, string fPwd)
        {
            var member = db.tMember
                .Where(m => m.fUserId == fUserId && m.fPwd == fPwd)
                .FirstOrDefault();
            if (member == null)
            {
                ViewBag.Message = "帳密錯誤, 登入失敗";
                return View();
            }
            Session["Welcome"] = member.fName + "歡迎光臨";
            Session["Member"] = member;
            return RedirectToAction("Index");
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Register(tMember member)
        {
            if (ModelState.IsValid == false)
            {
                return View();
            }
            var pmember = db.tMember
                .Where(m => m.fUserId == member.fUserId)
                .FirstOrDefault();
            if (pmember == null)
            {
                db.tMember.Add(member);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.Message = member.fUserId + "已有人使用,請重新註冊";
            return View();
        }
        //顯示fUserId自己的購物車內容
        public ActionResult ShoppingCar()
        {
            Session["OrderGuid"] = Guid.NewGuid().ToString();
            string fuserid = (Session["Member"] as tMember).fUserId;
            //var shoppingCar = db.tShoppingCar
            //    .Where(s => s.fUserId == fuserid && s.fIsApproved == "否")
            //    //.Join(db.tProduct, p => p.fPId, s.fPId, (p, s) => )
            //    .ToList();

            var shoppingCar = (from s in db.tShoppingCar
                              where s.fUserId == fuserid && s.fIsApproved == "否"
                              join p in db.tProduct on s.fPid equals p.fPId
                              select new ShoppingCarItem
                              {
                                  UserId = fuserid,
                                  Fid = s.fid,
                                  Pid = s.fPid,
                                  Name = p.fName,
                                  Qty = s.fQty,
                                  Price =  p.fPrice,
                                  IsApproved = s.fIsApproved
                              }).ToList();
            return View("ShoppingCar", "_LayoutMember", shoppingCar);
        }
        //按下按鈕加入購物車並存入tshoppingcar
        public ActionResult AddCar(string fPId)
        {
            string fUserId = ((tMember)Session["Member"]).fUserId;
            //商業邏輯層
            var currentCar = db.tShoppingCar
                .Where(p => p.fPid == fPId && p.fIsApproved == "否" && p.fUserId == fUserId)
                .FirstOrDefault();
            if (currentCar == null)
            {
                var product = db.tProduct
                    .Where(p => p.fPId == fPId)
                    .FirstOrDefault();
                tShoppingCar shoppingCar = new tShoppingCar();
                shoppingCar.fUserId = fUserId;
                //TODO 刪除tShoppingCar欄位
                shoppingCar.fPid = fPId;
                shoppingCar.fQty = 1;
                shoppingCar.fIsApproved = "否";
                db.tShoppingCar.Add(shoppingCar);
            }
            else
            {
                currentCar.fQty++;
            }
            db.SaveChanges();
            //
            return RedirectToAction("ShoppingCar");
        }
        //購物車畫面輸入fReceiver,fMail,fAddress後存入db.tOrder
        [HttpPost]
        public ActionResult ShoppingCar(FormCollection formCollection, string fReceiver, string fEMail, string fAddress)
        {
            // 获取用户勾选的商品数量
            string quantityJson = formCollection["quantity"];
            Dictionary<string, int> itemQuantities = JsonConvert.DeserializeObject<Dictionary<string, int>>(quantityJson);

            // 获取当前用户的ID
            string fUserId = ((tMember)Session["Member"]).fUserId;

            // 创建订单
            string guid = Session["OrderGuid"].ToString();
            tOrder order = new tOrder();
            order.fOrderGuid = guid;
            order.fUserId = fUserId;
            order.fReceiver = fReceiver;
            order.fEmail = fEMail;
            order.fAddress = fAddress;
            order.fDate = DateTime.Now;
            order.fPaid = "未付款";

            // 保存订单
            db.tOrder.Add(order);

            // 遍历itemQuantities字典，处理每个商品
            foreach (var item in itemQuantities)
            {
                string fPId = item.Key;
                int quantity = item.Value;

                // 根据商品ID找到对应的商品
                var product = db.tProduct.FirstOrDefault(p => p.fPId == fPId);

                if (product != null)
                {
                    // 假设tOrderList是您要新增的数据表
                    tOrderDetail orderDetail = new tOrderDetail();
                    orderDetail.fOrderGuid = guid;
                    orderDetail.fUserId = fUserId;
                    orderDetail.fPId = fPId;
                    orderDetail.fName = product.fName;
                    orderDetail.fPrice = product.fPrice;
                    orderDetail.fQty = quantity;
                    orderDetail.fIsApproved = "是";
                    // 保存订单详情
                    db.tOrderDetail.Add(orderDetail);
                    // 找到对应的购物车记录并删除
                    var shoppingCarItem = db.tShoppingCar.FirstOrDefault(p => p.fPid == fPId && p.fIsApproved == "否" && p.fUserId == fUserId);
                    if (shoppingCarItem != null)
                    {
                        db.tShoppingCar.Remove(shoppingCarItem);
                    }
                }
            }
            // 提交数据库更改
            db.SaveChanges();
            return RedirectToAction("Payment");

        }

        public ActionResult OrderList(string searchOrder)
        {

            string fUserId = ((tMember)Session["Member"]).fUserId;
            var orderList = db.tOrder
                .Where(m => m.fUserId == fUserId)
                .OrderByDescending(m => m.fDate)
                .ToList();
            if (!string.IsNullOrEmpty(searchOrder))
            {
                // 按空格分隔搜索条件
                var searchTerms = searchOrder.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);


                foreach (var term in searchTerms)
                {
                    // 嘗試解析日期輸入，如果成功，則分別比較日期的年、月和日部分
                    if (DateTime.TryParse(term, out DateTime parsedDate))
                    {
                        int year = parsedDate.Year;
                        int month = parsedDate.Month;
                        int day = parsedDate.Day;

                        orderList = orderList.Where(c =>
                            SqlFunctions.DatePart("year", c.fDate) == year &&
                            SqlFunctions.DatePart("month", c.fDate) == month &&
                            SqlFunctions.DatePart("day", c.fDate) == day ||
                            c.fPaid.Contains(term)
                        ).ToList();
                    }
                    else
                    {
                        orderList = orderList.Where(c =>
                            c.fPaid.Contains(term)
                        ).ToList();
                    }
                }

            }
            return View("OrderList", "_LayoutMember", orderList);
        

    }
    public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
        public ActionResult Payment()
        {

            return View("Payment", "_LayoutMember");
        }
        public ActionResult Delete(int fId)
        {
            string fUserId = ((tMember)Session["Member"]).fUserId;
            var deletecar = db.tOrder
                .Where(p => p.fId == fId && p.fUserId == fUserId)
                .FirstOrDefault();
            if (deletecar != null)
            {
                db.tOrder.Remove(deletecar);
            }
            db.SaveChanges();
            return RedirectToAction("OrderList");
        }
        public ActionResult Deletecar(int fId)
        {
            string fUserId = ((tMember)Session["Member"]).fUserId;
            var deletecar = db.tShoppingCar
                .Where(p => p.fid == fId && p.fUserId == fUserId)
                .FirstOrDefault();
            if (deletecar != null)
            {
                db.tShoppingCar.Remove(deletecar);
            }
            db.SaveChanges();
            return RedirectToAction("ShoppingCar");
        }
        [HttpPost]
        public ActionResult ProcessPayment(int orderId)
        {
            var order = db.tOrder.Find(orderId);

            if (order != null)
            {
                // 更新訂單狀態為已完成付款
                order.fPaid = "已付款完成";

                // 提交更改到資料庫
                db.SaveChanges();

                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }
        }

        public ActionResult bank()
        {
            string fUserId = ((tMember)Session["Member"]).fUserId;
            var order = db.tOrder
         .Where(m => m.fUserId == fUserId && m.fPaid != "已付款")
         .OrderByDescending(m => m.fDate)
         .ToList();
            ViewBag.OrderList = order;
            return View("bank", "_LayoutMember");
        }
    }
}