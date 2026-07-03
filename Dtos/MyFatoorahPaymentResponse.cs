namespace DentalLab.Api.Dtos
{
    public class MyFatoorahPaymentResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public MyFatoorahData Data { get; set; } = new();
    }
}
