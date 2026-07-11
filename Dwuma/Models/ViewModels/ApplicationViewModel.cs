using Dwuma.Models.Data.DwumaContext;


namespace Dwuma.Models.ViewModels
{
        public class UploadCvRequest
        {
            public IFormFile File { get; set; }
            public int UserId { get; set; }
        }

   
}
