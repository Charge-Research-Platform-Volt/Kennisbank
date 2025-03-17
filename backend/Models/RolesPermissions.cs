using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace KnowledgeBank.Models;

[Table("roles")]
public class Role
{
    [Column("id")]
    [Key]
    public required int Id { get; set; }

    [Column("name")]
    [MaxLength(50)]
    public required string Name { get; set; }

    [Column("description")]
    [MaxLength(255)]
    public required string Description { get; set; }

    // Navigation properties
    public ICollection<UserRole> UserRoles { get; set; }
    public ICollection<RolePermission> RolePermissions { get; set; }
}

[Table("permissions")]
public class Permission
{
    [Column("id")]
    [Key]
    public required int Id { get; set; }

    [Column("name")]
    [MaxLength(50)]
    public required string Name { get; set; }

    [Column("description")]
    [MaxLength(255)]
    public required string Description { get; set; }

    // Navigation properties
    public ICollection<RolePermission> RolePermissions { get; set; }
}

[Table("user-role")]
public class UserRole
{
    [Column("user-id")]
    public int UserId { get; set; }

    [Column("role-id")]
    public int RoleId { get; set; }

    // Navigation properties
    public User User { get; set; }
    public Role Role { get; set; }
}

[Table("role-permission")]
public class RolePermission
{
    [Column("role-id")]
    public int RoleId { get; set; }

    [Column("permission-id")]
    public int PermissionId { get; set; }

    // Navigation properties
    public Role Role { get; set; }
    public Permission Permission { get; set; }
}