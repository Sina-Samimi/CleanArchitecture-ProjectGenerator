using System;
using System.Threading;
using System.Threading.Tasks;

namespace LogsDtoCloneTest.WebSite.Services;

public interface ISmsSender
{
    Task SendVerificationCodeAsync(string phoneNumber, string code, TimeSpan lifetime, CancellationToken cancellationToken = default);
}
