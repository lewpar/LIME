using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations.Schema;

namespace LIME.Database.Tables;

[Table("agents")]
[PrimaryKey(nameof(Id))]
public class Agents
{
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    public string? Name { get; set; }

    [Column("address")]
    public string? Address { get; set; }

    [Column("port")]
    public string? Port { get; set; }

    [Column("activated")]
    public bool Activated { get; set; }
}
