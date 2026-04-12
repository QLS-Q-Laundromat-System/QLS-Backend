using System.ComponentModel.DataAnnotations;
using QLS.Backend.Models.Enums;

namespace QLS.Backend.Models;

public class PriceList
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(50)]
    public required string Code { get; set; }

    [MaxLength(200)]
    public required string Name { get; set; }

    public string? Description { get; set; }

    public required DateOnly ValidFrom { get; set; } 
    public DateOnly? ValidTo { get; set; }

    public int Priority { get; set; } = 0;

    public PriceListStatus Status { get; set; } = PriceListStatus.Draft;

    public Currency Currency { get; set; } = Currency.VND;

    public Guid? CreatedBy { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<PriceListStoreType> PriceListStoreTypes { get; set; } = new List<PriceListStoreType>();
    public ICollection<PriceModePerKg> PriceModePerKgs { get; set; } = new List<PriceModePerKg>();
    public ICollection<PriceModePerSession> PriceModePerSessions { get; set; } = new List<PriceModePerSession>();
}
