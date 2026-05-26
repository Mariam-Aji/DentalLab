using DentalLab.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace DentalLab.Api.Dtos
{
    public class LabDto
    {
        public int Id { get; set; }
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty; 
    }
}
