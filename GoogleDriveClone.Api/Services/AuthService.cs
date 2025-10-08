using GoogleDriveClone.Api.Entities;
using GoogleDriveClone.Api.Interfaces;
using GoogleDriveClone.SharedModels.DTOs;
using GoogleDriveClone.SharedModels.Results;
using Microsoft.AspNetCore.Identity;

namespace GoogleDriveClone.Api.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ITokenService _tokenService;

    public AuthService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto registerDto)
    {
        // ���������� �� ���������� � ����� email ��� ����
        var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
        if (existingUser != null)
        {
            return DomainErrors.User.EmailAlreadyExists;
        }

        // ���������� �� ���������� � ����� username ��� ����
        var existingUserByName = await _userManager.FindByNameAsync(registerDto.Username);
        if (existingUserByName != null)
        {
            return DomainErrors.User.UsernameAlreadyExists;
        }

        // ��������� ������ �����������
        var user = new User
        {
            Email = registerDto.Email,
            UserName = registerDto.Username
        };

        var result = await _userManager.CreateAsync(user, registerDto.Password);
        if (!result.Succeeded)
        {
            return DomainErrors.User.RegistrationFailed;
        }

        // �������� JWT �����
        var token = _tokenService.GenerateJwtToken(user);

        return new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email!,
            Username = user.UserName!
        };
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(LoginDto loginDto)
    {
        // ������ ����������� �� email
        var user = await _userManager.FindByEmailAsync(loginDto.Email);
        if (user == null)
        {
            return DomainErrors.User.InvalidCredentials;
        }

        // ���������� ������
        var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
        if (!result.Succeeded)
        {
            return DomainErrors.User.InvalidCredentials;
        }

        // �������� JWT �����
        var token = _tokenService.GenerateJwtToken(user);

        return new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email!,
            Username = user.UserName!
        };
    }
}