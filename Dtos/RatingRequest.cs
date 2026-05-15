namespace DentalLab.Api.Dtos
{
    public class RatingRequest
    {
        public int LabId { get; set; }
        public int Quality { get; set; }    
        public int TimeScore { get; set; }
    }
}
