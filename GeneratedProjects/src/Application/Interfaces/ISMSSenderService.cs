using TestAttarClone.Application.DTOs.Sms;
using TestAttarClone.SharedKernel.DTOs;

namespace TestAttarClone.Application.Interfaces;

public interface ISMSSenderService
{
    Task<ResponseDto> PhoneNumberConfirmSms(VerifyPhonenumberSmsDto verifyPhonenumber);

    Task<ResponseDto> TicketReplySms(TicketReplySmsDto dto);

    Task<ResponseDto> WithdrawalResponseSms(WithdrawalResponseSmsDto dto);

    Task<ResponseDto> ProductFollowUpStatusSms(ProductFollowUpStatusSmsDto dto);

    Task<ResponseDto> SellerProductRequestReplySms(SellerProductRequestReplySmsDto dto);

    Task<ResponseDto> BackInStockSms(BackInStockSmsDto dto);

}
