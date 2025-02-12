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

            // Проверка дали вече съществува запис за тази книга и потребител
            var userBook = _context.UserBooks.FirstOrDefault(ub => ub.BookId == bookId && ub.UserId == userId);

            if (userBook == null)
            {
                // Ако записът не съществува, създай нов
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
                // Ако записът съществува, обнови статуса
                userBook.Status = status;
                _context.UserBooks.Update(userBook);
            }

            _context.SaveChanges();
            return RedirectToAction("MyBooks");
        }

        
        // Действие за страница My Books
        public IActionResult MyBooks()
        {
            var userId = _userManager.GetUserId(User);

            // Извличане на книгите за текущия потребител
            var userBooks = _context.UserBooks
                .Where(ub => ub.UserId == userId)
                .Include(ub => ub.Book)
                .ToList();

            return View(userBooks);
        }
        [HttpPost]
        public IActionResult RemoveFromBookshelf(int id)
        {
            // Вземи UserId на текущия потребител
            var userId = _userManager.GetUserId(User);

            // Намиране на връзката между потребителя и книгата
            var userBook = _context.UserBooks.FirstOrDefault(ub => ub.BookId == id && ub.UserId == userId);

            // Ако не е намерена връзка, връщаме грешка
            if (userBook == null)
            {
                return NotFound();
            }

            // Премахваме връзката
            _context.UserBooks.Remove(userBook);

            // Записваме промените в базата данни
            _context.SaveChanges();

            // Пренасочваме потребителя обратно към страницата със собствените му книги
            return RedirectToAction("MyBooks");
        }
        [Route("Home/MyBooks")]
        public async Task<IActionResult> MyBooks(string searchQuery)
        {
            var userId = _userManager.GetUserId(User);

            // Включваме информацията за книгите
            var userBooksQuery = _context.UserBooks
                .Include(ub => ub.Book)
                .Where(ub => ub.UserId == userId);

            // Ако има търсене, филтрираме
            if (!string.IsNullOrEmpty(searchQuery))
            {
                searchQuery = searchQuery.ToLower().Trim();

                userBooksQuery = userBooksQuery.Where(ub =>
                    ub.Book != null && (
                        ub.Book.Title.ToLower().Contains(searchQuery) ||
                        ub.Book.Author.ToLower().Contains(searchQuery) ||
                        ub.Book.Genre.ToLower().Contains(searchQuery)
                    ));
            }

            var userBooks = await userBooksQuery.ToListAsync();

            ViewData["CurrentFilter"] = searchQuery;
            return View(userBooks);
        }




        [Route("Home/Books/Create")]
        public IActionResult Books(string searchQuery)
        {
            // Вземи всички книги
            var books = _context.Books.AsQueryable();

            // Ако има търсене, филтрирай резултатите
            if (!string.IsNullOrEmpty(searchQuery))
            {
                books = books.Where(b =>
                    b.Title.Contains(searchQuery) ||
                    b.Author.Contains(searchQuery) ||
                    b.Genre.Contains(searchQuery));
            }

            // Запази текущия филтър, за да го покажеш в полето за търсене
            ViewData["CurrentFilter"] = searchQuery;

            // Предай списъка с книги към изгледа
            return View(books.ToList());
        }

        public async Task<IActionResult> EditUser(string id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            user.Email = model.Email;
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
                return RedirectToAction("Users");

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            // Проверка дали потребителят е в ролята "Admin"
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["Error"] = "Admin users cannot be deleted!";
                return RedirectToAction("Users");
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = "User deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Error deleting user.";
            }

            return RedirectToAction("Users");
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