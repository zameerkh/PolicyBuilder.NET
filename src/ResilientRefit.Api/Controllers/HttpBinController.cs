using Microsoft.AspNetCore.Mvc;

using ResilientRefit.Core.Proxy;

namespace ResilientRefit.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HttpBinController : ControllerBase
{
    private readonly IHttpBinClient _httpBinClient;

    public HttpBinController(IHttpBinClient httpBinClient)
    {
        _httpBinClient = httpBinClient;
    }

    [HttpGet("get")]
    public async Task<IActionResult> Get()
    {
        var content = await _httpBinClient.GetAsync();
        return Ok(content);
    }

    [HttpPost("post")]
    public async Task<IActionResult> Post([FromBody] object data)
    {
        var content = await _httpBinClient.PostAsync(data);
        return Ok(content);
    }

    [HttpPut("put")]
    public async Task<IActionResult> Put([FromBody] object data)
    {
        var content = await _httpBinClient.PutAsync(data);
        return Ok(content);
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> Delete()
    {
        var content = await _httpBinClient.DeleteAsync();
        return Ok(content);
    }
}