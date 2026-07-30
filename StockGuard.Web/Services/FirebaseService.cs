using Firebase.Database;
using Firebase.Database.Query;
using StockGuard.Web.Constants;
using StockGuard.Web.Models;

namespace StockGuard.Web.Services
{
    public class FirebaseService
    {
        private readonly FirebaseClient _client;

        public FirebaseService()
        {
            _client = new FirebaseClient(
                FirebaseConfig.DatabaseUrl,
                new FirebaseOptions
                {
                    HttpClientFactory =
                        new FirebaseHttpClientFactory(),
                    AuthTokenAsyncFactory =
                        () => Task.FromResult(string.Empty)
                });
        }

        private static HttpClient GetHttpClient()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    (m, c, ch, e) => true
            };
            return new HttpClient(handler);
        }

        // ── USERS ─────────────────────────────────────────────────

        public async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                var result = await _client
                    .Child("users")
                    .OnceAsync<User>();

                if (result == null)
                    return new List<User>();

                var users = new List<User>();
                foreach (var item in result)
                {
                    if (item.Object == null) continue;
                    var user = item.Object;
                    if (string.IsNullOrEmpty(user.UniqueKey))
                        user.UniqueKey = item.Key;
                    if (!user.IsDeleted)
                        users.Add(user);
                }

                return users
                    .OrderByDescending(u => u.DateCreated)
                    .ToList();
            }
            catch { return new List<User>(); }
        }

        public async Task<User?> GetUserByEmailAsync(
            string email)
        {
            try
            {
                var users = await GetAllUsersAsync();
                return users.FirstOrDefault(
                    u => u.Email == email &&
                         !u.IsDeleted);
            }
            catch { return null; }
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                var key = string.IsNullOrEmpty(
                    user.UniqueKey)
                    ? user.Id.ToString()
                    : user.UniqueKey;

                await _client
                    .Child("users")
                    .Child(key)
                    .PutAsync(user);
                return true;
            }
            catch { return false; }
        }

        // ── TOOLS ─────────────────────────────────────────────────

        public async Task<List<Tool>> GetAllToolsAsync()
        {
            try
            {
                var result = await _client
                    .Child("tools")
                    .OnceAsync<Tool>();

                if (result == null)
                    return new List<Tool>();

                var tools = new List<Tool>();

                foreach (var item in result)
                {
                    if (item.Object == null) continue;

                    var tool = item.Object;

                    // ✅ Ensure required fields are not null
                    if (string.IsNullOrEmpty(tool.ToolId))
                        tool.ToolId = item.Key;

                    tool.ToolName =
                        tool.ToolName ?? string.Empty;
                    tool.Status =
                        tool.Status ?? "Available";
                    tool.Condition =
                        tool.Condition ?? "Good";
                    tool.CatalogId =
                        tool.CatalogId ?? string.Empty;
                    tool.AssignedWorkerId =
                        tool.AssignedWorkerId ?? string.Empty;
                    tool.AssignedWorkerName =
                        tool.AssignedWorkerName ?? string.Empty;
                    tool.ProjectId =
                        tool.ProjectId ?? string.Empty;
                    tool.QrCode =
                        tool.QrCode ?? string.Empty;

                    if (!tool.IsDeleted)
                        tools.Add(tool);
                }

                return tools
                    .OrderBy(t => t.ToolName)
                    .ThenBy(t => t.ToolId)
                    .ToList();
            }
            catch { return new List<Tool>(); }
        }

        public async Task<Tool?> GetToolByIdAsync(
    string toolId)
        {
            try
            {
                // ✅ Use GetAllTools and filter
                // instead of direct node access
                // which can return null on missing fields
                var allTools = await GetAllToolsAsync();

                return allTools.FirstOrDefault(
                    t => t.ToolId == toolId);
            }
            catch { return null; }
        }

        public async Task<bool> CreateToolAsync(Tool tool)
        {
            try
            {
                await _client
                    .Child("tools")
                    .Child(tool.ToolId)
                    .PutAsync(tool);
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> UpdateToolAsync(Tool tool)
        {
            try
            {
                await _client
                    .Child("tools")
                    .Child(tool.ToolId)
                    .PutAsync(tool);
                return true;
            }
            catch { return false; }
        }

        // ── CATALOGS ──────────────────────────────────────────────

        public async Task<List<EquipmentCatalog>>
            GetAllCatalogsAsync()
        {
            try
            {
                var result = await _client
                    .Child("catalogs")
                    .OnceAsync<EquipmentCatalog>();

                if (result == null)
                    return new List<EquipmentCatalog>();

                return result
                    .Where(c => c.Object != null &&
                                !c.Object.IsDeleted)
                    .Select(c => c.Object)
                    .OrderBy(c => c.CatalogName)
                    .ToList();
            }
            catch { return new List<EquipmentCatalog>(); }
        }

        public async Task<bool> CreateCatalogAsync(
            EquipmentCatalog catalog)
        {
            try
            {
                await _client
                    .Child("catalogs")
                    .Child(catalog.CatalogId)
                    .PutAsync(catalog);
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> UpdateCatalogAsync(
            EquipmentCatalog catalog)
        {
            try
            {
                await _client
                    .Child("catalogs")
                    .Child(catalog.CatalogId)
                    .PutAsync(catalog);
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> DeleteCatalogAsync(
            string catalogId)
        {
            try
            {
                await _client
                    .Child("catalogs")
                    .Child(catalogId)
                    .DeleteAsync();
                return true;
            }
            catch { return false; }
        }

        // ── PROJECTS ──────────────────────────────────────────────

        public async Task<List<Project>>
            GetAllProjectsAsync()
        {
            try
            {
                var result = await _client
                    .Child("projects")
                    .OnceAsync<Project>();

                if (result == null)
                    return new List<Project>();

                return result
                    .Where(p => p.Object != null &&
                                !p.Object.IsDeleted)
                    .Select(p => p.Object)
                    .OrderByDescending(p => p.StartDate)
                    .ToList();
            }
            catch { return new List<Project>(); }
        }

        public async Task<Project?> GetProjectByIdAsync(string projectId)
        {
            try
            {
                var result = await _client
                    .Child("projects")
                    .Child(projectId)
                    .OnceSingleAsync<Project>();

                return result;
            }
            catch { return null; }
        }

        public async Task<Project?> GetActiveProjectAsync()
        {
            try
            {
                var projects = await GetAllProjectsAsync();
                return projects.FirstOrDefault(
                    p => p.Status == "Active");
            }
            catch { return null; }
        }

        public async Task<bool> CreateProjectAsync(
            Project project)
        {
            try
            {
                await _client
                    .Child("projects")
                    .Child(project.ProjectId)
                    .PutAsync(project);
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> UpdateProjectAsync(
            Project project)
        {
            try
            {
                await _client
                    .Child("projects")
                    .Child(project.ProjectId)
                    .PutAsync(project);
                return true;
            }
            catch { return false; }
        }

        // ── PROJECT WORKERS ───────────────────────────────────────

        public async Task<List<string>> GetProjectWorkerKeysAsync(string projectId)
        {
            try
            {
                List<string> keys = new();

                // Retry up to 3 times — Firebase sometimes returns incomplete data on first read
                for (int i = 0; i < 3; i++)
                {
                    var result = await _client
                        .Child("projectWorkers")
                        .Child(projectId)
                        .OnceAsync<object>();

                    if (result == null) continue;

                    keys = result
                        .Where(r => r.Key != null && r.Key != "worker-default")
                        .Select(r => r.Key)
                        .ToList();

                    System.Diagnostics.Debug.WriteLine(
                        $"GetProjectWorkerKeys attempt {i + 1}: found {keys.Count} keys for {projectId}: [{string.Join(", ", keys)}]");

                    if (keys.Count > 0) break;

                    await Task.Delay(200); // wait 200ms before retry
                }

                return keys;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetProjectWorkerKeys ERROR: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<bool> AssignWorkerToProjectAsync(
            string projectId, string workerKey)
        {
            try
            {
                await _client
                    .Child("projectWorkers")
                    .Child(projectId)
                    .Child(workerKey)
                    .PutAsync(true);
                return true;
            }
            catch { return false; }
        }

        public async Task<bool>
            RemoveWorkerFromProjectAsync(
            string projectId, string workerKey)
        {
            try
            {
                await _client
                    .Child("projectWorkers")
                    .Child(projectId)
                    .Child(workerKey)
                    .DeleteAsync();
                return true;
            }
            catch { return false; }
        }

        // ── PROJECT TOOLS ─────────────────────────────────────────

        public async Task<List<string>>
            GetProjectToolIdsAsync(string projectId)
        {
            try
            {
                var result = await _client
                    .Child("projectTools")
                    .Child(projectId)
                    .OnceAsync<bool>();

                if (result == null)
                    return new List<string>();

                return result
                    .Select(r => r.Key)
                    .ToList();
            }
            catch { return new List<string>(); }
        }

        public async Task<bool> DeployToolToProjectAsync(
            string projectId, string toolId)
        {
            try
            {
                await _client
                    .Child("projectTools")
                    .Child(projectId)
                    .Child(toolId)
                    .PutAsync(true);
                return true;
            }
            catch { return false; }
        }

        // ── DAMAGE REPORTS ────────────────────────────────────────

        public async Task<List<DamageReport>>
            GetAllDamageReportsAsync()
        {
            try
            {
                var result = await _client
                    .Child("damageReports")
                    .OnceAsync<DamageReport>();

                if (result == null)
                    return new List<DamageReport>();

                return result
                    .Where(r => r.Object != null)
                    .Select(r => r.Object)
                    .OrderByDescending(r => r.ReportDate)
                    .ToList();
            }
            catch { return new List<DamageReport>(); }
        }

        public async Task<bool> UpdateDamageReportAsync(
            string key, DamageReport report)
        {
            try
            {
                await _client
                    .Child("damageReports")
                    .Child(key)
                    .PutAsync(report);
                return true;
            }
            catch { return false; }
        }

       
        public async Task<List<DamageReportResult>>
    GetAllDamageReportsRawAsync()
        {
            try
            {
                var result = await _client
                    .Child("damageReports")
                    .OnceAsync<DamageReport>();

                if (result == null)
                    return new List<DamageReportResult>();

                return result
                    .Where(r => r.Object != null)
                    .Select(r => new DamageReportResult
                    {
                        Key = r.Key,
                        Report = r.Object
                    })
                    .OrderByDescending(r =>
                        r.Report.ReportDate)
                    .ToList();
            }
            catch
            {
                return new List<DamageReportResult>();
            }
        }

        // ── TRANSACTIONS ──────────────────────────────────────────

        public async Task<List<TransactionLog>>
    GetAllTransactionsAsync()
        {
            try
            {
                // ✅ Match exact node name used by mobile
                var result = await _client
                    .Child("transactions")
                    .OnceAsync<TransactionLog>();

                if (result == null || !result.Any())
                {
                    System.Diagnostics.Debug.WriteLine(
                        "GetAllTransactions: No results");
                    return new List<TransactionLog>();
                }

                System.Diagnostics.Debug.WriteLine(
                    $"GetAllTransactions: " +
                    $"Found {result.Count()} raw items");

                var transactions = new List<TransactionLog>();

                foreach (var item in result)
                {
                    if (item?.Object == null)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"Skipping null item: {item?.Key}");
                        continue;
                    }

                    var tx = item.Object;

                    // ✅ Sync key if needed
                    if (string.IsNullOrEmpty(tx.ToolId))
                        tx.ToolId = string.Empty;

                    // ✅ Fix all nullable fields
                    tx.ToolId = tx.ToolId ?? string.Empty;
                    tx.ToolName = tx.ToolName ?? string.Empty;
                    tx.WorkerId = tx.WorkerId ?? string.Empty;
                    tx.WorkerName = tx.WorkerName ?? string.Empty;
                    tx.Action = tx.Action ?? string.Empty;
                    tx.Description = tx.Description ?? string.Empty;
                    tx.Condition = tx.Condition ?? "Good";

                    System.Diagnostics.Debug.WriteLine(
                        $"TX: {tx.Action} | " +
                        $"{tx.ToolName} | " +
                        $"{tx.WorkerName} | " +
                        $"{tx.Date}");

                    transactions.Add(tx);
                }

                System.Diagnostics.Debug.WriteLine(
                    $"GetAllTransactions: " +
                    $"Returning {transactions.Count} items");

                return transactions
                    .OrderByDescending(t => t.Date)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"GetAllTransactions error: " +
                    $"{ex.GetType().Name}: {ex.Message}");
                return new List<TransactionLog>();
            }
        }

        // ✅ Add this method too — same as mobile
        public async Task<List<TransactionLog>>
            GetToolTransactionsAsync(string toolId)
        {
            try
            {
                var all = await GetAllTransactionsAsync();
                return all
                    .Where(t => t.ToolId == toolId)
                    .OrderByDescending(t => t.Date)
                    .ToList();
            }
            catch { return new List<TransactionLog>(); }
        }

        public async Task<List<TransactionLog>>
            GetWorkerTransactionsAsync(string workerId)
        {
            try
            {
                var all = await GetAllTransactionsAsync();
                return all
                    .Where(t => t.WorkerId == workerId)
                    .OrderByDescending(t => t.Date)
                    .ToList();
            }
            catch { return new List<TransactionLog>(); }
        }
        // ── PAUSE REQUESTS ────────────────────────────────────────────

        public async Task<List<PauseRequest>>
            GetAllPauseRequestsAsync()
        {
            try
            {
                var result = await _client
                    .Child("pauseRequests")
                    .OnceAsync<PauseRequest>();

                if (result == null)
                    return new List<PauseRequest>();

                return result
                    .Where(r => r.Object != null)
                    .Select(r => r.Object)
                    .OrderByDescending(r => r.RequestDate)
                    .ToList();
            }
            catch { return new List<PauseRequest>(); }
        }

        // ✅ Replace tuple version with class version
        public async Task<List<PauseRequestResult>>
            GetAllPauseRequestsRawAsync()
        {
            try
            {
                var result = await _client
                    .Child("pauseRequests")
                    .OnceAsync<PauseRequest>();

                if (result == null)
                    return new List<PauseRequestResult>();

                return result
                    .Where(r => r.Object != null)
                    .Select(r => new PauseRequestResult
                    {
                        Key = r.Key,
                        Report = r.Object
                    })
                    .OrderByDescending(r =>
                        r.Report.RequestDate)
                    .ToList();
            }
            catch
            {
                return new List<PauseRequestResult>();
            }
        }

        public async Task<bool> UpdatePauseRequestAsync(
            string key, PauseRequest request)
        {
            try
            {
                await _client
                    .Child("pauseRequests")
                    .Child(key)
                    .PutAsync(request);
                return true;
            }
            catch { return false; }
        }
        // ── ADD direct assignment method ──────────────────────────────
        public async Task<bool> DirectAssignToolAsync(
            string toolId,
            string workerId,
            string workerName)
        {
            try
            {
                var tool = await GetToolByIdAsync(toolId);
                if (tool == null) return false;

                // ✅ Assign directly — status = Borrowed
                tool.Status = "Borrowed";
                tool.AssignedWorkerId = workerId;
                tool.AssignedWorkerName = workerName;
                tool.BorrowDate = DateTime.Now;

                await UpdateToolAsync(tool);

                // ✅ Log transaction
                await LogTransactionAsync(
                    new TransactionLog
                    {
                        ToolId = tool.ToolId,
                        ToolName = tool.ToolName,
                        WorkerId = workerId,
                        WorkerName = workerName,
                        Action = "Borrowed",
                        Description =
                            $"Directly assigned by " +
                            $"Project Engineer",
                        Condition = tool.Condition,
                        Date = DateTime.Now
                    });

                return true;
            }
            catch { return false; }
        }

        // ── ADD Log Transaction method ────────────────────────────────
        public async Task LogTransactionAsync(
            TransactionLog log)
        {
            try
            {
                await _client
                    .Child("transactions")
                    .PostAsync(log);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"LogTransaction error: {ex.Message}");
            }
        }

        // ── ADD Unassign Tool method ──────────────────────────────────
        public async Task<bool> UnassignToolAsync(
            string toolId)
        {
            try
            {
                var tool = await GetToolByIdAsync(toolId);
                if (tool == null) return false;

                tool.Status = "Available";
                tool.AssignedWorkerId = string.Empty;
                tool.AssignedWorkerName = string.Empty;
                tool.BorrowDate = null;

                await UpdateToolAsync(tool);
                return true;
            }
            catch { return false; }
        }
    }
}