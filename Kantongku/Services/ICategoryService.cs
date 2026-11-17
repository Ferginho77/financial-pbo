using Kantongku.Models.ViewModels;

namespace Kantongku.Services;

public interface ICategoryService
{
    Task<List<ExpenseGroupViewModel>> GetGroupsAsync(string userId);
    Task SeedDefaultsAsync(string userId);

    Task<ServiceResult> CreateGroupAsync(string userId, string name);
    Task<ServiceResult> RenameGroupAsync(string userId, int groupId, string name);
    Task<ServiceResult> DeleteGroupAsync(string userId, int groupId);

    Task<ServiceResult> CreateCategoryAsync(string userId, int groupId, string name);
    Task<ServiceResult> RenameCategoryAsync(string userId, int categoryId, string name);
    Task<ServiceResult> DeleteCategoryAsync(string userId, int categoryId);
}
