using BE1.Models;
using BE1.Models.viewmodel;
using PayPal.Api;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web;
using System.Web.Mvc;

namespace BE1.Areas.Customer.Controllers
{
    public class PaypalController : Controller
    {
        private MyStoreEntities1 db = new MyStoreEntities1();

        private Payment CreatePayment(APIContext apiContext, string redirectUrl, CheckoutVM model)
        {
            var itemList = new ItemList() { items = new List<Item>() };

            foreach (var item in model.CartItems)
            {
                itemList.items.Add(new Item()
                {
                    name = item.ProductName,
                    currency = "USD",
                    price = item.UnitPrice.ToString(),
                    quantity = item.Quantity.ToString(),
                    sku = item.ProductID.ToString()
                });
            }

            var payer = new Payer() { payment_method = "paypal" };

            var redirectUrls = new RedirectUrls()
            {
                cancel_url = redirectUrl,
                return_url = redirectUrl
            };

            var details = new Details()
            {
                tax = "0",
                shipping = "0",
                subtotal = model.TotalAmount.ToString()
            };

            var amount = new Amount()
            {
                currency = "USD",
                total = model.TotalAmount.ToString(),
                details = details
            };

            var transactionList = new List<Transaction>();
            transactionList.Add(new Transaction()
            {
                description = "Transaction description",
                invoice_number = Convert.ToString(new Random().Next(100000)),
                amount = amount,
                item_list = itemList
            });

            var payment = new Payment()
            {
                intent = "sale",
                payer = payer,
                transactions = transactionList,
                redirect_urls = redirectUrls
            };

            return payment.Create(apiContext);
        }

        private APIContext GetAPIContext()
        {
            var clientId = ConfigurationManager.AppSettings["clientId"];
            var clientSecret = ConfigurationManager.AppSettings["clientSecret"];
            var mode = ConfigurationManager.AppSettings["mode"] ?? "sandbox";

  
            if (string.IsNullOrWhiteSpace(clientId) || 
                clientId.Contains("YOUR_") ||
                string.IsNullOrWhiteSpace(clientSecret) || 
                clientSecret.Contains("YOUR_"))
            {
                throw new InvalidOperationException(
                    "PayPal credentials are not properly configured. Please set 'clientId' and 'clientSecret' in Web.config appSettings. " +
                    "Get your credentials from https://developer.paypal.com/dashboard/");
            }

            var config = new Dictionary<string, string>
            {
                { "mode", mode },
                { "clientId", clientId },
                { "clientSecret", clientSecret }
            };

            var accessToken = new OAuthTokenCredential(config).GetAccessToken();
            return new APIContext(accessToken);
        }

        public ActionResult PaymentWithPaypal(CheckoutVM model)
        {
            try
            {
                APIContext apiContext = GetAPIContext();

                string payerId = Request.Params["PayerID"];

                if (string.IsNullOrEmpty(payerId))
                {
                    string baseURI = Request.Url.Scheme + "://" + Request.Url.Authority + "/Paypal/PaymentWithPaypal?";
                    var guid = Convert.ToString(new Random().Next(100000));
                    var createdPayment = CreatePayment(apiContext, baseURI + "guid=" + guid, model);

                    var links = createdPayment.links.GetEnumerator();
                    string paypalRedirectUrl = null;

                    while (links.MoveNext())
                    {
                        var lnk = links.Current;
                        if (lnk.rel.ToLower().Trim().Equals("approval_url"))
                        {
                            paypalRedirectUrl = lnk.href;
                        }
                    }

                    Session.Add(guid, createdPayment.id);
                    return Redirect(paypalRedirectUrl);
                }
                else
                {
                    var guid = Request.Params["guid"];
                    var executedPayment = ExecutePayment(apiContext, payerId, Session[guid] as string);

                    if (executedPayment.state.ToLower() != "approved")
                    {
                        return View("~/Areas/Customer/Views/Paypal/FailureView.cshtml");
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                // Configuration error - return view with error message
                ViewBag.ErrorMessage = ex.Message;
                return View("~/Areas/Customer/Views/Paypal/FailureView.cshtml");
            }
            catch (Exception ex)
            {
                // Log the exception details for debugging
                System.Diagnostics.Debug.WriteLine($"PayPal payment error: {ex.Message}");
                return View("~/Areas/Customer/Views/Paypal/FailureView.cshtml");
            }

            return RedirectToAction("CreateOrder", "Order", model);
        }

        private Payment ExecutePayment(APIContext apiContext, string payerId, string paymentId)
        {
            var paymentExecution = new PaymentExecution() { payer_id = payerId };
            var payment = new Payment() { id = paymentId };
            return payment.Execute(apiContext, paymentExecution);
        }
    }
}
