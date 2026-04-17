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

    [HttpGet("{id}")]
    public async Task<ActionResult<StoreResponseDto>> GetStoreById(Guid id)
    {
        var store = await _storeService.GetStoreByIdAsync(id);
        return Ok(ApiResponse<StoreResponseDto>.Success(store, "Lấy dữ liệu thành công"));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SystemAdmin,BrandAdmin")]
    public async Task<IActionResult> UpdateStore(Guid id, [FromBody] UpdateStoreDto dto)
    {
        var response = await _storeService.UpdateStoreAsync(id, dto);
        return Ok(ApiResponse<StoreResponseDto>.Success(response, "Cập nhật cửa hàng thành công"));
    }

    [HttpGet("{id}/accounts")]
    [Authorize(Roles = "SystemAdmin,BrandAdmin,Manager")]
    public async Task<IActionResult> GetAccountsByStore(Guid id)
    {
        var accounts = await _storeService.GetAccountsByStoreIdAsync(id);
        return Ok(ApiResponse<IEnumerable<StoreAccountDto>>.Success(accounts, "Lấy danh sách tài khoản thành công"));
    }

    [HttpGet("{id}/machines")]
    [Authorize(Roles = "SystemAdmin,BrandAdmin,Manager,Staff")]
    public async Task<IActionResult> GetMachinesByStore(Guid id)
    {
        var machines = await _storeService.GetMachinesByStoreIdAsync(id);
        return Ok(ApiResponse<IEnumerable<Machine>>.Success(machines, "Lấy danh sách máy thành công"));
    }

    [HttpPatch("{id}/type")]
    [Authorize(Roles = "SystemAdmin,BrandAdmin")]
    public async Task<IActionResult> AssignStoreType(Guid id, [FromBody] UpdateStoreTypeDto dto)
    {
        var result = await _storeService.AssignStoreTypeAsync(id, dto.StoreTypeId);
        if (!result) return NotFound(ApiResponse<object>.Error(404, "Không tìm thấy cửa hàng"));
        return Ok(ApiResponse<object>.Success(new { }, "Gán hạng cửa hàng thành công"));
    }
}
