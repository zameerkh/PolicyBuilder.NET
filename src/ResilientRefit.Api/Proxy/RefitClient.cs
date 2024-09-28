using Refit;

namespace ResilientRefit.Core.Proxy;

public interface IHttpBinClient
{
    [Get("/get")]
    Task<string> GetAsync();

    [Post("/post")]
    Task<string> PostAsync([Body] object data);

    [Put("/put")]
    Task<string> PutAsync([Body] object data);

    [Delete("/delete")]
    Task<string> DeleteAsync();
}