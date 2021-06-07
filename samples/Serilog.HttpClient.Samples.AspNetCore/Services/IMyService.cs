using System.Threading.Tasks;

namespace Serilog.HttpClient.Samples.AspNetCore.Services
{
    public interface IMyService
    {
        Task<object> SendRequest();
    }
}