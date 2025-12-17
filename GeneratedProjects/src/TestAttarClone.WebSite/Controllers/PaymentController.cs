using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestAttarClone.Application.Commands.Billing;
using TestAttarClone.Application.Queries.Billing;
using TestAttarClone.Application.Queries.Identity.GetUserById;
using TestAttarClone.Application.DTOs.Billing;
using TestAttarClone.Application.DTOs;
using TestAttarClone.Domain.Enums;
using TestAttarClone.SharedKernel.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Parbad;
using Parbad.Gateway.ZarinPal;
using Parbad.AspNetCore;

namespace TestAttarClone.WebSite.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IOnlinePayment _onlinePayment;
        private static readonly Random _random = new Random();
        private static readonly object _lockObject = new object();

        public PaymentController(IMediator mediator, IOnlinePayment onlinePayment)
        {
            _mediator = mediator;
            _onlinePayment = onlinePayment;
        }

        /// <summary>
        /// Generates a unique tracking number for payment gateway
        /// Uses timestamp + random number to ensure uniqueness
        /// </summary>
        private static long GenerateUniqueTrackingNumber()
        {
            lock (_lockObject)
            {
                // Get current timestamp in milliseconds (last 10 digits)
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var timestampPart = timestamp % 10000000000; // Last 10 digits
                
                // Generate random 6-digit number
                var randomPart = _random.Next(100000, 999999);
                
                // Combine: timestamp (10 digits) + random (6 digits) = 16 digits
                // This ensures uniqueness even if called multiple times in the same millisecond
                return timestampPart * 1000000 + randomPart;
            }
        }

        [HttpGet("/Payment/{orderId}")]
        public async Task<IActionResult> Payment(Guid orderId)
        {
            // Load transaction information (query returns Result<FrontTransactionInfoDto>)
            var transResult = await _mediator.Send(new FrontGetTransactioninfoQuery(orderId));
            if (!transResult.IsSuccess || transResult.Value is null)
            {
                return NotFound();
            }

            var transaction = transResult.Value;

            // Optionally load user data if needed (GetUserByIdQuery returns Result<UserDto>)
            var userResult = await _mediator.Send(new GetUserByIdQuery(transaction.UserId));
            var phone = transaction.Phonenumber;
            var userPhone = (userResult.IsSuccess && userResult.Value is not null)
                ? userResult.Value.PhoneNumber
                : phone;

            // Convert amount to Rials (gateway expects Rials). Assuming Amount is in Toman.
            int amount = (int)(transaction.Amount * 10m);

            var callbackUrl = Url.Action("VerifyPayment", "Payment", null, Request.Scheme) ?? "";

            // Check if there's already a pending transaction for this invoice
            // If exists, use its tracking number; otherwise create a new one
            long trackingNumber;
            var invoiceResult = await _mediator.Send(new GetUserInvoiceDetailsQuery(orderId, transaction.UserId));
            if (invoiceResult.IsSuccess && invoiceResult.Value is not null)
            {
                // Find existing pending transaction
                var pendingTransaction = invoiceResult.Value.Transactions
                    .FirstOrDefault(t => t.Method == PaymentMethod.OnlineGateway && 
                                       t.Status == TransactionStatus.Pending &&
                                       long.TryParse(t.Reference, out _));
                
                if (pendingTransaction is not null && long.TryParse(pendingTransaction.Reference, out var existingTrackingNumber))
                {
                    // Use existing pending transaction's tracking number
                    trackingNumber = existingTrackingNumber;
                }
                else
                {
                    // Generate new unique tracking number
                    trackingNumber = GenerateUniqueTrackingNumber();
                    
                    // Create a pending payment transaction to store the tracking number before sending to gateway
                    var createTransactionCmd = new RecordInvoiceTransactionCommand(
                        orderId,
                        transaction.Amount,
                        PaymentMethod.OnlineGateway,
                        TransactionStatus.Pending,
                        trackingNumber.ToString(), // Store tracking number in Reference
                        "ZarinPal",
                        $"پرداخت آنلاین فاکتور",
                        null, // Metadata
                        DateTimeOffset.Now);

                    var createTransactionResult = await _mediator.Send(createTransactionCmd);
                    if (!createTransactionResult.IsSuccess)
                    {
                        var errorResponse = new ResponseDto<string>
                        {
                            Code = 500,
                            Success = false,
                            Messages = new List<Messages> { new Messages { message = createTransactionResult.Error ?? "خطا در ایجاد تراکنش پرداخت." } }
                        };
                        return View("PayResult", errorResponse);
                    }
                }
            }
            else
            {
                // Generate unique tracking number if invoice not found (fallback)
                trackingNumber = GenerateUniqueTrackingNumber();
                
                // Create a pending payment transaction to store the tracking number before sending to gateway
                var createTransactionCmd = new RecordInvoiceTransactionCommand(
                    orderId,
                    transaction.Amount,
                    PaymentMethod.OnlineGateway,
                    TransactionStatus.Pending,
                    trackingNumber.ToString(), // Store tracking number in Reference
                    "ZarinPal",
                    $"پرداخت آنلاین فاکتور",
                    null, // Metadata
                    DateTimeOffset.Now);

                var createTransactionResult = await _mediator.Send(createTransactionCmd);
                if (!createTransactionResult.IsSuccess)
                {
                    var errorResponse = new ResponseDto<string>
                    {
                        Code = 500,
                        Success = false,
                        Messages = new List<Messages> { new Messages { message = createTransactionResult.Error ?? "خطا در ایجاد تراکنش پرداخت." } }
                    };
                    return View("PayResult", errorResponse);
                }
            }

            var paymentResult = await _onlinePayment.RequestAsync(invoice =>
            {
                invoice
                        .SetZarinPalData($"پرداخت فاکتور به شماره {orderId} توسط {userPhone}")
                    .SetTrackingNumber(trackingNumber)
                    .SetAmount(amount)
                    .SetCallbackUrl(callbackUrl)
                    .UseZarinPal();
            });

            if (paymentResult.IsSucceed)
            {
                return paymentResult.GatewayTransporter.TransportToGateway();
            }

            var failResponse = new ResponseDto<string>
            {
                Code = 500,
                Success = false,
                Messages = new List<Messages> { new Messages { message = paymentResult.Message } }
            };

            return View("PayResult", failResponse);
        }

        [HttpGet("/Payment/Cart/{cartId}")]
        public async Task<IActionResult> PaymentCart(Guid cartId)
        {
            // Load cart transaction information (query returns Result<FrontTransactionInfoDto>)
            var transResult = await _mediator.Send(new FrontGetCartTransactionQuery(cartId));
            if (!transResult.IsSuccess || transResult.Value is null)
            {
                return NotFound();
            }

            var transaction = transResult.Value;

            // Optionally load user data if needed (GetUserByIdQuery returns Result<UserDto>)
            var userResult = await _mediator.Send(new GetUserByIdQuery(transaction.UserId));
            var userPhone = (userResult.IsSuccess && userResult.Value is not null)
                ? userResult.Value.PhoneNumber
                : string.Empty;

            // Convert amount to Rials (gateway expects Rials). Assuming Amount is in Toman.
            int amount = (int)(transaction.Amount * 10m);

            var callbackUrl = Url.Action("VerifyPayment", "Payment", null, Request.Scheme) ?? "";

            // Generate unique tracking number to avoid duplicates in payment gateway
            var trackingNumber = GenerateUniqueTrackingNumber();

            // For cart payment, we need to get the invoice ID first
            // The cart should be converted to invoice in checkout, so we need to find the invoice
            // For now, we'll handle this in verify by searching all recent invoices
            // A better approach would be to store cartId -> invoiceId mapping, but for now we'll use tracking number lookup

            var paymentResult = await _onlinePayment.RequestAsync(invoice =>
            {
                invoice
                    .SetZarinPalData($"پرداخت سبد خرید {cartId} توسط {userPhone}")
                    .SetTrackingNumber(trackingNumber)
                    .SetAmount(amount)
                    .SetCallbackUrl(callbackUrl)
                    .UseZarinPal();
            });

            if (paymentResult.IsSucceed)
            {
                return paymentResult.GatewayTransporter.TransportToGateway();
            }

            var errorResponse = new ResponseDto<string>
            {
                Code = 500,
                Success = false,
                Messages = new List<Messages> { new Messages { message = paymentResult.Message } }
            };

            return View("PayResult", errorResponse);
        }

        [HttpGet("VerifyPayment")]
        [HttpPost("VerifyPayment")]
        public async Task<IActionResult> VerifyPayment()
        {
            ResponseDto<string> response = new ResponseDto<string>();

            var invoice = await _onlinePayment.FetchAsync();
            if (invoice == null)
            {
                return BadRequest();
            }

            if (invoice.Status != PaymentFetchResultStatus.ReadyForVerifying)
            {
                response.Success = false;
                response.Code = 500;
                response.Messages = new List<Messages> { new Messages { message = invoice.Message } };

                return RedirectToAction("PayResult", response);
            }

            var verifyResult = await _onlinePayment.VerifyAsync(invoice);

            if (verifyResult.Status == PaymentVerifyResultStatus.Succeed)
            {
                // تراکنش موفق
                // Convert amount from Rials (gateway) to Toman (system) - divide by 10
                var amountInToman = verifyResult.Amount / 10m;
                FrontVerifyTransactionCommand frontVerifyTransaction = new FrontVerifyTransactionCommand(
                    verifyResult.TrackingNumber, 
                    verifyResult.TransactionCode,
                    amountInToman);
                var res = await _mediator.Send(frontVerifyTransaction);
                if (!res.IsSuccess)
                {
                    response.Success = false;
                    response.Code = 500;
                    response.Messages = new List<Messages> { new Messages { message = "پرداخت انجام شد ولی مشکلی در سایت بوجود آمد،لطفا با پشتیبانی تماس بگیرید" } };
                    return RedirectToAction("PayResult", new { response = response });
                }

                response.Success = true;
                response.Code = 200;
                response.Data = verifyResult.TransactionCode; // کد پیگیری

                return RedirectToAction("PayResult", response);
            }

            response.Success = false;
            response.Code = 500;
            response.Messages = new List<Messages> { new Messages { message = verifyResult.Message } };
            return RedirectToAction("PayResult", response);
        }

        [Route("/payresult")]
        public IActionResult PayResult(ResponseDto<string> response)
        {
            return View(response);
        }
    }
}
