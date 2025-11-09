using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using bds.Data; // đổi thành namespace chứa ApplicationDbContext

namespace bds.Services
{
    public class ExpirationChecker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ExpirationChecker> _logger;
        private readonly TimeSpan _delayBetweenRuns;
        private readonly object _runLock = new object();
        private bool _isRunning = false;

        public ExpirationChecker(IServiceScopeFactory scopeFactory, ILogger<ExpirationChecker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            // mặc định chạy 24 giờ; nếu muốn cấu hình từ appsettings, inject IOptions và đọc ở đây
            _delayBetweenRuns = TimeSpan.FromHours(24);
        }

        // Nếu bạn muốn cấu hình khoảng delay từ appsettings, thay constructor để nhận IOptions<ExpirationOptions>
        // và gán _delayBetweenRuns từ options.IntervalMinutes...

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ExpirationChecker started at: {time}", DateTimeOffset.Now);

            // Chạy ngay lần đầu, rồi đợi
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // tránh chạy chồng chéo nếu công việc trước chưa hoàn tất
                    if (Monitor.TryEnter(_runLock))
                    {
                        try
                        {
                            if (_isRunning)
                            {
                                _logger.LogWarning("Previous run still in progress — skipping this cycle.");
                            }
                            else
                            {
                                _isRunning = true;
                                await DoWorkAsync(stoppingToken);
                            }
                        }
                        finally
                        {
                            _isRunning = false;
                            Monitor.Exit(_runLock);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Could not acquire run lock — skipping this cycle.");
                    }
                }
                catch (OperationCanceledException)
                {
                    // normal shutdown
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception in ExpirationChecker loop");
                }

                // đợi theo cấu hình, nhưng cho CancellationToken dừng sớm nếu app tắt
                try
                {
                    await Task.Delay(_delayBetweenRuns, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // stoppingToken canceled, exit loop
                    break;
                }
            }

            _logger.LogInformation("ExpirationChecker stopping at: {time}", DateTimeOffset.Now);
        }

        private async Task DoWorkAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ExpirationChecker running DoWork at: {time}", DateTimeOffset.Now);

            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Dùng UTC hoặc local tùy bạn; ở đây dùng DateTime.UtcNow nếu bạn lưu CreateAt ở UTC
                var now = DateTime.Now; // hoặc DateTime.UtcNow

                // Lọc các item chưa là "Hết hạn" và đã quá 7 ngày
                var expiredPosts = db.Posts
                    .Where(p => p.Status != "Hết hạn" && now > p.CreateAt.AddDays(7))
                    .ToList();

                var expiredProjects = db.Projects
                    .Where(pr => pr.Status != "Hết hạn" && now > pr.CreateAt.AddDays(7))
                    .ToList();

                int total = expiredPosts.Count + expiredProjects.Count;

                if (total > 0)
                {
                    foreach (var p in expiredPosts) p.Status = "Hết hạn";
                    foreach (var pr in expiredProjects) pr.Status = "Hết hạn";

                    try
                    {
                        await db.SaveChangesAsync(cancellationToken);
                        _logger.LogInformation("ExpirationChecker updated {count} items to 'Hết hạn' at {time}", total, DateTimeOffset.Now);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving expired updates");
                    }
                }
                else
                {
                    _logger.LogInformation("No expired items found at: {time}", DateTimeOffset.Now);
                }
            }
        }
    }
}
