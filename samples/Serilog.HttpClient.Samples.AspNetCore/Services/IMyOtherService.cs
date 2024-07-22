using System.Threading.Tasks;

namespace Serilog.HttpClient.Samples.AspNetCore.Services
{
    public interface IMyOtherService
    {
        Task<object> SendRequest();
    }
}
