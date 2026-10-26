using BulkyBookBusiness.Services.iServices;
using BulkyBookDataAccess.Data;
using BulkyBookModels;
using BulkyBookModels.ViewModels;
using BulkyBookUtility;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using Stripe.Climate;
using System;
using System.Collections.Generic;
using System.Text;


namespace BulkyBookBusiness.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDBContext _context;

        public OrderService(ApplicationDBContext context)
        {
            this._context = context;
        }

        public async Task<bool> CancelOrderWithRefundAsync(int orderId)
        {
            var order = await _context.OrderHeaders.FindAsync(orderId);

            if(order == null)
            {
                throw new KeyNotFoundException($"Order Id: {orderId} not found!");
            }

            if(order.OrderStatus == SD.StatusShipped)
            {
                throw new InvalidOperationException($"Cannot cancelorders that have been shipped!");
            }

            if(order.OrderStatus == SD.StatusCancelled || order.OrderStatus == SD.StatusRefunded)
            {
                throw new InvalidOperationException($"Order has already been cancelled!");
            }

            bool refundIssues = false;

            if(!string.IsNullOrEmpty(order.PaymentIntentId) &&
                (order.OrderStatus == SD.StatusApproved ||
                order.OrderStatus == SD.StatusInProcess))
            {
                try {
                    var options = new RefundCreateOptions { PaymentIntent = order.PaymentIntentId, Reason = RefundReasons.RequestedByCustomer };
                    var service = new RefundService();
                    Refund refund = service.Create(options);

                    if(refund.Status == "succeeded" || refund.Status == "pending")
                    {
                        refundIssues = true;
                        order.OrderStatus = SD.StatusRefunded;
                    }
                }
                catch(StripeException ex)
                {
                    order.OrderStatus = SD.StatusCancelled;
                    await _context.SaveChangesAsync();
                    throw new InvalidOperationException($"Stripe refund failed: {ex.Message}. Order has been cancelled, but refund must be processed!");
                }
            } else
            {
                order.OrderStatus = SD.StatusCancelled;
            }

            await _context.SaveChangesAsync();
            return refundIssues;

        }

        public async Task<string> CreateStripeCheckoutSessionAsync(OrderHeader orderHeader, IEnumerable<ShoppingCart> cartItems, string domain)
        {
            if(orderHeader == null)
            {
                throw new ArgumentNullException(nameof(orderHeader));
            }
            if(cartItems == null)
            {
                throw new ArgumentNullException(nameof(cartItems));
            }


            var options = new SessionCreateOptions
            {
                SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={orderHeader.Id}",
                CancelUrl = domain + "customer/cart/index",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                Metadata = new Dictionary<string, string>
                    {
                        {"OrderId", orderHeader.Id.ToString() }
                    }
            };

            foreach (var item in cartItems)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Price),
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Title
                        }
                    },
                    Quantity = item.Count,
                };

                options.LineItems.Add(sessionLineItem);
            }

            var service = new SessionService();
            Session session = service.Create(options);

            await UpdateStripePaymentAsync(orderHeader.Id, session.Id, session.PaymentIntentId);
            return session.Url;
        }

        public async Task<bool> VerifyStripePaymentAsync(OrderHeader orderHeader)
        {
            var service = new SessionService();
            Session session = service.Get(orderHeader.SessionId);

            if (session.PaymentStatus.ToLower() == "paid")
            {
                await UpdateStripePaymentAsync(orderHeader.Id, session.Id, session.PaymentIntentId);
                await UpdateOrderStatusAsync(orderHeader.Id, SD.StatusApproved);
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<OrderHeader> CreateOrderAsync(OrderHeader orderHeader)
        {
            _context.OrderHeaders.Add(orderHeader);
            await _context.SaveChangesAsync();
            return orderHeader;
        }

        

        public async Task<IEnumerable<OrderHeader>> GetAllOrdersAsync(string? userId = null, string? status = null, bool includeUser = false, bool includeDetails = false)
        {
            var query = _context.OrderHeaders.AsQueryable();

            if (includeUser)
            {
                query = query.Include(q => q.ApplicationUser);
            }
            if (includeDetails)
            {
                query = query.Include(q => q.OrderDetails).ThenInclude(d => d.Product);
            }
            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(q => q.ApplicationUserId == userId);
            }
            if (!string.IsNullOrEmpty(status) && status.ToLower() != "all")
            {
                if(status.ToLower() == "cancelled")
                {
                    query = query.Where(q => q.OrderStatus.ToLower() == SD.StatusCancelled || q.OrderStatus.ToLower() == SD.StatusRefunded);

                }
                else
                {
                    query = query.Where(q => q.OrderStatus.ToLower() == status.ToLower());
                }
            }

            return await query.ToListAsync();
        }

        public async Task<OrderHeader?> GetOrderByIdAsync(int id, bool includeUser = false, bool includeDetails = false)
        {
            var query = _context.OrderHeaders.AsQueryable();

            if (includeUser)
            {
                query = query.Include(q => q.ApplicationUser);
            }
            if (includeDetails)
            {
                query = query.Include(q => q.OrderDetails).ThenInclude(d => d.Product);
            }

            return await query.FirstOrDefaultAsync(q => q.Id == id);
        }

        public async Task UpdateOrderAsync(OrderHeader orderHeader)
        {
            _context.OrderHeaders.Update(orderHeader);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateOrderStatusAsync(int orderId, string status, string? carrier = null, string? trackingNumber = null)
        {
            var order = await _context.OrderHeaders.FindAsync(orderId);
            if(order == null)
            {
                throw new KeyNotFoundException($"Order Id: {orderId} not found!");
            }

            order.OrderStatus = status;

            if(status == SD.StatusShipped)
            {
                order.ShippingDate = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(carrier))
                {
                    order.Carrier = carrier;
                }
                if (!string.IsNullOrEmpty(trackingNumber))
                {
                    order.TrackingNumber = trackingNumber;
                }
            }
            

            _context.OrderHeaders.Update(order);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateStripePaymentAsync(int orderId, string sessionId, string paymentIntentId)
        {
            var order = await _context.OrderHeaders.FindAsync(orderId);
            if (order == null)
            {
                throw new KeyNotFoundException($"Order Id: {orderId} not found!");
            }

            if (!string.IsNullOrEmpty(sessionId))
            {
                order.SessionId = sessionId;
            }
            if (!string.IsNullOrEmpty(paymentIntentId))
            {
                order.PaymentIntentId = paymentIntentId;
            }

            await _context.SaveChangesAsync();
        }

        
    }
}
