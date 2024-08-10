using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations.Schema;

namespace LIME.Dashboard.Database.Models;

[Table("agents_pending")]
[PrimaryKey(nameof(Id))]
public class AgentPending
{
    [Column("id")]
    public int Id { get; set; }

    [Column("secret")]
    public string? Secret { get; set; }

    [Column("date_created")]
    public DateTime CreatedDate { get; set; }

    [Column("date_expire")]
    public DateTime ExpireDate { get; set; }
}
