using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations.Schema;

namespace LIME.Mediator.Database.Models;

[Table("agents")]
[PrimaryKey(nameof(Id))]
public class Agent
{
    [Column("id")]
    public int Id { get; set; }

    [Column("guid")]
    public Guid Guid { get; set; }

    [Column("name")]
    public string? Name { get; set; }

    [Column("address")]
    public string? Address { get; set; }

    [Column("status")]
    public AgentStatus Status { get; set; }
}

