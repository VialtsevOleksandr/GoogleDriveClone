using GoogleDriveClone.SharedModels.Results;

namespace GoogleDriveClone.SharedModels.DTOs;

public record ApiResponse<T>(bool Success, string Message, T Data);

public record ApiErrorResponse(bool Success, Error Error);

// ��� ������� �������� ��� �����
public record ApiResponse(bool Success, string Message);