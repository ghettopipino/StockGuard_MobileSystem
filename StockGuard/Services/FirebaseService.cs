using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.Maui.Controls;
using StockGuard.Constants;
using StockGuard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StockGuard.Services
{
    public class FirebaseService
    {
        private readonly FirebaseClient _client;

        // ─────────────────────────────────────────────────────────
        // CACHE
        // ─────────────────────────────────────────────────────────

        private List<Tool>? _toolCache;
        private DateTime _toolCacheTime = DateTime.MinValue;

        private List<EquipmentCatalog>? _catalogCache;
        private DateTime _catalogCacheTime = DateTime.MinValue;

        private List<TransactionLog>? _transactionCache;
        private DateTime _transactionCacheTime = DateTime.MinValue;

        private static readonly TimeSpan CacheTtl =
            TimeSpan.FromMinutes(3);

        private static readonly TimeSpan TransactionCacheTtl =
            TimeSpan.FromMinutes(1);

        // ─────────────────────────────────────────────────────────
        // CONSTRUCTOR
        // ─────────────────────────────────────────────────────────

        public FirebaseService()
        {
            _client = new FirebaseClient(
                FirebaseConfig.DatabaseUrl,
                new FirebaseOptions
                {
                    AuthTokenAsyncFactory =
                        () => Task.FromResult(string.Empty)
                });
        }

        // ─────────────────────────────────────────────────────────
        // CACHE INVALIDATION
        // ─────────────────────────────────────────────────────────

        public void InvalidateToolCache()
        {
            _toolCache = null;
        }

        public void InvalidateCatalogCache()
        {
            _catalogCache = null;
        }

        public void InvalidateTransactionCache()
        {
            _transactionCache = null;
        }

        // ─────────────────────────────────────────────────────────
        // USERS
        // ─────────────────────────────────────────────────────────

        public async Task<bool> CreateUserWithKeyAsync(
            User user)
        {
            try
            {
                var key =
                    string.IsNullOrEmpty(user.UniqueKey)
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

        public async Task<bool> CreateUserAsync(
            User user)
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
                var result =
                    await _client
                        .Child("users")
                        .OnceAsync<User>();

                if (result == null ||
                    result.Count == 0)
                {
                    return new List<User>();
                }

                var users =
                    new List<User>();

                foreach (var item in result)
                {
                    if (item.Object == null)
                        continue;

                    var user =
                        item.Object;

                    if (string.IsNullOrEmpty(
                            user.UniqueKey))
                    {
                        user.UniqueKey =
                            item.Key;
                    }

                    if (!user.IsDeleted)
                        users.Add(user);
                }

                return users
                    .OrderByDescending(
                        u => u.DateCreated)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"GetAllUsersAsync error: {ex.Message}");

                return new List<User>();
            }
        }

        public async Task<User?> GetUserByEmailAsync(
            string email)
        {
            try
            {
                var result =
                    await _client
                        .Child("users")
                        .OnceAsync<User>();

                if (result == null)
                    return null;

                foreach (var item in result)
                {
                    if (item.Object == null)
                        continue;

                    var user =
                        item.Object;

                    if (string.IsNullOrEmpty(
                            user.UniqueKey))
                    {
                        user.UniqueKey =
                            item.Key;
                    }

                    if (user.Email == email &&
                        !user.IsDeleted)
                    {
                        return user;
                    }
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

        public async Task<bool> UpdateUserAsync(
            User user)
        {
            try
            {
                var key =
                    string.IsNullOrEmpty(user.UniqueKey)
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

        // ─────────────────────────────────────────────────────────
        // TOOLS
        // ─────────────────────────────────────────────────────────

        public async Task<Tool?> GetToolByIdAsync(
            string toolId)
        {
            try
            {
                return await _client
                    .Child("tools")
                    .Child(toolId)
                    .OnceSingleAsync<Tool>();
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<Tool>> GetAllToolsAsync(
            bool forceRefresh = false)
        {
            if (!forceRefresh &&
                _toolCache != null &&
                DateTime.UtcNow - _toolCacheTime <
                CacheTtl)
            {
                return _toolCache;
            }

            try
            {
                var result =
                    await _client
                        .Child("tools")
                        .OnceAsync<Tool>();

                _toolCache =
                    result?
                        .Where(t =>
                            t.Object != null &&
                            !t.Object.IsDeleted)
                        .Select(t => t.Object)
                        .ToList()
                    ?? new List<Tool>();

                _toolCacheTime =
                    DateTime.UtcNow;

                return _toolCache;
            }
            catch
            {
                return _toolCache ??
                       new List<Tool>();
            }
        }

        public async Task<List<Tool>>
            GetToolsByWorkerAsync(
                string workerId)
        {
            try
            {
                var tools =
                    await GetAllToolsAsync();

                return tools
                    .Where(t =>
                        t.AssignedWorkerId ==
                        workerId)
                    .ToList();
            }
            catch
            {
                return new List<Tool>();
            }
        }

        public async Task<List<Tool>>
            GetToolsByCatalogAsync(
                string catalogId)
        {
            try
            {
                var tools =
                    await GetAllToolsAsync();

                return tools
                    .Where(t =>
                        t.CatalogId ==
                        catalogId)
                    .ToList();
            }
            catch
            {
                return new List<Tool>();
            }
        }

        public async Task<bool> UpdateToolAsync(
            Tool tool)
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
            catch
            {
                return false;
            }
        }

        public async Task<bool> CreateToolAsync(
            Tool tool)
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
            catch
            {
                return false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // BORROW REQUESTS
        // ─────────────────────────────────────────────────────────

        public async Task<string>
            CreateBorrowRequestAsync(
                BorrowRequest request)
        {
            try
            {
                var result =
                    await _client
                        .Child("borrowRequests")
                        .PostAsync(request);

                return result.Key;
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<bool>
            UpdateBorrowRequestAsync(
                string key,
                BorrowRequest request)
        {
            try
            {
                await _client
                    .Child("borrowRequests")
                    .Child(key)
                    .PutAsync(request);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<BorrowRequest>>
            GetPendingRequestsForWorkerAsync(
                string workerId)
        {
            try
            {
                var result =
                    await _client
                        .Child("borrowRequests")
                        .OnceAsync<BorrowRequest>();

                if (result == null)
                    return new List<BorrowRequest>();

                return result
                    .Where(r =>
                        r.Object != null)
                    .Select(r =>
                        r.Object)
                    .Where(r =>
                        r.OwnerId == workerId &&
                        r.Status == "Pending")
                    .ToList();
            }
            catch
            {
                return new List<BorrowRequest>();
            }
        }

        public async Task<List<BorrowRequestResult>>
            GetAllBorrowRequestsRawAsync()
        {
            try
            {
                var result =
                    await _client
                        .Child("borrowRequests")
                        .OnceAsync<BorrowRequest>();

                if (result == null)
                {
                    return new List<
                        BorrowRequestResult>();
                }

                return result
                    .Where(r =>
                        r.Object != null)
                    .Select(r =>
                        new BorrowRequestResult
                        {
                            Key =
                                r.Key,

                            Request =
                                r.Object
                        })
                    .OrderByDescending(r =>
                        r.Request.RequestDate)
                    .ToList();
            }
            catch
            {
                return new List<
                    BorrowRequestResult>();
            }
        }

        // ─────────────────────────────────────────────────────────
        // TRANSACTIONS
        // ─────────────────────────────────────────────────────────

        public async Task LogTransactionAsync(
            TransactionLog log)
        {
            try
            {
                await _client
                    .Child("transactions")
                    .PostAsync(log);

                InvalidateTransactionCache();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"LogTransactionAsync error: {ex.Message}");
            }
        }

        public async Task<List<TransactionLog>>
            GetAllTransactionsAsync(
                bool forceRefresh = false)
        {
            if (!forceRefresh &&
                _transactionCache != null &&
                DateTime.UtcNow -
                _transactionCacheTime <
                TransactionCacheTtl)
            {
                return _transactionCache;
            }

            try
            {
                var result =
                    await _client
                        .Child("transactions")
                        .OnceAsync<TransactionLog>();

                _transactionCache =
                    result?
                        .Where(t =>
                            t.Object != null)
                        .Select(t =>
                            t.Object)
                        .OrderByDescending(t =>
                            t.Date)
                        .ToList()
                    ?? new List<TransactionLog>();

                _transactionCacheTime =
                    DateTime.UtcNow;

                return _transactionCache;
            }
            catch
            {
                return _transactionCache ??
                       new List<TransactionLog>();
            }
        }

        public async Task<List<TransactionLog>>
            GetToolTransactionsAsync(
                string toolId,
                bool forceRefresh = false)
        {
            var all =
                await GetAllTransactionsAsync(
                    forceRefresh);

            return all
                .Where(t =>
                    t.ToolId == toolId)
                .OrderByDescending(t =>
                    t.Date)
                .ToList();
        }

        public async Task<List<TransactionLog>>
            GetWorkerTransactionsAsync(
                string workerId,
                bool forceRefresh = false)
        {
            var all =
                await GetAllTransactionsAsync(
                    forceRefresh);

            return all
                .Where(t =>
                    t.WorkerId == workerId)
                .OrderByDescending(t =>
                    t.Date)
                .ToList();
        }

        // ─────────────────────────────────────────────────────────
        // DAMAGE REPORTS
        // ─────────────────────────────────────────────────────────

        public async Task<string>
            SubmitDamageReportAsync(
                DamageReport report)
        {
            try
            {
                var result =
                    await _client
                        .Child("damageReports")
                        .PostAsync(report);

                return result.Key;
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<List<DamageReportResult>>
            GetAllDamageReportsRawAsync()
        {
            try
            {
                var result =
                    await _client
                        .Child("damageReports")
                        .OnceAsync<DamageReport>();

                if (result == null)
                {
                    return new List<
                        DamageReportResult>();
                }

                return result
                    .Where(r =>
                        r.Object != null)
                    .Select(r =>
                        new DamageReportResult
                        {
                            Key =
                                r.Key,

                            Report =
                                r.Object
                        })
                    .OrderByDescending(r =>
                        r.Report.ReportDate)
                    .ToList();
            }
            catch
            {
                return new List<
                    DamageReportResult>();
            }
        }

        public async Task<List<DamageReport>>
            GetAllDamageReportsAsync()
        {
            try
            {
                var result =
                    await _client
                        .Child("damageReports")
                        .OnceAsync<DamageReport>();

                if (result == null)
                {
                    return new List<
                        DamageReport>();
                }

                return result
                    .Where(r =>
                        r.Object != null)
                    .Select(r =>
                        r.Object)
                    .OrderByDescending(r =>
                        r.ReportDate)
                    .ToList();
            }
            catch
            {
                return new List<
                    DamageReport>();
            }
        }

        public async Task<bool>
            UpdateDamageReportAsync(
                string key,
                DamageReport report)
        {
            try
            {
                await _client
                    .Child("damageReports")
                    .Child(key)
                    .PutAsync(report);

                return true;
            }
            catch
            {
                return false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // CATALOGS
        // ─────────────────────────────────────────────────────────

        public async Task<List<EquipmentCatalog>>
            GetAllCatalogsAsync(
                bool forceRefresh = false)
        {
            if (!forceRefresh &&
                _catalogCache != null &&
                DateTime.UtcNow -
                _catalogCacheTime <
                CacheTtl)
            {
                return _catalogCache;
            }

            try
            {
                var result =
                    await _client
                        .Child("catalogs")
                        .OnceAsync<EquipmentCatalog>();

                _catalogCache =
                    result?
                        .Where(c =>
                            c.Object != null &&
                            !c.Object.IsDeleted)
                        .Select(c =>
                            c.Object)
                        .OrderBy(c =>
                            c.CatalogName)
                        .ToList()
                    ?? new List<EquipmentCatalog>();

                _catalogCacheTime =
                    DateTime.UtcNow;

                return _catalogCache;
            }
            catch
            {
                return _catalogCache ??
                       new List<EquipmentCatalog>();
            }
        }

        public async Task<bool>
            CreateCatalogAsync(
                EquipmentCatalog catalog)
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
            catch
            {
                return false;
            }
        }

        public async Task<bool>
            UpdateCatalogAsync(
                EquipmentCatalog catalog)
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
            catch
            {
                return false;
            }
        }

        public async Task<bool>
            DeleteCatalogAsync(
                string catalogId)
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
            catch
            {
                return false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // TRANSFER REQUESTS
        // ─────────────────────────────────────────────────────────

        public async Task<string>
            CreateTransferRequestAsync(
                TransferRequest request)
        {
            try
            {
                var result =
                    await _client
                        .Child("transferRequests")
                        .PostAsync(request);

                return result.Key;
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<bool>
            UpdateTransferRequestAsync(
                string key,
                TransferRequest request)
        {
            try
            {
                await _client
                    .Child("transferRequests")
                    .Child(key)
                    .PutAsync(request);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<TransferRequestResult>>
            GetAllTransferRequestsRawAsync()
        {
            try
            {
                var result =
                    await _client
                        .Child("transferRequests")
                        .OnceAsync<TransferRequest>();

                if (result == null)
                {
                    return new List<
                        TransferRequestResult>();
                }

                return result
                    .Where(r =>
                        r.Object != null)
                    .Select(r =>
                        new TransferRequestResult
                        {
                            Key =
                                r.Key,

                            Request =
                                r.Object
                        })
                    .OrderByDescending(r =>
                        r.Request.RequestDate)
                    .ToList();
            }
            catch
            {
                return new List<
                    TransferRequestResult>();
            }
        }

        // ─────────────────────────────────────────────────────────
        // PROJECTS
        // ─────────────────────────────────────────────────────────

        public async Task<bool>
            CreateProjectAsync(
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
            catch
            {
                return false;
            }
        }

        public async Task<bool>
            UpdateProjectAsync(
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
            catch
            {
                return false;
            }
        }

        public async Task<List<Project>>
            GetAllProjectsAsync()
        {
            try
            {
                var result =
                    await _client
                        .Child("projects")
                        .OnceAsync<Project>();

                if (result == null)
                    return new List<Project>();

                return result
                    .Where(p =>
                        p.Object != null &&
                        !p.Object.IsDeleted)
                    .Select(p =>
                        p.Object)
                    .OrderByDescending(p =>
                        p.StartDate)
                    .ToList();
            }
            catch
            {
                return new List<Project>();
            }
        }

        public async Task<Project?>
            GetActiveProjectAsync()
        {
            try
            {
                var projects =
                    await GetAllProjectsAsync();

                return projects
                    .FirstOrDefault(p =>
                        p.Status == "Active");
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool>
            SetActiveProjectAsync(
                string projectId)
        {
            try
            {
                var projects =
                    await GetAllProjectsAsync();

                foreach (var project in
                    projects.Where(p =>
                        p.Status == "Active"))
                {
                    project.Status =
                        "Paused";

                    await UpdateProjectAsync(
                        project);
                }

                var selected =
                    projects.FirstOrDefault(p =>
                        p.ProjectId ==
                        projectId);

                if (selected != null)
                {
                    selected.Status =
                        "Active";

                    await UpdateProjectAsync(
                        selected);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // PROJECT WORKERS
        // ─────────────────────────────────────────────────────────

        public async Task<bool>
            AssignWorkerToProjectAsync(
                string projectId,
                string workerKey)
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
            catch
            {
                return false;
            }
        }

        public async Task<bool>
            RemoveWorkerFromProjectAsync(
                string projectId,
                string workerKey)
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
            catch
            {
                return false;
            }
        }

        public async Task<List<string>>
            GetProjectWorkerKeysAsync(
                string projectId)
        {
            try
            {
                var result =
                    await _client
                        .Child("projectWorkers")
                        .Child(projectId)
                        .OnceAsync<bool>();

                if (result == null)
                    return new List<string>();

                return result
                    .Select(r => r.Key)
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        // ─────────────────────────────────────────────────────────
        // PROJECT TOOLS
        // ─────────────────────────────────────────────────────────

        public async Task<bool>
            DeployToolToProjectAsync(
                string projectId,
                string toolId)
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
            catch
            {
                return false;
            }
        }

        public async Task<bool>
            RemoveToolFromProjectAsync(
                string projectId,
                string toolId)
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
            catch
            {
                return false;
            }
        }

        public async Task<List<string>>
            GetProjectToolIdsAsync(
                string projectId)
        {
            try
            {
                var result =
                    await _client
                        .Child("projectTools")
                        .Child(projectId)
                        .OnceAsync<bool>();

                if (result == null)
                    return new List<string>();

                return result
                    .Select(r => r.Key)
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        // ─────────────────────────────────────────────────────────
        // RETURN REQUESTS
        // ─────────────────────────────────────────────────────────

        public async Task<string>
            CreateReturnRequestAsync(
                ReturnRequest request)
        {
            try
            {
                var result =
                    await _client
                        .Child("returnRequests")
                        .PostAsync(request);

                return result.Key;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"CreateReturnRequestAsync error: {ex.Message}");

                return string.Empty;
            }
        }

        public async Task<bool>
            UpdateReturnRequestAsync(
                string key,
                ReturnRequest request)
        {
            try
            {
                await _client
                    .Child("returnRequests")
                    .Child(key)
                    .PutAsync(request);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"UpdateReturnRequestAsync error: {ex.Message}");

                return false;
            }
        }

        public async Task<List<ReturnRequestResult>>
            GetAllReturnRequestsRawAsync()
        {
            try
            {
                var result =
                    await _client
                        .Child("returnRequests")
                        .OnceAsync<ReturnRequest>();

                if (result == null)
                {
                    return new List<
                        ReturnRequestResult>();
                }

                return result
                    .Where(r =>
                        r.Object != null)
                    .Select(r =>
                        new ReturnRequestResult
                        {
                            Key =
                                r.Key,

                            Request =
                                r.Object
                        })
                    .OrderByDescending(r =>
                        r.Request.RequestDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"GetAllReturnRequestsRawAsync error: {ex.Message}");

                return new List<
                    ReturnRequestResult>();
            }
        }

        // ─────────────────────────────────────────────────────────
        // GLOBAL REAL-TIME LISTENER
        // ─────────────────────────────────────────────────────────

        public void StartGlobalListener(
            Action onChanged)
        {
            var nodes =
                new[]
                {
                    "tools",
                    "transactions",
                    "borrowRequests",
                    "transferRequests",
                    "damageReports",
                    "returnRequests",
                    "users",
                    "projects",
                    "projectTools",
                    "projectWorkers",
                    "projectEquipment",
                    "catalogs",
                    "preAssignments"
                };

            foreach (var node in nodes)
            {
                _client
                    .Child(node)
                    .AsObservable<object>()
                    .Skip(1)
                    .Subscribe(
                        _ =>
                            MainThread
                                .BeginInvokeOnMainThread(
                                    onChanged),

                        ex =>
                            System.Diagnostics.Debug
                                .WriteLine(
                                    $"Listener error [{node}]: " +
                                    $"{ex.Message}")
                    );
            }
        }

        public IDisposable
            StartGlobalListenerDisposable(
                Action onChanged)
        {
            var cts =
                new CancellationTokenSource();

            Task.Run(
                async () =>
                {
                    while (!cts.Token
                        .IsCancellationRequested)
                    {
                        try
                        {
                            await Task.Delay(
                                TimeSpan.FromSeconds(10),
                                cts.Token);

                            MainThread
                                .BeginInvokeOnMainThread(
                                    onChanged);
                        }
                        catch (TaskCanceledException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug
                                .WriteLine(
                                    $"Polling error: " +
                                    $"{ex.Message}");
                        }
                    }
                },
                cts.Token);

            return new CancellationDisposable(
                cts);
        }

        // ─────────────────────────────────────────────────────────
        // PRE-ASSIGNMENTS
        // ─────────────────────────────────────────────────────────

        public async Task<bool>
            CreatePreAssignmentAsync(
                PreAssignment assignment)
        {
            try
            {
                var tool =
                    await GetToolByIdAsync(
                        assignment.ToolId);

                if (tool == null ||
                    tool.Status != "Available")
                {
                    return false;
                }

                var existing =
                    await _client
                        .Child("preAssignments")
                        .OnceAsync<PreAssignment>();

                bool alreadyPending =
                    existing.Any(x =>
                        x.Object != null &&
                        x.Object.ToolId ==
                        assignment.ToolId &&
                        x.Object.Status ==
                        "Pending");

                if (alreadyPending)
                    return false;

                assignment.Status =
                    "Pending";

                assignment.DateCreated =
                    DateTime.Now;

                await _client
                    .Child("preAssignments")
                    .PostAsync(assignment);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"CreatePreAssignmentAsync error: " +
                    $"{ex.Message}");

                return false;
            }
        }

        public async Task<bool>
            BorrowToolForProjectAsync(
                string toolId,
                string toolName,
                string workerId,
                string workerName,
                string projectId,
                string projectName,
                string assignedById,
                string assignedByName)
        {
            try
            {
                var tool =
                    await GetToolByIdAsync(
                        toolId);

                if (tool == null ||
                    tool.Status != "Available")
                {
                    return false;
                }

                tool.Status =
                    "Borrowed";

                tool.AssignedWorkerId =
                    workerId;

                tool.AssignedWorkerName =
                    workerName;

                tool.BorrowDate =
                    DateTime.Now;

                tool.BorrowedProjectId =
                    projectId;

                tool.BorrowedProjectName =
                    projectName;

                tool.AssignedById =
                    assignedById;

                tool.AssignedByName =
                    assignedByName;

                var updated =
                    await UpdateToolAsync(
                        tool);

                if (!updated)
                    return false;

                // Direct distribution is performed by the PE.
                await LogTransactionAsync(
                    new TransactionLog
                    {
                        ToolId =
                            toolId,

                        ToolName =
                            toolName,

                        WorkerId =
                            workerId,

                        WorkerName =
                            workerName,

                        ProjectId =
                            projectId,

                        ProjectName =
                            projectName,

                        PerformedById =
                            assignedById,

                        PerformedByName =
                            assignedByName,

                        Action =
                            "Borrowed",

                        Description =
                            $"Equipment assigned to " +
                            $"{workerName} by " +
                            $"{assignedByName}.",

                        Condition =
                            tool.Condition,

                        Date =
                            DateTime.Now
                    });

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"BorrowToolForProjectAsync error: " +
                    $"{ex.Message}");

                return false;
            }
        }

        public async Task<List<PreAssignmentResult>>
            GetPendingAssignmentsForWorkerAsync(
                string workerId)
        {
            try
            {
                var result =
                    await _client
                        .Child("preAssignments")
                        .OnceAsync<PreAssignment>();

                if (result == null)
                {
                    return new List<
                        PreAssignmentResult>();
                }

                return result
                    .Where(r =>
                        r.Object != null &&
                        r.Object.WorkerId ==
                        workerId &&
                        r.Object.Status ==
                        "Pending")
                    .Select(r =>
                        new PreAssignmentResult
                        {
                            Key =
                                r.Key,

                            Assignment =
                                r.Object
                        })
                    .OrderByDescending(r =>
                        r.Assignment.DateCreated)
                    .ToList();
            }
            catch
            {
                return new List<
                    PreAssignmentResult>();
            }
        }

        public async Task<bool>
            ConfirmAssignmentAsync(
                string assignmentKey,
                PreAssignment assignment)
        {
            try
            {
                var tool =
                    await GetToolByIdAsync(
                        assignment.ToolId);

                if (tool == null)
                    return false;

                if (tool.Status != "Available")
                    return false;

                // Worker becomes responsible custodian.
                tool.AssignedWorkerId =
                    assignment.WorkerId;

                tool.AssignedWorkerName =
                    assignment.WorkerName;

                // Project assignment.
                tool.BorrowedProjectId =
                    assignment.ProjectId;

                tool.BorrowedProjectName =
                    assignment.ProjectName;

                // PE who originally assigned it.
                tool.AssignedById =
                    assignment.AssignedById;

                tool.AssignedByName =
                    assignment.AssignedByName;

                tool.BorrowDate =
                    DateTime.Now;

                tool.Status =
                    "Borrowed";

                tool.PreAssignedWorkerId =
                    string.Empty;

                tool.PreAssignedWorkerName =
                    string.Empty;

                var updated =
                    await UpdateToolAsync(
                        tool);

                if (!updated)
                    return false;

                // Worker performed the confirmation.
                await LogTransactionAsync(
                    new TransactionLog
                    {
                        ToolId =
                            tool.ToolId,

                        ToolName =
                            tool.ToolName,

                        WorkerId =
                            assignment.WorkerId,

                        WorkerName =
                            assignment.WorkerName,

                        ProjectId =
                            assignment.ProjectId,

                        ProjectName =
                            assignment.ProjectName,

                        PerformedById =
                            assignment.WorkerId,

                        PerformedByName =
                            assignment.WorkerName,

                        Action =
                            "Borrowed",

                        Description =
                            $"Equipment assigned by " +
                            $"{assignment.AssignedByName} " +
                            $"and accepted by " +
                            $"{assignment.WorkerName}.",

                        Condition =
                            tool.Condition,

                        Date =
                            DateTime.Now
                    });

                assignment.Status =
                    "Accepted";

                await _client
                    .Child("preAssignments")
                    .Child(assignmentKey)
                    .PutAsync(assignment);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"ConfirmAssignmentAsync error: " +
                    $"{ex.Message}");

                return false;
            }
        }

        public async Task<bool>
            DeclineAssignmentAsync(
                string assignmentKey,
                PreAssignment assignment)
        {
            try
            {
                assignment.Status =
                    "Declined";

                await _client
                    .Child("preAssignments")
                    .Child(assignmentKey)
                    .PutAsync(assignment);

                return true;
            }
            catch
            {
                return false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // PROJECT LOOKUP BY WORKER
        // ─────────────────────────────────────────────────────────

        public async Task<Project?>
            GetProjectForWorkerAsync(
                string workerId)
        {
            try
            {
                var projects =
                    await GetAllProjectsAsync();

                var candidates =
                    projects
                        .Where(p =>
                            p.Status != "Completed")
                        .ToList();

                Project? found =
                    null;

                foreach (var project in candidates)
                {
                    var workerKeys =
                        await GetProjectWorkerKeysAsync(
                            project.ProjectId);

                    if (!workerKeys.Contains(
                            workerId))
                    {
                        continue;
                    }

                    if (project.Status ==
                        "Active")
                    {
                        return project;
                    }

                    found ??=
                        project;
                }

                return found;
            }
            catch
            {
                return null;
            }
        }

        // ─────────────────────────────────────────────────────────
        // PROJECT EQUIPMENT REQUIREMENTS
        // ─────────────────────────────────────────────────────────

        public async Task<bool>
            SetProjectEquipmentRequirementAsync(
                string projectId,
                string catalogId,
                string catalogName,
                int quantity)
        {
            try
            {
                await _client
                    .Child("projectEquipment")
                    .Child(projectId)
                    .Child(catalogId)
                    .PutAsync(
                        new ProjectEquipmentRequirement
                        {
                            ProjectId =
                                projectId,

                            CatalogId =
                                catalogId,

                            CatalogName =
                                catalogName,

                            QuantityNeeded =
                                quantity
                        });

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<
            List<ProjectEquipmentRequirement>>
            GetProjectEquipmentRequirementsAsync(
                string projectId)
        {
            try
            {
                var result =
                    await _client
                        .Child("projectEquipment")
                        .Child(projectId)
                        .OnceAsync<
                            ProjectEquipmentRequirement>();

                return result?
                    .Where(r =>
                        r.Object != null)
                    .Select(r =>
                        r.Object)
                    .ToList()
                    ?? new List<
                        ProjectEquipmentRequirement>();
            }
            catch
            {
                return new List<
                    ProjectEquipmentRequirement>();
            }
        }

        public async Task<
            List<ProjectEquipmentRequirement>>
            GetAllActiveProjectEquipmentRequirementsAsync()
        {
            try
            {
                var projects =
                    await GetAllProjectsAsync();

                var activeProjects =
                    projects
                        .Where(p =>
                            p.Status != "Completed")
                        .ToList();

                var requirements =
                    new List<
                        ProjectEquipmentRequirement>();

                foreach (var project in
                    activeProjects)
                {
                    var projectRequirements =
                        await GetProjectEquipmentRequirementsAsync(
                            project.ProjectId);

                    requirements.AddRange(
                        projectRequirements);
                }

                return requirements;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"GetAllActiveProjectEquipmentRequirementsAsync " +
                    $"error: {ex.Message}");

                return new List<
                    ProjectEquipmentRequirement>();
            }
        }

        public async Task<bool>
            RemoveProjectEquipmentRequirementAsync(
                string projectId,
                string catalogId)
        {
            try
            {
                await _client
                    .Child("projectEquipment")
                    .Child(projectId)
                    .Child(catalogId)
                    .DeleteAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}