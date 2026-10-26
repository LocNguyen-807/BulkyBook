using BulkyBookBusiness.Services.iServices;
using Mailjet.Client;
using Mailjet.Client.TransactionalEmails;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace BulkyBookBusiness.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;
        private readonly string _secretKey;
        private readonly string _senderEmail;
        private readonly string _senderName;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _apiKey = _configuration["Mailjet:ApiKey"];
            _secretKey = _configuration["Mailjet:SecretKey"];
            _senderEmail = _configuration["Mailjet:SenderEmail"];
            _senderName = _configuration["Mailjet:SenderName"];
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            try
            {
                MailjetClient client = new MailjetClient(_apiKey, _secretKey);

                var email = new TransactionalEmailBuilder().WithFrom(new SendContact(_senderEmail, _senderName))
                    .WithTo(new SendContact(toEmail)).WithSubject(subject).WithHtmlPart(htmlContent).Build();

                var response = await client.SendTransactionalEmailAsync(email);

                //if(response.Messages != null && response.Messages.Length > 0)
                //{
                //    var message = response.Messages[0];
                //    if(message.Status != "success")
                //    {
                //        return true;
                //    }
                //    else
                //    {
                //        return false;
                //    }
                //}

                return true;

            }catch(Exception e)
            {
                return false;
            }
        }

        public async Task<bool> SendOrderConfirmationEmailAsync(string toEmail, int orderId, decimal orderTotal)
        {
            var subject = $"Order Confirmation #{orderId} = BulkyBook";

            var htmlContent = $@"<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px; background-color: #ffffff;'>
        <div style='text-align: center; border-bottom: 2px solid #007bff; padding-bottom: 10px; margin-bottom: 20px;'>
            <h2 style='color: #007bff; margin: 0;'>BulkyBook</h2>
            <p style='color: #6c757d; font-size: 14px; margin: 5px 0 0 0;'>Thank you for your purchase!</p>
        </div>
        
        <p style='color: #333333; font-size: 16px;'>Hello,</p>
        <p style='color: #333333; font-size: 16px;'>Your order has been successfully placed and is currently being processed.</p>
        
        <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #007bff;'>
            <p style='margin: 8px 0; color: #333333;'><strong>Order ID:</strong> #{orderId}</p>
            <p style='margin: 8px 0; color: #333333;'><strong>Order Date:</strong> {DateTime.Now:MMMM dd, yyyy}</p>
            <p style='margin: 8px 0; color: #333333;'><strong>Order Total:</strong> <span style='color: #28a745; font-weight: bold;'>${orderTotal:N2}</span></p>
        </div>
        
        <p style='color: #333333; font-size: 16px;'>We will send you another update once your order is packed and ready for shipping.</p>
        
        <div style='margin-top: 30px; padding-top: 15px; border-top: 1px solid #e0e0e0; text-align: center; color: #888888; font-size: 13px;'>
            <p style='margin: 0;'>Best regards,<br/><strong>The BulkyBook Team</strong></p>
        </div>
    </div>";

            return await SendEmailAsync(toEmail, subject, htmlContent);
        }
    }
}
