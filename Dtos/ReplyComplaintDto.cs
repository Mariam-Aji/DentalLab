using System.ComponentModel.DataAnnotations;

namespace DentalLab.Api.Dtos.Complaints
{
    public class ReplyComplaintDto
    {
        [Required(ErrorMessage = "نص الرد مطلوب.")]
        public string ReplyText { get; set; } = null!;
    }
}