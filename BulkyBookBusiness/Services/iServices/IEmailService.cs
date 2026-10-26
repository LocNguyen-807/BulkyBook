using System;
using System.Collections.Generic;
using System.Text;

namespace BulkyBookBusiness.Services.iServices
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent);

        Task<bool> SendOrderConfirmationEmailAsync(string toEmail, int orderId, decimal orderTotal);
    }
}
