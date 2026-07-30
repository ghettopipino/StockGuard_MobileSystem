using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.Maui.Controls;
using Newtonsoft.Json;
using StockGuard.Constants;
using StockGuard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Disposables;

namespace StockGuard.Services
{
    public class FirebaseService
    {
        private readonly FirebaseClient _client;

        // ── Tool cache ────────────────────────────────────────────────────────
        private List<Tool>? _toolCache;
        private DateTime _toolCacheTime = DateTime.MinValue;

        // ── Catalog cache ─────────────────────────────────────────────────────
        private List<EquipmentCatalog>? _catalogCache;
        private DateTime _catalogCacheTime = DateTime.MinValue;

        // ── Transaction cache ─────────────────────────────────────────────────
        private List<TransactionLog>? _transactionCache;
        private DateTime _transactionCacheTime = DateTime.MinValue;

        // Tools and catalogs change infrequently — 3 minute TTL
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(3);
        // Transactions change more often — 1 minute TTL
        private static readonly TimeSpan TransactionCacheTtl = TimeSpan.FromMinutes(1);

        // ── Constructor ───────────────────────────────────────────────────────
        public FirebaseService()
        {
            _client = new FirebaseClient(
                FirebaseConfig.DatabaseUrl,
                new FirebaseOptions
                {
                    AuthTokenAsyncFactory = () =>
                        Task.FromResult(string.Empty)
                });
        }

        private static HttpClient GetHttpClient()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    (message, cert, chain, errors) => true
            };
            return new HttpClient(handler);
        }

        // ── Cache invalidation ────────────────────────────────────────────────
        public void InvalidateToolCache() => _toolCache = null;
        public void InvalidateCatalogCache() => _catalogCache = null;
        public void InvalidateTransactionCache() => _transactionCache = null;

        // ── USERS ─────────────────────────────────────────────────────────────

        public async Task<bool> CreateUserWithKeyAsync(User user)
        {
            try
            {
                var key = string.IsNullOrEmpty(user.UniqueKey)
                    ? user.Id.ToString()
                    : user.UniqueKey;

                await _client
                    .Child("users")
                    .Child(key)
                    .PutAsync(user);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"CreateUserWithKeyAsync error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CreateUserAsync(User user)
        {
            try
            {
                await _client
                    .Child("users")
                    .Child(user.Id.ToString())
                    .PutAsync(user);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"CreateUserAsync error: {ex.Message}");
                return false;
            }
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                var result = await _client
                    .Child("users")
                    .OnceAsync<User>();

                if (result == null || result.Count == 0)
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

                System.Diagnostics.Debug.WriteLine(
                    $"GetAllUsersAsync: Found {users.Count} users");

                foreach (var u in users)
                    System.Diagnostics.Debug.WriteLine(
                        $"  → Email: {u.Email} | UniqueKey: {u.UniqueKey}");

                return users
                    .OrderByDescending(u => u.DateCreated)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"GetAllUsersAsync error: {ex.Message}");
                return new List<User>();
            }
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            try
            {
                var result = await _client
                    .Child("users")
                    .OnceAsync<User>();

                if (result == null) return null;

                foreach (var item in result)
                {
                    if (item.Object == null) continue;

                    var user = item.Object;

                    if (string.IsNullOrEmpty(user.UniqueKey))
                        user.UniqueKey = item.Key;

                    if (user.Email == email && !user.IsDeleted)
                        return user;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"GetUserByEmailAsync error: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                var key = string.IsNullOrEmpty(user.UniqueKey)
                    ? user.Id.ToString()
                    : user.UniqueKey;

                await _client
                    .Child("users")
                    .Child(key)
                    .PutAsync(user);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"UpdateUserAsync error: {ex.Message}");
                return false;
            }
        }

        // ── TOOLS ─────────────────────────────────────────────────────────────

        public async Task<Tool?> GetToolByIdAsync(string toolId)
        {
            try
            {
                return await _client
                    .Child("tools")
                    .Child(toolId)
                    .OnceSingleAsync<Tool>();
            }
            catch { return null; }
        }

        public async Task<List<Tool>> GetAllToolsAsync(bool forceRefresh = false)
        {
            if (!forceRefresh
                && _toolCache != null
                && DateTime.UtcNow - _toolCacheTime < CacheTtl)
            {
                return _toolCache;
            }

            try
            {
                var result = await _client
                    .Child("tools")
                    .OnceAsync<Tool>();

                _toolCache = result
                    ?.Where(t => t.Object != null && !t.Object.IsDeleted)
                    .Select(t => t.Object)
                    .ToList()
                    ?? new List<Tool>();

                _toolCacheTime = DateTime.UtcNow;
                return _toolCache;
            }
            catch
            {
                return _toolCache ?? new List<Tool>();
            }
        }

        public async Task<List<Tool>> GetToolsByWorkerAsync(string workerId)
        {
            try
            {
                var tools = await GetAllToolsAsync();
                return tools
                    .Where(t => t.AssignedWorkerId == workerId)
                    .ToList();
            }
            catch { return new List<Tool>(); }
        }

        public async Task<List<Tool>> GetToolsByCatalogAsync(string catalogId)
        {
            try
            {
                var all = await GetAllToolsAsync();
                return all
                    .Where(t => t.CatalogId == catalogId)
                    .ToList();
            }
            catch { return new List<Tool>(); }
        }

        public async Task<bool> UpdateToolAsync(Tool tool)
        {
            try
            {
                await _client
                    .Child("tools")
                    .Child(tool.ToolId)
                    .PutAsync(tool);

                InvalidateToolCache();
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> CreateToolAsync(Tool tool)
        {
            try
            {
                await _client
                    .Child("tools")
                    .Child(tool.ToolId)
                    .PutAsync(tool);

                InvalidateToolCache();
                return true;
            }
            catch { return false; }
        }

        // ── BORROW REQUESTS ───────────────────────────────────────────────────

        public async Task<string> CreateBorrowRequestAsync(BorrowRequest request)
        {
            try
            {
                var result = await _client
                    .Child("borrowRequests")
                    .PostAsync(request);
                return result.Key;
            }
            catch { return string.Empty; }
        }

        public async Task<bool> UpdateBorrowRequestAsync(
            string key, BorrowRequest request)
        {
            try
            {
                await _client
                    .Child("borrowRequests")
                    .Child(key)
                    .PutAsync(request);
                return true;
            }
            catch { return false; }
        }

        public async Task<List<BorrowRequest>> GetPendingRequestsForWorkerAsync(
            string workerId)
        {
            try
            {
                var result = await _client
                    .Child("borrowRequests")
                    .OnceAsync<BorrowRequest>();

                if (result == null)
                    return new List<BorrowRequest>();

                return result
                    .Where(r => r.Object != null)
                    .Select(r => r.Object)
                    .Where(r => r.OwnerId == workerId && r.Status == "Pending")
                    .ToList();
            }
            catch { return new List<BorrowRequest>(); }
        }

        public async Task<List<BorrowRequestResult>> GetAllBorrowRequestsRawAsync()
        {
            try
            {
                var result = await _client
                    .Child("borrowRequests")
                    .OnceAsync<BorrowRequest>();

                if (result == null)
                    return new List<BorrowRequestResult>();

                return result
                    .Where(r => r.Object != null)
                    .Select(r => new BorrowRequestResult
                    {
                        Key = r.Key,
                        Request = r.Object
                    })
                    .OrderByDescending(r => r.Request.RequestDate)
                    .ToList();
            }
            catch { return new List<BorrowRequestResult>(); }
        }

        // ── TRANSACTIONS ──────────────────────────────────────────────────────

        public async Task LogTransactionAsync(TransactionLog log)
        {
            try
            {
                await _client
                    .Child("transactions")
                    .PostAsync(log);

                InvalidateTransactionCache();
            }
            catch { }
        }

        /// <summary>
        /// Fetches ALL transactions. Used as the single source of truth for
        /// GetToolTransactionsAsync and GetWorkerTransactionsAsync so Firebase
        /// is only hit once per TTL window regardless of which method is called.
        /// </summary>
        public async Task<List<TransactionLog>> GetAllTransactionsAsync(
            bool forceRefresh = false)
        {
            if (!forceRefresh
                && _transactionCache != null
                && DateTime.UtcNow - _transactionCacheTime < TransactionCacheTtl)
            {
                return _transactionCache;
            }

            try
            {
                var result = await _client
                    .Child("transactions")
                    .OnceAsync<TransactionLog>();

                _transactionCache = result
                    ?.Where(t => t.Object != null)
                    .Select(t => t.Object)
                    .OrderByDescending(t => t.Date)
                    .ToList()
                    ?? new List<TransactionLog>();

                _transactionCacheTime = DateTime.UtcNow;
                return _transactionCache;
            }
            catch
            {
                return _transactionCache ?? new List<TransactionLog>();
            }
        }

        /// <summary>
        /// Returns transactions for a specific tool.
        /// Uses the shared transaction cache — no extra Firebase call
        /// if GetAllTransactionsAsync was recently called.
        /// </summary>
        public async Task<List<TransactionLog>> GetToolTransactionsAsync(
            string toolId, bool forceRefresh = false)
        {
            var all = await GetAllTransactionsAsync(forceRefresh);
            return all
                .Where(t => t.ToolId == toolId)
                .OrderByDescending(t => t.Date)
                .ToList();
        }

        /// <summary>
        /// Returns transactions for a specific worker.
        /// Uses the shared transaction cache — no extra Firebase call
        /// if GetAllTransactionsAsync was recently called.
        /// </summary>
        public async Task<List<TransactionLog>> GetWorkerTransactionsAsync(
            string workerId, bool forceRefresh = false)
        {
            var all = await GetAllTransactionsAsync(forceRefresh);
            return all
                .Where(t => t.WorkerId == workerId)
                .OrderByDescending(t => t.Date)
                .ToList();
        }

        // ── DAMAGE REPORTS ────────────────────────────────────────────────────

        public async Task<string> SubmitDamageReportAsync(DamageReport report)
        {
            try
            {
                var result = await _client
                    .Child("damageReports")
                    .PostAsync(report);
                return result.Key;
            }
            catch { return string.Empty; }
        }

        public async Task<List<DamageReportResult>> GetAllDamageReportsRawAsync()
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
                    .OrderByDescending(r => r.Report.ReportDate)
                    .ToList();
            }
            catch { return new List<DamageReportResult>(); }
        }

        public async Task<List<DamageReport>> GetAllDamageReportsAsync()
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

        // ── CATALOGS ──────────────────────────────────────────────────────────

        /// <summary>
        /// Single GetAllCatalogsAsync — cached, with forceRefresh support.
        /// The old uncached version has been removed to eliminate the
        /// duplicate method compiler error.
        /// </summary>
        public async Task<List<EquipmentCatalog>> GetAllCatalogsAsync(
            bool forceRefresh = false)
        {
            if (!forceRefresh
                && _catalogCache != null
                && DateTime.UtcNow - _catalogCacheTime < CacheTtl)
            {
                return _catalogCache;
            }

            try
            {
                var result = await _client
                    .Child("catalogs")
                    .OnceAsync<EquipmentCatalog>();

                _catalogCache = result
                    ?.Where(c => c.Object != null && !c.Object.IsDeleted)
                    .Select(c => c.Object)
                    .OrderBy(c => c.CatalogName)
                    .ToList()
                    ?? new List<EquipmentCatalog>();

                _catalogCacheTime = DateTime.UtcNow;
                return _catalogCache;
            }
            catch
            {
                return _catalogCache ?? new List<EquipmentCatalog>();
            }
        }

        public async Task<bool> CreateCatalogAsync(EquipmentCatalog catalog)
        {
            try
            {
                await _client
                    .Child("catalogs")
                    .Child(catalog.CatalogId)
                    .PutAsync(catalog);

                InvalidateCatalogCache();
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> UpdateCatalogAsync(EquipmentCatalog catalog)
        {
            try
            {
                await _client
                    .Child("catalogs")
                    .Child(catalog.CatalogId)
                    .PutAsync(catalog);

                InvalidateCatalogCache();
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> DeleteCatalogAsync(string catalogId)
        {
            try
            {
                await _client
                    .Child("catalogs")
                    .Child(catalogId)
                    .DeleteAsync();

                InvalidateCatalogCache();
                return true;
            }
            catch { return false; }
        }

        // ── SEED DATA ─────────────────────────────────────────────────────────

        public async Task SeedToolsIfEmptyAsync()
        {
            try
            {
                var tools = await GetAllToolsAsync();
                if (tools.Count > 0) return;

                var sampleTools = new List<Tool>
                {
                    new Tool { ToolId = "PD-001", ToolName = "Power Drill",   Status = "Available", QrCode = "PD-001", CatalogId = "CAT-001" },
                    new Tool { ToolId = "PD-002", ToolName = "Power Drill",   Status = "Available", QrCode = "PD-002", CatalogId = "CAT-001" },
                    new Tool { ToolId = "PD-003", ToolName = "Power Drill",   Status = "Available", QrCode = "PD-003", CatalogId = "CAT-001" },
                    new Tool { ToolId = "JH-001", ToolName = "Jack Hammer",   Status = "Available", QrCode = "JH-001", CatalogId = "CAT-002" },
                    new Tool { ToolId = "JH-002", ToolName = "Jack Hammer",   Status = "Available", QrCode = "JH-002", CatalogId = "CAT-002" },
                    new Tool { ToolId = "LS-001", ToolName = "L-Shape Ruler", Status = "Available", QrCode = "LS-001", CatalogId = "CAT-003" },
                    new Tool { ToolId = "LS-002", ToolName = "L-Shape Ruler", Status = "Available", QrCode = "LS-002", CatalogId = "CAT-003" },
                };

                foreach (var tool in sampleTools)
                    await CreateToolAsync(tool);
            }
            catch { }
        }

        // ── TRANSFER REQUESTS ─────────────────────────────────────────────────

        public async Task<string> CreateTransferRequestAsync(
            TransferRequest request)
        {
            try
            {
                var result = await _client
                    .Child("transferRequests")
                    .PostAsync(request);
                return result.Key;
            }
            catch { return string.Empty; }
        }

        public async Task<bool> UpdateTransferRequestAsync(
            string key, TransferRequest request)
        {
            try
            {
                await _client
                    .Child("transferRequests")
                    .Child(key)
                    .PutAsync(request);
                return true;
            }
            catch { return false; }
        }

        public async Task<List<TransferRequestResult>> GetAllTransferRequestsRawAsync()
        {
            try
            {
                var result = await _client
                    .Child("transferRequests")
                    .OnceAsync<TransferRequest>();

                if (result == null)
                    return new List<TransferRequestResult>();

                return result
                    .Where(r => r.Object != null)
                    .Select(r => new TransferRequestResult
                    {
                        Key = r.Key,
                        Request = r.Object
                    })
                    .OrderByDescending(r => r.Request.RequestDate)
                    .ToList();
            }
            catch { return new List<TransferRequestResult>(); }
        }

        // ── PROJECTS ──────────────────────────────────────────────────────────

        public async Task<bool> CreateProjectAsync(Project project)
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

        public async Task<bool> UpdateProjectAsync(Project project)
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

        public async Task<List<Project>> GetAllProjectsAsync()
        {
            try
            {
                var result = await _client
                    .Child("projects")
                    .OnceAsync<Project>();

                if (result == null)
                    return new List<Project>();

                return result
                    .Where(p => p.Object != null && !p.Object.IsDeleted)
                    .Select(p => p.Object)
                    .OrderByDescending(p => p.StartDate)
                    .ToList();
            }
            catch { return new List<Project>(); }
        }

        public async Task<Project?> GetActiveProjectAsync()
        {
            try
            {
                var projects = await GetAllProjectsAsync();
                return projects.FirstOrDefault(p => p.Status == "Active");
            }
            catch { return null; }
        }

        public async Task<bool> SetActiveProjectAsync(string projectId)
        {
            try
            {
                var projects = await GetAllProjectsAsync();

                foreach (var project in projects.Where(p => p.Status == "Active"))
                {
                    project.Status = "Paused";
                    await UpdateProjectAsync(project);
                }

                var selected = projects.FirstOrDefault(
                    p => p.ProjectId == projectId);

                if (selected != null)
                {
                    selected.Status = "Active";
                    await UpdateProjectAsync(selected);
                }

                return true;
            }
            catch { return false; }
        }

        // ── PROJECT WORKERS ───────────────────────────────────────────────────

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

        public async Task<bool> RemoveWorkerFromProjectAsync(
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

        public async Task<List<string>> GetProjectWorkerKeysAsync(string projectId)
        {
            try
            {
                var result = await _client
                    .Child("projectWorkers")
                    .Child(projectId)
                    .OnceAsync<bool>();

                if (result == null)
                    return new List<string>();

                return result.Select(r => r.Key).ToList();
            }
            catch { return new List<string>(); }
        }

        // ── PROJECT TOOLS ─────────────────────────────────────────────────────

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

        public async Task<bool> RemoveToolFromProjectAsync(
            string projectId, string toolId)
        {
            try
            {
                await _client
                    .Child("projectTools")
                    .Child(projectId)
                    .Child(toolId)
                    .DeleteAsync();
                return true;
            }
            catch { return false; }
        }

        public async Task<List<string>> GetProjectToolIdsAsync(string projectId)
        {
            try
            {
                var result = await _client
                    .Child("projectTools")
                    .Child(projectId)
                    .OnceAsync<bool>();

                if (result == null)
                    return new List<string>();

                return result.Select(r => r.Key).ToList();
            }
            catch { return new List<string>(); }
        }

        // ── PAUSE REQUESTS ────────────────────────────────────────────────────

        public async Task<string> CreatePauseRequestAsync(PauseRequest request)
        {
            try
            {
                var result = await _client
                    .Child("pauseRequests")
                    .PostAsync(request);
                return result.Key;
            }
            catch { return string.Empty; }
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

        public async Task<List<PauseRequestResult>> GetAllPauseRequestsRawAsync()
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
                        Request = r.Object
                    })
                    .OrderByDescending(r => r.Request.RequestDate)
                    .ToList();
            }
            catch { return new List<PauseRequestResult>(); }
        }

        // ── GLOBAL REAL-TIME LISTENER ─────────────────────────────────────────

        public void StartGlobalListener(Action onChanged)
        {
            var nodes = new[]
            {
                "tools", "transactions", "borrowRequests",
                "transferRequests", "damageReports", "pauseRequests",
                "users", "projects", "projectTools",
                "projectWorkers", "catalogs"
            };

            foreach (var node in nodes)
            {
                _client
                    .Child(node)
                    .AsObservable<object>()
                    .Skip(1)
                    .Subscribe(
                        _ => MainThread.BeginInvokeOnMainThread(onChanged),
                        ex => System.Diagnostics.Debug.WriteLine(
                            $"Listener error [{node}]: {ex.Message}")
                    );
            }
        }

        public IDisposable StartGlobalListenerDisposable(Action onChanged)
        {
            var cts = new CancellationTokenSource();

            Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(
                            TimeSpan.FromSeconds(10),
                            cts.Token);

                        MainThread.BeginInvokeOnMainThread(onChanged);
                    }
                    catch (TaskCanceledException) { break; }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"Polling error: {ex.Message}");
                    }
                }
            }, cts.Token);

            return new CancellationDisposable(cts);
        }

        // ── PRE-ASSIGNMENTS ───────────────────────────────────────────────────

        public async Task<string> PreAssignToolAsync(
    string toolId, string toolName,
    string workerId, string workerName,
    string projectId, string projectName,
    string assignedByName)
        {
            try
            {
                var preAssign = new PreAssignment
                {
                    ToolId = toolId,
                    ToolName = toolName,
                    WorkerId = workerId,
                    WorkerName = workerName,
                    ProjectId = projectId,
                    ProjectName = projectName,
                    AssignedByName = assignedByName,
                    Status = "Pending",
                    DateCreated = DateTime.Now
                };

                var result = await _client
                    .Child("preAssignments")
                    .PostAsync(preAssign);

                return result.Key;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"PreAssignToolAsync error: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task<List<PreAssignmentResult>> GetPendingAssignmentsForWorkerAsync(
            string workerId)
        {
            try
            {
                var result = await _client
                    .Child("preAssignments")
                    .OnceAsync<PreAssignment>();

                if (result == null)
                    return new List<PreAssignmentResult>();

                return result
                    .Where(r => r.Object != null &&
                                r.Object.WorkerId == workerId &&
                                r.Object.Status == "Pending")
                    .Select(r => new PreAssignmentResult
                    {
                        Key = r.Key,
                        Assignment = r.Object
                    })
                    .OrderByDescending(r => r.Assignment.DateCreated)
                    .ToList();
            }
            catch { return new List<PreAssignmentResult>(); }
        }

        public async Task<bool> ConfirmAssignmentAsync(
            string assignmentKey, PreAssignment assignment)
        {
            try
            {
                var tool = await GetToolByIdAsync(assignment.ToolId);
                if (tool == null) return false;

                if (tool.Status == "Borrowed")
                    return false; // someone else already has it

                tool.AssignedWorkerId = assignment.WorkerId;
                tool.AssignedWorkerName = assignment.WorkerName;
                tool.BorrowDate = DateTime.Now;
                tool.Status = "Borrowed";
                await UpdateToolAsync(tool);

                await LogTransactionAsync(new TransactionLog
                {
                    ToolId = tool.ToolId,
                    ToolName = tool.ToolName,
                    WorkerId = assignment.WorkerId,
                    WorkerName = assignment.WorkerName,
                    Action = "Borrowed",
                    Description =
                        $"Accepted assignment from {assignment.AssignedByName}",
                    Condition = tool.Condition,
                    Date = DateTime.Now
                });

                assignment.Status = "Accepted";
                await _client
                    .Child("preAssignments")
                    .Child(assignmentKey)
                    .PutAsync(assignment);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"ConfirmAssignmentAsync error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeclineAssignmentAsync(
            string assignmentKey, PreAssignment assignment)
        {
            try
            {
                assignment.Status = "Declined";
                await _client
                    .Child("preAssignments")
                    .Child(assignmentKey)
                    .PutAsync(assignment);
                return true;
            }
            catch { return false; }
        }
    }
}