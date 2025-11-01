using bds.Data;
using bds.Models;
using Microsoft.AspNetCore.Http;
using System;

namespace bds.Services
{
    public class LogService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LogService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public void AddLog(int? userId, string actionType, string description, string? tableName = null, int? recordId = null, bool isSuccess = true)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            var log = new Log
            {
                UserID = userId,
                ActionType = actionType,
                ActionDescription = description,
                TableName = tableName,
                RecordID = recordId,
                IPAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
                BrowserInfo = httpContext?.Request?.Headers["User-Agent"],
                IsSuccess = isSuccess,
                CreatedAt = DateTime.Now
            };
            _context.Logs.Add(log);
            _context.SaveChanges();
        }
    }
}
