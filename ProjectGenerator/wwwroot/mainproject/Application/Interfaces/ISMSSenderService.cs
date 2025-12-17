using Attar.Application.DTOs.Sms;
using Attar.SharedKernel.DTOs;

namespace Attar.Application.Interfaces;

public interface ISMSSenderService
{
    Task<ResponseDto> PhoneNumberConfirmSms(VerifyPhonenumberSmsDto verifyPhonenumber);

    Task<ResponseDto> TicketReplySms(TicketReplySmsDto dto);

    Task<ResponseDto> WithdrawalResponseSms(WithdrawalResponseSmsDto dto);

    Task<ResponseDto> ProductFollowUpStatusSms(ProductFollowUpStatusSmsDto dto);

    Task<ResponseDto> SellerProductRequestReplySms(SellerProductRequestReplySmsDto dto);

    Task<ResponseDto> BackInStockSms(BackInStockSmsDto dto);

}
