using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace MultiFuelMaster.Models
{
    public class UserRole
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string? Description { get; set; }
        
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        /// <summary>
        /// JSON string with permissions: key -> access level
        /// </summary>
        public string? PermissionsJson { get; set; }

        /// <summary>
        /// System role flag (cannot be deleted)
        /// </summary>
        public bool IsSystem { get; set; } = false;

        /// <summary>
        /// Get permissions dictionary from JSON
        /// </summary>
        public Dictionary<string, PermissionLevel> GetPermissions()
        {
            if (string.IsNullOrEmpty(PermissionsJson))
                return new Dictionary<string, PermissionLevel>();

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, int>>(PermissionsJson)
                    .ToDictionary(k => k.Key, v => (PermissionLevel)v.Value);
            }
            catch
            {
                return new Dictionary<string, PermissionLevel>();
            }
        }

        /// <summary>
        /// Set permissions from dictionary
        /// </summary>
        public void SetPermissions(Dictionary<string, PermissionLevel> permissions)
        {
            var intDict = permissions.ToDictionary(k => k.Key, v => (int)v.Value);
            PermissionsJson = JsonSerializer.Serialize(intDict);
        }

        /// <summary>
        /// Get access level for specific permission
        /// </summary>
        public PermissionLevel GetPermission(string key)
        {
            var permissions = GetPermissions();
            return permissions.TryGetValue(key, out var level) ? level : PermissionLevel.NotSet;
        }

        /// <summary>
        /// Set access level for specific permission
        /// </summary>
        public void SetPermission(string key, PermissionLevel level)
        {
            var permissions = GetPermissions();
            permissions[key] = level;
            SetPermissions(permissions);
        }
    }
}
