using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;

namespace BooksTracking.Models
{
    public class Book
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(100)]
        public string Author { get; set; }

        [StringLength(100)]
        public string Genre { get; set; }

        [StringLength(13)]
        public string ISBN { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [Display(Name = "Cover Image")]
        public string? CoverImage { get; set; } // Stores the file path or URL

        //public string Status { get; set; }
        public DateTime PublishedDate { get; set; }

        // Навигационно свойство към UserBook
        public ICollection<UserBook> UserBooks { get; set; }


        // Navigation property for Reviews
        //public virtual ICollection<Review> Reviews { get; set; }
    }
}
 

