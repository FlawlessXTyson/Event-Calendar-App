using System;

namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models
{
    public class ErrorLog
    {
        public int Id { get; set; }

        public string Message { get; set; } = string.Empty;

        public string? StackTrace { get; set; }

        public string? Path { get; set; }

        public string? Method { get; set; }

        public int StatusCode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}