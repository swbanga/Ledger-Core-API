namespace LedgerCore.Application.Contracts;

public interface IRequestContext
{
    Guid GetUserId();
    string GetIpAddress();
    string GetDeviceId();
}
