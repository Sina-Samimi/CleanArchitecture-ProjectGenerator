using LogsDtoCloneTest.Application.DTOs.Sms;
using LogsDtoCloneTest.SharedKernel.DTOs;

namespace LogsDtoCloneTest.Application.Interfaces;

public interface ISMSSenderService
{
    Task<ResponseDto> PhoneNumberConfirmSms(VerifyPhonenumberSmsDto verifyPhonenumber);

    Task<ResponseDto> TicketReplySms(TicketReplySmsDto dto);

    Task<ResponseDto> WithdrawalResponseSms(WithdrawalResponseSmsDto dto);

    Task<ResponseDto> ProductFollowUpStatusSms(ProductFollowUpStatusSmsDto dto);

    Task<ResponseDto> SellerProductRequestReplySms(SellerProductRequestReplySmsDto dto);

    Task<ResponseDto> BackInStockSms(BackInStockSmsDto dto);

}
