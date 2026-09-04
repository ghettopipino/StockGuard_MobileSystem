using System;
using System.Linq;
using System.Threading.Tasks;
using StockGuard.Models;

namespace StockGuard.Services
{
    /// <summary>
    /// Handles distribution of an equipment item that has already
    /// been borrowed from the office by the Project Engineer.
    ///
    /// IMPORTANT:
    /// Available office tools cannot be distributed directly.
    ///
    /// Flow:
    /// Available
    ///     -> PE borrows into project
    /// Borrowed / PE accountable
    ///     -> PE creates PreAssignment
    /// Borrowed / PE accountable
    ///     -> Worker accepts
    /// Borrowed / Worker accountable
    /// </summary>
    public static class WorkerAssignmentHelper
    {
        public static async Task<bool>
            AssignToolToWorkerViaPickerAsync(
                FirebaseService firebase,
                AuthService auth,
                Tool tool,
                string projectId)
        {
            if (tool == null)
                return false;


            // ─────────────────────────────────────────────
            // TOOL MUST ALREADY BE BORROWED
            // ─────────────────────────────────────────────

            if (!string.Equals(
                    tool.Status,
                    "Borrowed",
                    StringComparison.OrdinalIgnoreCase))
            {
                await Shell.Current.DisplayAlert(
                    "Borrow Equipment First",
                    $"{tool.ToolName} ({tool.ToolId}) has not yet " +
                    $"been borrowed into the project.\n\n" +
                    "Borrow the physical equipment from Project Details first.",
                    "OK");

                return false;
            }


            // ─────────────────────────────────────────────
            // PROJECT CONTEXT
            // ─────────────────────────────────────────────

            if (string.IsNullOrWhiteSpace(projectId))
            {
                await Shell.Current.DisplayAlert(
                    "No Project",
                    "This action requires a project.",
                    "OK");

                return false;
            }


            if (!string.Equals(
                    tool.BorrowedProjectId,
                    projectId,
                    StringComparison.OrdinalIgnoreCase))
            {
                await Shell.Current.DisplayAlert(
                    "Wrong Project",
                    $"{tool.ToolName} ({tool.ToolId}) is borrowed " +
                    "under a different project.",
                    "OK");

                return false;
            }


            // ─────────────────────────────────────────────
            // TOOL MUST STILL BE WITH PE
            // ─────────────────────────────────────────────

            if (!string.IsNullOrWhiteSpace(
                    tool.AssignedWorkerId))
            {
                await Shell.Current.DisplayAlert(
                    "Already Distributed",
                    $"{tool.ToolName} ({tool.ToolId}) is already " +
                    $"assigned to {tool.AssignedWorkerName}.",
                    "OK");

                return false;
            }


            // ─────────────────────────────────────────────
            // PROJECT
            // ─────────────────────────────────────────────

            var projects =
                await firebase.GetAllProjectsAsync();

            var project =
                projects.FirstOrDefault(p =>
                    string.Equals(
                        p.ProjectId,
                        projectId,
                        StringComparison.OrdinalIgnoreCase));

            if (project == null ||
                project.Status == "Completed")
            {
                await Shell.Current.DisplayAlert(
                    "Invalid Project",
                    "This project is no longer active.",
                    "OK");

                return false;
            }


            // ─────────────────────────────────────────────
            // PROJECT WORKERS
            // ─────────────────────────────────────────────

            var workerKeys =
                await firebase
                    .GetProjectWorkerKeysAsync(
                        projectId);

            var allUsers =
                await firebase
                    .GetAllUsersAsync();

            var workers =
                allUsers
                    .Where(u =>
                        string.Equals(
                            u.Role,
                            "Worker",
                            StringComparison.OrdinalIgnoreCase) &&

                        string.Equals(
                            u.AccountStatus,
                            "Approved",
                            StringComparison.OrdinalIgnoreCase) &&

                        workerKeys.Any(key =>
                            string.Equals(
                                key,
                                u.UniqueKey,
                                StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(u => u.FullName)
                    .ToList();

            if (workers.Count == 0)
            {
                await Shell.Current.DisplayAlert(
                    "No Workers on Project",
                    $"{project.ProjectName} has no workers assigned yet.\n\n" +
                    "Assign workers to the project first.",
                    "OK");

                return false;
            }


            // ─────────────────────────────────────────────
            // SELECT WORKER
            // ─────────────────────────────────────────────

            var workerNames =
                workers
                    .Select(w => w.FullName)
                    .ToArray();

            var selectedWorkerName =
                await Shell.Current.DisplayActionSheet(
                    $"Distribute {tool.ToolName} ({tool.ToolId})",
                    "Cancel",
                    null,
                    workerNames);

            if (string.IsNullOrWhiteSpace(
                    selectedWorkerName) ||
                selectedWorkerName == "Cancel")
            {
                return false;
            }

            var worker =
                workers.FirstOrDefault(w =>
                    string.Equals(
                        w.FullName,
                        selectedWorkerName,
                        StringComparison.OrdinalIgnoreCase));

            if (worker == null)
                return false;


            // ─────────────────────────────────────────────
            // CURRENT PE
            // ─────────────────────────────────────────────

            var currentPE =
                auth.CurrentUser;

            if (currentPE == null)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    "Project Engineer could not be identified.",
                    "OK");

                return false;
            }


            // ─────────────────────────────────────────────
            // CREATE PRE-ASSIGNMENT
            // ─────────────────────────────────────────────

            var assignment =
                new PreAssignment
                {
                    ToolId =
                        tool.ToolId,

                    ToolName =
                        tool.ToolName,

                    WorkerId =
                        worker.UniqueKey,

                    WorkerName =
                        worker.FullName,

                    ProjectId =
                        project.ProjectId,

                    ProjectName =
                        project.ProjectName,

                    AssignedById =
                        currentPE.UniqueKey,

                    AssignedByName =
                        currentPE.FullName,

                    Status =
                        "Pending",

                    DateCreated =
                        DateTime.Now
                };

            bool success =
                await firebase
                    .CreatePreAssignmentAsync(
                        assignment);

            if (!success)
            {
                await Shell.Current.DisplayAlert(
                    "Could Not Distribute",
                    $"{tool.ToolName} ({tool.ToolId}) could not " +
                    "be distributed.\n\n" +
                    "It may already have a pending assignment.",
                    "OK");

                return false;
            }


            await Shell.Current.DisplayAlert(
                "Distribution Sent",
                $"{tool.ToolName} ({tool.ToolId}) was sent to " +
                $"{worker.FullName}.\n\n" +
                "The Project Engineer remains accountable until " +
                "the worker confirms receipt.",
                "OK");

            return true;
        }
    }
}