using Microsoft.EntityFrameworkCore;

namespace QLS.Backend.Models;

[Index(nameof(PriceListId), nameof(StoreTypeId), IsUnique = true)]
public class PriceListStoreType
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required Guid PriceListId { get; set; }
    public PriceList? PriceList { get; set; }

    public required Guid StoreTypeId { get; set; }
    public StoreType? StoreType { get; set; }

    public int? OverridePriority { get; set; } 
}
