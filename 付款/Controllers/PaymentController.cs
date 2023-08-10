using Newtonsoft.Json;
using project.Models;
using project.Models.Confirm;
using project.Models.Request;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

public class PaymentController : Controller
{
    private project_v5 db = new project_v5();

    [HttpPost]
    public async Task<RedirectResult> APIConfirm(string transactionId, string orderId, string amount, string currency)
    {
        PaymentConfirmDto dto = new PaymentConfirmDto
        {
            Amount = int.Parse(amount),
            Currency = currency
        };

        HttpClient client = new HttpClient();
        string requestData = JsonConvert.SerializeObject(dto);

        HttpResponseMessage response = await client.PostAsync($"https://localhost:44375/api/LinePay/Confirm?transactionId={transactionId}&orderId={orderId}",
        new StringContent(requestData, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        var apiResponse = await response.Content.ReadAsStringAsync();
        var confirmResponse = JsonConvert.DeserializeObject<PaymentConfirmResponseDto>(apiResponse);

        if (confirmResponse.ReturnCode == "0000")
        {
            string guid = Session["OrderGuid"].ToString();
            var order = db.tOrder
           .FirstOrDefault(m => m.fOrderGuid == guid);
            if (order != null)
            {
                order.fPaid = "已付款";
                db.SaveChanges();
            }

            // RedirectToAction("Finish");    是在 API 中呼叫的，而不是從網頁瀏覽器發起的 HTTP 請求
            return Redirect("~/Payment/finish");
        }
        else
        {
            //RedirectToAction("Cancel");
            return Redirect("~/Payment/Cancel");
        }
    }

    //Payment/APIRequest
    [HttpGet]
    public async Task<ActionResult> APIRequest()
    {
        // 取得會員使用者ID
        string fUserId = ((tMember)Session["Member"]).fUserId;
        string orderGuid = Session["OrderGuid"].ToString();

        // 從資料庫中取得顧客在購物車的資料
        var productList = db.tOrderDetail
            .Where(p => p.fIsApproved == "是" && p.fUserId == fUserId && p.fOrderGuid == orderGuid)
            .Select(p =>
                new PaymentProduct()
                {
                    ProductId = p.fPId,
                    Quantity = p.fQty
                })
            .ToList();
        // 取得訂單識別碼
        PaymentRequestParameter paymentRequest = new PaymentRequestParameter()
        {
            OrderId = orderGuid,
            Products = productList
        };
        string requestData = JsonConvert.SerializeObject(paymentRequest);

        // 建立 HttpClient
        HttpClient client = new HttpClient();
        // 使用Web API的HttpClient發送請求並獲取回應
        HttpResponseMessage response = await client.PostAsync($"https://localhost:44375/api/LinePay/Create",
        new StringContent(requestData, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        // 解析 API 回傳的資訊，反序列化
        var apiResponse = await response.Content.ReadAsStringAsync();
        var orderItem = JsonConvert.DeserializeObject<PaymentResponseDto>(apiResponse);
        // 成功後導轉到 line 付款頁
        if (orderItem.ReturnCode == "0000")
        {
            string guid = Session["OrderGuid"].ToString();
            var order = db.tOrder
                .FirstOrDefault(m => m.fOrderGuid == guid);
            if (order != null)
            {
                order.fTrancsationId = orderItem.Info.TransactionId;
                db.SaveChanges();
            }

            return Redirect(orderItem.Info.PaymentUrl.Web);
        }
        else
        {
            //用SetStatus()會有副作用，阻止傳回Content
            Response.StatusCode = 400;
            //設定TrySkipIisCustomErrors，停用IIS自訂錯誤頁面
            Response.TrySkipIisCustomErrors = true;
            return Content(
                "{ \"error\": \"發生錯誤，資料無法導轉到line pay\" }", "application/json");
        }
    }

    [HttpGet]
    public ActionResult Cancel()
    {
        return View("Cancel", "_LayoutMember");
    }

    [HttpGet]
    public ActionResult Confirm(string transactionId, string orderId)
    {
        // 取得商品金額
        string fUserId = ((tMember)Session["Member"]).fUserId;
        var productList = db.tOrderDetail
            .Where(p => p.fIsApproved == "是" && p.fUserId == fUserId && p.fOrderGuid == orderId);

        int totalAmount = 0;

        foreach (var product in productList)
        {
            totalAmount += product.fPrice * product.fQty;
        }
        //資料帶到confirm前端
        ViewData["transactionId"] = transactionId;
        ViewData["orderId"] = orderId;
        ViewData["totalAmount"] = totalAmount;

        return View("Confirm", "_LayoutMember", productList);
    }

    public ActionResult Finish()
    {
        return View("finish", "_LayoutMember");
    }
}