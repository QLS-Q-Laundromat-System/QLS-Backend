using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLS.Backend.DTOs.Store;
using QLS.Backend.DTOs;
using QLS.Backend.Interfaces.Stores;
using System.Security.Claims;
using QLS.Backend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace QLS.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StoreController : ControllerBase
{
    private readonly IStoreService _storeService;

    public StoreController(IStoreService storeService)
    {
        _storeService = storeService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Store>>> GetStores()
    {
        var stores = await _storeService.GetStoresAsync();
        return Ok(stores);
    }

    [HttpGet("count")]
    public async Task<ActionResult<int>> GetStoreCount()
    {
        var count = await _storeService.GetStoreCountAsync();
        return Ok(new { success = true, count = count });
    }

    [HttpPost]
    [Authorize(Roles = "SystemAdmin,BrandAdmin")]
    public async Task<IActionResult> CreateStore([FromBody] CreateStoreDto dto)
    {
        var response = await _storeService.CreateStoreAsync(dto);
        return Ok(ApiResponse<StoreResponseDto>.Success(response, "Tạo cửa hàng thành công"));
    }
}
