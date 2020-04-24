﻿using Etch.OrchardCore.ContentPermissions.Models;
using Microsoft.AspNetCore.Http;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.Security.Services;
using System;
using System.Linq;

namespace Module.Services {
   public class ContentPermissionsService : IContentPermissionsService {
      #region Dependencies

      private readonly IContentDefinitionManager _contentDefinitionManager;
      private readonly IHttpContextAccessor _httpContextAccessor;

      #endregion

      #region Constructor

      public ContentPermissionsService(IContentDefinitionManager contentDefinitionManager, IHttpContextAccessor httpContextAccessor, IRoleService roleService) {
         _contentDefinitionManager = contentDefinitionManager;
         _httpContextAccessor = httpContextAccessor;
      }

      #endregion

      #region Helpers

      public bool CanAccess(ContentPermissionsPart part) {

         if (part.Roles.Contains("Anonymous")) {
            return true;
         }

         if (_httpContextAccessor.HttpContext.User == null) {
            return false;
         }

         if (part.Roles.Contains("Authenticated") && _httpContextAccessor.HttpContext.User.Identity.IsAuthenticated) {
            return true;
         }

         foreach (var role in part.Roles) {
            if (_httpContextAccessor.HttpContext.User.IsInRole(role)) {
               return true;
            }
         }

         return false;
      }

      public ContentPermissionsPartSettings GetSettings(ContentPermissionsPart part) {
         var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(part.ContentItem.ContentType);
         var contentTypePartDefinition = contentTypeDefinition.Parts.FirstOrDefault(x => string.Equals(x.PartDefinition.Name, nameof(ContentPermissionsPart)));
         return contentTypePartDefinition.GetSettings<ContentPermissionsPartSettings>();
      }

      #endregion

   }

   public interface IContentPermissionsService {

      bool CanAccess(ContentPermissionsPart part);

      ContentPermissionsPartSettings GetSettings(ContentPermissionsPart part);

   }
}
