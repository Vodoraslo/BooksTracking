using BooksTracking.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace BooksTracking.Models
{
    public class UserBook
    {
        public int Id { get; set; } // Първичен ключ

        // Свързване с потребителя
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public IdentityUser User { get; set; }

        // Свързване с книгата
        public int BookId { get; set; }
        [ForeignKey("BookId")]
        public Book Book { get; set; }

        // Статус на книгата
        public string Status { get; set; }
    }
}
