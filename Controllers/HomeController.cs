using System.Diagnostics;
using BooksTracking.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using BooksTracking.Areas.Identity.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BooksTracking.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly BooksTrackingDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ILogger<HomeController> logger, BooksTrackingDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _context = context;
            this._userManager = userManager;
        }

        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> Users()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }
        // Main Index action
        public IActionResult Index()
        {
            ViewData["UserID"] = _userManager.GetUserId(this.User);
            return View();
        }

        // Books listing action
        [HttpGet("Home/Books")]
        public IActionResult Books()
        {
            var allBooks = _context.Books.ToList(); // Get all books from the database
            return View(allBooks); // Pass books to the view
        }

        // Create a book (View)
        [Authorize(Roles = "Admin")]
        public IActionResult CreateBook()
        {
            return View(new Book()); // For creating a new book (empty form)
        }

        // POST method for creating a book
        [HttpPost]
        public IActionResult CreateForm(Book model)
        {
            if (ModelState.IsValid)
            {
                // Assign a default cover image if none is provided
                model.CoverImage = string.IsNullOrEmpty(model.CoverImage)
                    ? "/images/default-cover.png"
                    : model.CoverImage.Trim();

                // Add the new book to the database
                _context.Books.Add(model);
                _context.SaveChanges();

                return RedirectToAction("Books");
            }

            // Return the form with validation errors if any
            return View("CreateBook", model);
        }

        // Edit a book (View)
        [Authorize(Roles = "Admin")]
        public IActionResult EditBook(int id)
        {
            var book = _context.Books.FirstOrDefault(b => b.Id == id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book); // Pass the book to the view for editing
        }

        // POST method for editing a book
        [HttpPost]
        public IActionResult EditBook(Book model)
        {
            if (ModelState.IsValid)
            {
                var existingBook = _context.Books.FirstOrDefault(b => b.Id == model.Id);
                if (existingBook == null)
                {
                    return NotFound();
                }

                // Update the book details
                existingBook.Title = model.Title;
                existingBook.Author = model.Author;
                existingBook.Genre = model.Genre;
                existingBook.ISBN = model.ISBN;
                existingBook.Description = model.Description;
                existingBook.PublishedDate = model.PublishedDate;

                // Update the cover image if provided
                existingBook.CoverImage = string.IsNullOrEmpty(model.CoverImage)
                    ? existingBook.CoverImage
                    : model.CoverImage.Trim();

                _context.Books.Update(existingBook);
                _context.SaveChanges();

                return RedirectToAction("Books");
            }

            // Return the form with validation errors if any
            return View("EditBook", model);
        }

        // Delete book by ID
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var book = _context.Books.FirstOrDefault(b => b.Id == id);
            if (book == null)
            {
                return NotFound();
            }

            _context.Books.Remove(book);
            _context.SaveChanges();

            return RedirectToAction("Books");
        }

        // View details of a single book
        public IActionResult Details(int id)
        {
            var book = _context.Books.FirstOrDefault(b => b.Id == id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        [HttpPost]
        public IActionResult UpdateBookStatus(int bookId, string status)
        {
            var userId = _userManager.GetUserId(User);

            // �������� ���� ���� ���������� ����� �� ���� ����� � ����������
            var userBook = _context.UserBooks.FirstOrDefault(ub => ub.BookId == bookId && ub.UserId == userId);

            if (userBook == null)
            {
                // ��� ������� �� ����������, ������ ���
                userBook = new UserBook
                {
                    BookId = bookId,
                    UserId = userId,
                    Status = status
                };
                _context.UserBooks.Add(userBook);
            }
            else
            {
                // ��� ������� ����������, ������ �������
                userBook.Status = status;
                _context.UserBooks.Update(userBook);
            }

            _context.SaveChanges();
            return RedirectToAction("MyBooks");
        }

        // �������� �� �������� My Books
        public IActionResult MyBooks()
        {
            var userId = _userManager.GetUserId(User);

            // ��������� �� ������� �� ������� ����������
            var userBooks = _context.UserBooks
                .Where(ub => ub.UserId == userId)
                .Include(ub => ub.Book)
                .ToList();

            return View(userBooks);
        }
        [HttpPost]
        public IActionResult RemoveFromBookshelf(int id)
        {
            // ����� UserId �� ������� ����������
            var userId = _userManager.GetUserId(User);

            // �������� �� �������� ����� ����������� � �������
            var userBook = _context.UserBooks.FirstOrDefault(ub => ub.BookId == id && ub.UserId == userId);

            // ��� �� � �������� ������, ������� ������
            if (userBook == null)
            {
                return NotFound();
            }

            // ���������� ��������
            _context.UserBooks.Remove(userBook);

            // ��������� ��������� � ������ �����
            _context.SaveChanges();

            // ������������ ����������� ������� ��� ���������� ��� ����������� �� �����
            return RedirectToAction("MyBooks");
        }

        [Route("Home/Books/Create")]
        public IActionResult Books(string searchQuery)
        {
            // ����� ������ �����
            var books = _context.Books.AsQueryable();

            // ��� ��� �������, ��������� �����������
            if (!string.IsNullOrEmpty(searchQuery))
            {
                books = books.Where(b =>
                    b.Title.Contains(searchQuery) ||
                    b.Author.Contains(searchQuery) ||
                    b.Genre.Contains(searchQuery));
            }

            // ������ ������� ������, �� �� �� ������� � ������ �� �������
            ViewData["CurrentFilter"] = searchQuery;

            // ������ ������� � ����� ��� �������
            return View(books.ToList());
        }



        // Privacy page
        public IActionResult Privacy()
        {
            return View();
        }

        // Error handling page
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}