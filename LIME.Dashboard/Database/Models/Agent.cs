namespace LIME.Dashboard.Database.Models;

using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations.Schema;

[Table("agents")]
[PrimaryKey(nameof(Id))]
public class Agent
{
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    public string? Name { get; set; }

    [Column("address")]
    public string? Address { get; set; }

    [Column("activated")]
    public bool Activated { get; set; }
}

