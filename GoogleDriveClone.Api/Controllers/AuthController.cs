using GoogleDriveClone.Api.Interfaces;
using GoogleDriveClone.SharedModels.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace GoogleDriveClone.Api.Controllers;

[Route("api/[controller]")]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// ��������� ������ �����������
    /// </summary>
    /// <param name="registerDto">��� ��� ���������</param>
    /// <returns>JWT ����� �� ���������� ��� �����������</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        var result = await _authService.RegisterAsync(registerDto);
        return HandleResult(result, "��������� ������");
    }

    /// <summary>
    /// ���� ����������� � �������
    /// </summary>
    /// <param name="loginDto">��� ��� �����</param>
    /// <returns>JWT ����� �� ���������� ��� �����������</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        var result = await _authService.LoginAsync(loginDto);
        return HandleResult(result, "���� �������");
    }

    /// <summary>
    /// �������� ������� ��� �������� ��������������
    /// </summary>
    /// <returns>���������� ��� ��������� �����������</returns>
    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

        var userData = new
        {
            userId,
            userName,
            userEmail
        };

        var response = new ApiResponse<object>(true, "��� ����������� �������� ������", userData);
        return Ok(response);
    }
}