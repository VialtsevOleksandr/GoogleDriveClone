using GoogleDriveClone.Api.Interfaces;
using GoogleDriveClone.SharedModels.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;

namespace GoogleDriveClone.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class FilesController : ApiControllerBase
{
    private readonly IFileService _fileService;

    public FilesController(IFileService fileService)
    {
        _fileService = fileService;
    }

    /// <summary>
    /// ������������ �����
    /// </summary>
    /// <param name="uploadDto">���� ��� ������������</param>
    /// <returns>���������� ��� ������������ ����</returns>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile([FromForm] UploadFileDto uploadDto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _fileService.UploadFileAsync(uploadDto.File, userId);
        return HandleResult(result, "���� ������ �����������");
    }

    /// <summary>
    /// ��������� ������ ����� �����������
    /// </summary>
    /// <returns>������ ����� �����������</returns>
    [HttpGet]
    public async Task<IActionResult> GetFiles()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _fileService.GetUserFilesAsync(userId);
        return HandleResult(result, "������ ����� �������� ������");
    }

    /// <summary>
    /// ��������� ���������� ��� ���������� ����
    /// </summary>
    /// <param name="id">ID �����</param>
    /// <returns>���������� ��� ����</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetFile(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _fileService.GetFileByIdAsync(id, userId);
        return HandleResult(result, "���������� ��� ���� �������� ������");
    }

    /// <summary>
    /// ������������ ����� �� ��������� ����'����
    /// </summary>
    /// <param name="id">ID �����</param>
    /// <returns>���� ��� ������������</returns>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadFile(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _fileService.DownloadFileAsync(id, userId);

        // ���������� ������� ������, �� ������� ��������� - �� ����, � �� JSON
        if (!result.IsSuccess)
        {
            return HandleResult(result, string.Empty); // ������������� ��� ������� �������� �������
        }

        var downloadResult = result.Value!;
        // ��������� ����. ASP.NET Core ��� ����� ��� �������� ���������.
        return File(downloadResult.Content, downloadResult.ContentType, downloadResult.OriginalName);
    }

    /// <summary>
    /// ��������� �����
    /// </summary>
    /// <param name="id">ID �����</param>
    /// <returns>ϳ����������� ���������</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFile(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _fileService.DeleteFileAsync(id, userId);
        return HandleResult(result, "���� ������ ��������");
    }

    /// <summary>
    /// ������� ��������� �����
    /// </summary>
    /// <param name="request">������ ID ����� ��� ���������</param>
    /// <returns>ϳ����������� ���������</returns>
    [HttpPost("delete-batch")]
    public async Task<IActionResult> DeleteFiles([FromBody] BatchDeleteRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _fileService.DeleteFilesAsync(request.FileIds, userId);
        return HandleResult(result, $"����� ������ �������� ({request.FileIds.Count} ��.)");
    }

    /// <summary>
    /// ��������� ����� �����
    /// </summary>
    /// <param name="id">ID �����</param>
    /// <param name="request">����� ���� �����</param>
    /// <returns>�������� ���������� ��� ����</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateFileContent(string id, [FromBody] UpdateFileContentRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        
        // ���������� string � MemoryStream
        var contentBytes = Encoding.UTF8.GetBytes(request.Content);

        // ����������� SHA-256 ��� � ���������
        string newFileHash;
        using (var sha256 = SHA256.Create())
        {
            var hashBytes = sha256.ComputeHash(contentBytes);
            newFileHash = Convert.ToHexString(hashBytes).ToLower();
        }

        using var contentStream = new MemoryStream(contentBytes);

        // �������� � ����� � ����, � ��� ������� ���
        var result = await _fileService.UpdateFileContentAsync(id, userId, contentStream, newFileHash);

        return HandleResult(result, "���� ����� ������ ��������");
    }

    /// <summary>
    /// ��������� ���������� �����������
    /// </summary>
    /// <returns>���������� �����������</returns>
    [HttpGet("stats")]
    public async Task<IActionResult> GetUserStats()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _fileService.GetUserStatsAsync(userId);
        return HandleResult(result, "���������� ����������� �������� ������");
    }
}