using BulkyBookBusiness.Services;
using BulkyBookBusiness.Services.iServices;
using BulkyBookDataAccess;
using BulkyBookDataAccess.Data;
using BulkyBookModels;
using BulkyBookModels.ViewModels;
using BulkyBookUtility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.IdentityModel.Tokens;
using System.Reflection.Metadata;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.RoleAdmin)]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;

        [BindProperty]
        public OrderHeader OrderHeader { get; set; }

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            return View();
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int orderId)
        {
            OrderHeader orderHeader = await _orderService.GetOrderByIdAsync(orderId, true, true);
            return View(orderHeader);
        }

        [HttpPost]
        [Authorize(Roles =SD.RoleAdmin + "," +SD.RoleEmployee)]
        public async Task<IActionResult> UpdateOrderStatus(string status)
        {
            OrderHeader orderFromDb = await _orderService.GetOrderByIdAsync(OrderHeader.Id);

            if(orderFromDb == null)
            {
                TempData["error"] = "Order not found!";
                return RedirectToAction(nameof(Index));
            }

            string success = string.Empty;

            switch (status)
            {
                case SD.StatusInProcess:
                    success = "Order processing started!";
                    await _orderService.UpdateOrderStatusAsync(orderFromDb.Id ,status);
                    break;
                case SD.StatusCancelled:
                case SD.StatusRefunded:
                    try
                    {
                        bool refundIssued = await _orderService.CancelOrderWithRefundAsync(OrderHeader.Id);
                        if (refundIssued)
                        {
                            success = "Order cancelled and refund issued successfully. Funds will be returned to customer within 5-10 business days.";
                        }
                        else
                        {
                            success = "Order cancelled successfully. (No payment was processed)";
                        }
                    }
                    catch (InvalidOperationException ex)
                    {
                        // Business rule violation (e.g., trying to cancel shipped order)
                        TempData["error"] = ex.Message;
                        return RedirectToAction(nameof(Details), new { orderId = OrderHeader.Id });
                    }
                    catch (Stripe.StripeException ex)
                    {
                        // Refund failed - order is still cancelled but admin needs to manually refund
                        TempData["error"] = $"Order cancelled but refund failed: {ex.Message}. Please process refund manually in Stripe Dashboard.";
                        return RedirectToAction(nameof(Details), new { orderId = OrderHeader.Id });
                    }

                    break;
                case SD.StatusShipped:
                    if(string.IsNullOrEmpty(OrderHeader.Carrier) || string.IsNullOrEmpty(OrderHeader.TrackingNumber))
                    {
                        TempData["error"] = "Please provide carrier and tracking number info.";
                        return RedirectToAction("Details", new { orderId = orderFromDb.Id });
                    }

                    success = "Order shipped successfully!";
                    await _orderService.UpdateOrderStatusAsync(orderFromDb.Id , SD.StatusShipped, OrderHeader.Carrier, OrderHeader.TrackingNumber);
                    break;
                default:
                    TempData["error"] = "Invalid status update.";
                    return RedirectToAction("Details", new { orderId = orderFromDb.Id });
                    break;
            }
           
            
            TempData["Success"] = success;
            return RedirectToAction("Details", new { orderId = orderFromDb.Id });
        }

        [HttpPost]
        [Authorize(Roles =SD.RoleAdmin + "," +SD.RoleEmployee)]
        public async Task<IActionResult> UpdateOrderDetails()
        {
            OrderHeader orderFromDb = await _orderService.GetOrderByIdAsync(OrderHeader.Id);

            orderFromDb.Name = OrderHeader.Name;
            orderFromDb.PhoneNumber = OrderHeader.PhoneNumber;
            orderFromDb.StreetAddress = OrderHeader.StreetAddress;
            orderFromDb.City = OrderHeader.City;
            orderFromDb.State = OrderHeader.State;
            orderFromDb.PostalCode = OrderHeader.PostalCode;
            if(!string.IsNullOrEmpty(OrderHeader.Carrier) && orderFromDb.OrderStatus == SD.StatusShipped)
            {
                orderFromDb.Carrier = OrderHeader.Carrier;
            }
            if (!string.IsNullOrEmpty(OrderHeader.TrackingNumber) && OrderHeader.OrderStatus == SD.StatusShipped)
            { 
                orderFromDb.TrackingNumber = OrderHeader.TrackingNumber;
            }

            await _orderService.UpdateOrderAsync(orderFromDb);
            TempData["Success"] = "Order updated successfully!";
            return RedirectToAction("Details", new { orderId = orderFromDb.Id });
        }


        #region API CAllS
        [AllowAnonymous]
        public async Task<IActionResult> GetAll(string status)
        {
            string? userid = null;

            if(!User.IsInRole(SD.RoleAdmin) && !User.IsInRole(SD.RoleEmployee))
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;

                userid = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userid))
                {
                    return Unauthorized();
                }
            }

            var orders = await _orderService.GetAllOrdersAsync(userid, status);
            return Json(new { data = orders });
        }
        #endregion
    }
}
