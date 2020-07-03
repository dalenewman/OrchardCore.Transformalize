using OrchardCore.Security.Permissions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TransformalizeModule {

   public class Permissions : IPermissionProvider {

      public static readonly Permission ManageTransformalizeSettings = new Permission(
          nameof(ManageTransformalizeSettings),
          "Manage Transformalize Settings"
      );


      public Task<IEnumerable<Permission>> GetPermissionsAsync() =>
          Task.FromResult(new[] {
                ManageTransformalizeSettings
          }
          .AsEnumerable());

      public IEnumerable<PermissionStereotype> GetDefaultStereotypes() =>
          new[] {
               new PermissionStereotype {
                  Name = "Administrator",
                  Permissions = new[] { ManageTransformalizeSettings }
               }
          };
   }
}