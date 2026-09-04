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
        public async Task<string> CreateBorrowRequestAsync(
    BorrowRequest request)
        {
            try
            {
                // ─────────────────────────────────────────────
                // CHECK EXISTING REQUESTS
                // ─────────────────────────────────────────────

                var existingRequests =
                    await _client
                        .Child("borrowRequests")
                        .OnceAsync<BorrowRequest>();

                var duplicate =
                    existingRequests.Any(x =>
                        x.Object != null &&

                        string.Equals(
                            x.Object.ToolId?.Trim(),
                            request.ToolId?.Trim(),
                            StringComparison.OrdinalIgnoreCase) &&

                        string.Equals(
                            x.Object.RequesterId?.Trim(),
                            request.RequesterId?.Trim(),
                            StringComparison.OrdinalIgnoreCase) &&

                        string.Equals(
                            x.Object.Status?.Trim(),
                            "Pending",
                            StringComparison.OrdinalIgnoreCase));

                // Already has a pending request
                if (duplicate)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Duplicate borrow request blocked: " +
                        $"{request.RequesterId} / {request.ToolId}");

                    return "DUPLICATE";
                }

                // ─────────────────────────────────────────────
                // CREATE NEW REQUEST
                // ─────────────────────────────────────────────

                var result =
                    await _client
                        .Child("borrowRequests")
                        .PostAsync(request);

                var key = result.Key;

                if (string.IsNullOrWhiteSpace(key))
                    return string.Empty;

                request.RequestId = key;

                await _client
                    .Child("borrowRequests")
                    .Child(key)
                    .PutAsync(request);

                return key;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"CreateBorrowRequestAsync error: {ex.Message}");

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
                    return new List<BorrowRequestResult>();
                }

                var requests =
                    new List<BorrowRequestResult>();

                foreach (var item in result)
                {
                    if (item.Object == null)
                        continue;

                    var request =
                        item.Object;

                    if (string.IsNullOrWhiteSpace(
                            request.RequestId))
                    {
                        request.RequestId =
                            item.Key;
                    }

                    requests.Add(
                        new BorrowRequestResult
                        {
                            Key =
                                item.Key,

                            Request =
                                request
                        });
                }

                return requests
                    .OrderByDescending(r =>
                        r.Request.RequestDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"GetAllBorrowRequestsRawAsync error: {ex.Message}");

                return new List<BorrowRequestResult>();
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
        // LOST / MISSING REPORTS
        // ─────────────────────────────────────────────────────────

        public async Task<string>
            SubmitLostReportAsync(
                LostReport report)
        {
            try
            {
                // Prevent more than one active
                // Missing/Lost report for the same tool.
                var existing =
                    await _client
                        .Child("lostReports")
                        .OnceAsync<LostReport>();

                bool duplicate =
                    existing.Any(x =>
                        x.Object != null &&

                        string.Equals(
                            x.Object.ToolId?.Trim(),
                            report.ToolId?.Trim(),
                            StringComparison.OrdinalIgnoreCase) &&

                        (
                            x.Object.Status == "Pending" ||
                            x.Object.Status == "Lost"
                        ));

                if (duplicate)
                {
                    return "DUPLICATE";
                }

                var result =
                    await _client
                        .Child("lostReports")
                        .PostAsync(report);

                var key =
                    result.Key;

                if (string.IsNullOrWhiteSpace(
                        key))
                {
                    return string.Empty;
                }

                report.ReportId =
                    key;

                await _client
                    .Child("lostReports")
                    .Child(key)
                    .PutAsync(report);

                return key;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"SubmitLostReportAsync error: {ex.Message}");

                return string.Empty;
            }
        }


        public async Task<List<LostReportResult>>
            GetAllLostReportsRawAsync()
        {
            try
            {
                var result =
                    await _client
                        .Child("lostReports")
                        .OnceAsync<LostReport>();

                if (result == null)
                {
                    return new List<
                        LostReportResult>();
                }

                var reports =
                    new List<
                        LostReportResult>();

                foreach (var item in result)
                {
                    if (item.Object == null)
                        continue;

                    var report =
                        item.Object;

                    if (string.IsNullOrWhiteSpace(
                            report.ReportId))
                    {
                        report.ReportId =
                            item.Key;
                    }

                    reports.Add(
                        new LostReportResult
                        {
                            Key =
                                item.Key,

                            Report =
                                report
                        });
                }

                return reports
                    .OrderByDescending(r =>
                        r.Report.ReportDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"GetAllLostReportsRawAsync error: {ex.Message}");

                return new List<
                    LostReportResult>();
            }
        }


        public async Task<List<LostReport>>
            GetAllLostReportsAsync()
        {
            try
            {
                var result =
                    await _client
                        .Child("lostReports")
                        .OnceAsync<LostReport>();

                if (result == null)
                {
                    return new List<
                        LostReport>();
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"GetAllLostReportsAsync error: {ex.Message}");

                return new List<
                    LostReport>();
            }
        }


        public async Task<bool>
            UpdateLostReportAsync(
                string key,
                LostReport report)
        {
            try
            {
                await _client
                    .Child("lostReports")
                    .Child(key)
                    .PutAsync(report);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"UpdateLostReportAsync error: {ex.Message}");

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


        public async Task<string>
    BorrowToolIntoProjectAsync(
        string toolId,
        string projectId,
        string projectEngineerId,
        string projectEngineerName)
        {
            try
            {
                // ─────────────────────────────────────────────
                // VALIDATE PROJECT
                // ─────────────────────────────────────────────

                var projects =
                    await GetAllProjectsAsync();

                var project =
                    projects.FirstOrDefault(p =>
                        string.Equals(
                            p.ProjectId?.Trim(),
                            projectId?.Trim(),
                            StringComparison.OrdinalIgnoreCase));

                if (project == null ||
                    project.Status == "Completed")
                {
                    return "INVALID_PROJECT";
                }


                // ─────────────────────────────────────────────
                // FIND TOOL
                // ─────────────────────────────────────────────

                var tool =
                    await GetToolByIdAsync(
                        toolId);

                if (tool == null)
                {
                    return "NOT_FOUND";
                }


                // ─────────────────────────────────────────────
                // TOOL MUST BE AVAILABLE
                // ─────────────────────────────────────────────

                if (!string.Equals(
                        tool.Status,
                        "Available",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return "NOT_AVAILABLE";
                }


                // ─────────────────────────────────────────────
                // EQUIPMENT MUST BE REQUIRED BY PROJECT
                // ─────────────────────────────────────────────

                var requirements =
                    await GetProjectEquipmentRequirementsAsync(
                        projectId);

                var requirement =
                    requirements.FirstOrDefault(r =>
                        string.Equals(
                            r.CatalogId,
                            tool.CatalogId,
                            StringComparison.OrdinalIgnoreCase));

                if (requirement == null)
                {
                    return "NOT_REQUIRED";
                }


                // ─────────────────────────────────────────────
                // CHECK REQUIREMENT LIMIT
                // ─────────────────────────────────────────────

                var allTools =
                    await GetAllToolsAsync(
                        forceRefresh: true);

                int currentlyBorrowed =
                    allTools.Count(t =>
                        string.Equals(
                            t.CatalogId,
                            tool.CatalogId,
                            StringComparison.OrdinalIgnoreCase) &&

                        string.Equals(
                            t.BorrowedProjectId,
                            projectId,
                            StringComparison.OrdinalIgnoreCase));

                if (currentlyBorrowed >=
                    requirement.QuantityNeeded)
                {
                    return "REQUIREMENT_FULFILLED";
                }


                // ─────────────────────────────────────────────
                // PE BORROWS THE PHYSICAL TOOL
                // ─────────────────────────────────────────────

                tool.Status =
                    "Borrowed";

                tool.BorrowedProjectId =
                    project.ProjectId;

                tool.BorrowedProjectName =
                    project.ProjectName;

                // No Worker yet.
                tool.AssignedWorkerId =
                    string.Empty;

                tool.AssignedWorkerName =
                    string.Empty;

                // PE who borrowed the equipment.
                tool.AssignedById =
                    projectEngineerId;

                tool.AssignedByName =
                    projectEngineerName;

                tool.BorrowDate =
                    DateTime.Now;

                tool.PreAssignedWorkerId =
                    string.Empty;

                tool.PreAssignedWorkerName =
                    string.Empty;

                // Reset old check-in data.
                tool.LastCheckInLocation =
                    string.Empty;

                tool.LastCheckInDate =
                    null;

                tool.IsCheckInPending =
                    false;

                tool.LastCheckInVerifiedById =
                    string.Empty;

                tool.LastCheckInVerifiedByName =
                    string.Empty;


                bool updated =
                    await UpdateToolAsync(
                        tool);

                if (!updated)
                {
                    return "ERROR";
                }


                // Keep projectTools in sync.
                await DeployToolToProjectAsync(
                    project.ProjectId,
                    tool.ToolId);


                // ─────────────────────────────────────────────
                // TRANSACTION
                // ─────────────────────────────────────────────

                await LogTransactionAsync(
                    new TransactionLog
                    {
                        ToolId =
                            tool.ToolId,

                        ToolName =
                            tool.ToolName,

                        WorkerId =
                            string.Empty,

                        WorkerName =
                            string.Empty,

                        ProjectId =
                            project.ProjectId,

                        ProjectName =
                            project.ProjectName,

                        PerformedById =
                            projectEngineerId,

                        PerformedByName =
                            projectEngineerName,

                        Action =
                            "Borrowed",

                        Description =
                            $"{projectEngineerName} borrowed " +
                            $"{tool.ToolName} ({tool.ToolId}) " +
                            $"from the office for {project.ProjectName}.",

                        Condition =
                            tool.Condition,

                        Date =
                            DateTime.Now
                    });


                return "SUCCESS";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"BorrowToolIntoProjectAsync error: " +
                    $"{ex.Message}");

                return "ERROR";
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
                    "lostReports",
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

                if (tool == null)
                    return false;


                // Tool must already be borrowed from office.
                if (!string.Equals(
                        tool.Status,
                        "Borrowed",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }


                // Tool must belong to this same project.
                if (!string.Equals(
                        tool.BorrowedProjectId,
                        assignment.ProjectId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }


                // Tool must still be under PE accountability.
                if (!string.IsNullOrWhiteSpace(
                        tool.AssignedWorkerId))
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

                        string.Equals(
                            x.Object.ToolId,
                            assignment.ToolId,
                            StringComparison.OrdinalIgnoreCase) &&

                        string.Equals(
                            x.Object.Status,
                            "Pending",
                            StringComparison.OrdinalIgnoreCase));

                if (alreadyPending)
                    return false;


                assignment.Status =
                    "Pending";

                assignment.DateCreated =
                    DateTime.Now;


                await _client
                    .Child("preAssignments")
                    .PostAsync(
                        assignment);


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


                // Tool must already be borrowed by the PE.
                if (!string.Equals(
                        tool.Status,
                        "Borrowed",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }


                // Must still belong to the same project.
                if (!string.Equals(
                        tool.BorrowedProjectId,
                        assignment.ProjectId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }


                // Another worker must not already have it.
                if (!string.IsNullOrWhiteSpace(
                        tool.AssignedWorkerId))
                {
                    return false;
                }


                // ─────────────────────────────────────────────
                // ACCOUNTABILITY PE → WORKER
                // ─────────────────────────────────────────────

                tool.AssignedWorkerId =
                    assignment.WorkerId;

                tool.AssignedWorkerName =
                    assignment.WorkerName;

                tool.BorrowedProjectId =
                    assignment.ProjectId;

                tool.BorrowedProjectName =
                    assignment.ProjectName;

                tool.AssignedById =
                    assignment.AssignedById;

                tool.AssignedByName =
                    assignment.AssignedByName;


                // BorrowDate was already established when
                // the PE borrowed it from the office.
                tool.BorrowDate ??=
                    DateTime.Now;


                // Status DOES NOT change.
                tool.Status =
                    "Borrowed";


                tool.PreAssignedWorkerId =
                    string.Empty;

                tool.PreAssignedWorkerName =
                    string.Empty;


                bool updated =
                    await UpdateToolAsync(
                        tool);

                if (!updated)
                    return false;


                // ─────────────────────────────────────────────
                // TRANSACTION
                // ─────────────────────────────────────────────

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
                            "Assignment Accepted",

                        Description =
                            $"{assignment.WorkerName} accepted " +
                            $"{tool.ToolName} ({tool.ToolId}) from " +
                            $"{assignment.AssignedByName}. " +
                            "Accountability transferred to the worker.",

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
                    .PutAsync(
                        assignment);


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