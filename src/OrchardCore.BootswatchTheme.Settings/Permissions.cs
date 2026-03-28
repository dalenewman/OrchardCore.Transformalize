using OrchardCore.Security.Permissions;

namespace OrchardCore.BootswatchTheme.Settings {
   public class Permissions : IPermissionProvider {

      public static readonly Permission ManageBootswatchSettings = new Permission(
         nameof(ManageBootswatchSettings), "Manage Bootswatch Settings");

      public Task<IEnumerable<Permission>> GetPermissionsAsync() =>
         Task.FromResult(new[] { ManageBootswatchSettings }.AsEnumerable());

      public IEnumerable<PermissionStereotype> GetDefaultStereotypes() =>
         new[] {
            new PermissionStereotype {
               Name = "Administrator",
               Permissions = new[] { ManageBootswatchSettings }
            }
         };
   }
}
