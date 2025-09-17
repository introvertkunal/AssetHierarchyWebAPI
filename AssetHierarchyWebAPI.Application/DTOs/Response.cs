
namespace AssetHierarchyWebAPI.Application.DTOs
{
    public class ServiceResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ServiceResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public ServiceResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }
}