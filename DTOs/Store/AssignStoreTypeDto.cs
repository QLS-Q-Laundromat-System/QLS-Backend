using System;
using System.ComponentModel.DataAnnotations;

namespace QLS.Backend.DTOs.Store
{
    public class AssignStoreTypeDto
    {
        [Required(ErrorMessage = "StoreTypeId không được để trống")]
        public Guid StoreTypeId { get; set; }
    }
}
